using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Game.Network
{
    /// <summary>
    /// TCP 连接通道
    ///
    /// 职责：
    ///   1. 管理 TCP 连接的建立/断开
    ///   2. 独立接收线程持续读取数据，通过 PacketCodec 拆包
    ///   3. 解码后的消息压入线程安全队列，由主线程出队处理
    ///   4. 线程安全的发送接口
    ///
    /// 线程模型：
    ///   接收线程 → RingBuffer → PacketCodec.TryDecode → ReceivedPackets 队列
    ///   主线程调用 Send() → 直接 Write 到 NetworkStream（加锁保护）
    /// </summary>
    public class TcpChannel : IDisposable
    {
        // ── 配置 ────────────────────────────────
        private const int ReceiveBufferSize = 64 * 1024; // 64KB

        // ── 连接状态 ────────────────────────────
        private TcpClient     _client;
        private NetworkStream _stream;
        private Thread        _receiveThread;
        private volatile bool _isRunning;

        public bool IsConnected => _client != null && _client.Connected && _isRunning;

        // ── 接收缓冲区 ─────────────────────────
        private readonly byte[] _receiveBuffer   = new byte[ReceiveBufferSize];
        private int _bufferOffset;
        private int _bufferLength;

        // ── 线程安全的消息队列（主线程出队）────
        public struct ReceivedPacket
        {
            public ushort MsgId;
            public uint   Sequence;
            public byte[] Payload;
        }
        private readonly ConcurrentQueue<ReceivedPacket> _receivedPackets = new();
        private readonly ConcurrentQueue<string> _disconnectQueue = new();

        // ── 发送锁 ─────────────────────────────
        private readonly object _sendLock = new();

        // ── 断线回调（保留兼容性）────────────
        public event Action<string> OnDisconnected;

        // ── 序列号管理 ──────────────────────────
        private int _sendSequence;

        // ────────────────────────────────────────
        // 连接
        // ────────────────────────────────────────

        /// <summary>同步连接到服务器</summary>
        public void Connect(string host, int port)
        {
            if (IsConnected)
            {
                Debug.LogWarning("[TcpChannel] 已连接，请先断开");
                return;
            }

            try
            {
                _client = new TcpClient();
                _client.NoDelay = true; // 禁用 Nagle 算法，减少延迟
                _client.ReceiveBufferSize = ReceiveBufferSize;
                _client.SendBufferSize    = ReceiveBufferSize;
                _client.Connect(host, port);

                _stream = _client.GetStream();
                _isRunning    = true;
                _bufferOffset = 0;
                _bufferLength = 0;
                _sendSequence = 0;

                // 启动接收线程
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name         = "TcpReceiveThread"
                };
                _receiveThread.Start();

                Debug.Log($"[TcpChannel] 已连接: {host}:{port}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TcpChannel] 连接请求被拒绝: {e.Message}");
                Cleanup();
                // 必须抛出异常，让上层 NetworkManager 知道连接失败以进入自动重连
                throw;
            }
        }

        // ────────────────────────────────────────
        // 发送
        // ────────────────────────────────────────

        /// <summary>
        /// 发送消息（线程安全）
        /// </summary>
        public void Send(ushort msgId, byte[] payload)
        {
            if (!IsConnected)
            {
                Debug.LogWarning($"[TcpChannel] 未连接，无法发送 0x{msgId:X4}");
                return;
            }

            var seq = Interlocked.Increment(ref _sendSequence);
            var data = PacketCodec.Encode(msgId, (uint)seq, payload);

            lock (_sendLock)
            {
                try
                {
                    _stream.Write(data, 0, data.Length);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TcpChannel] 发送失败: {e.Message}");
                    HandleDisconnect("发送异常: " + e.Message);
                }
            }
        }

        // ────────────────────────────────────────
        // 接收（后台线程）
        // ────────────────────────────────────────

        private void ReceiveLoop()
        {
            try
            {
                while (_isRunning)
                {
                    // 整理缓冲区（将未消费的数据移到头部）
                    if (_bufferOffset > 0 && _bufferLength > 0)
                    {
                        Buffer.BlockCopy(_receiveBuffer, _bufferOffset, _receiveBuffer, 0, _bufferLength);
                    }
                    _bufferOffset = 0;

                    // 读取新数据
                    int writePos = _bufferLength;
                    int available = ReceiveBufferSize - writePos;
                    if (available <= 0)
                    {
                        Debug.LogError("[TcpChannel] 接收缓冲区溢出");
                        break;
                    }

                    int bytesRead = _stream.Read(_receiveBuffer, writePos, available);
                    if (bytesRead == 0)
                    {
                        // 服务端关闭了连接
                        HandleDisconnect("服务端关闭连接");
                        break;
                    }

                    _bufferLength += bytesRead;

                    // 拆包循环（处理粘包）
                    while (_bufferLength >= PacketCodec.HeaderSize)
                    {
                        if (!PacketCodec.TryDecode(
                            _receiveBuffer, ref _bufferOffset, ref _bufferLength,
                            out ushort msgId, out uint seq, out byte[] payload))
                        {
                            break; // 数据不足，等待下次读取
                        }

                        _receivedPackets.Enqueue(new ReceivedPacket
                        {
                            MsgId    = msgId,
                            Sequence = seq,
                            Payload  = payload
                        });
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // 正常关闭，忽略
            }
            catch (System.IO.IOException e)
            {
                if (_isRunning)
                {
                    HandleDisconnect("网络异常: " + e.Message);
                }
            }
            catch (Exception e)
            {
                if (_isRunning)
                {
                    Debug.LogError($"[TcpChannel] 接收异常: {e}");
                    HandleDisconnect("接收异常: " + e.Message);
                }
            }
        }

        // ────────────────────────────────────────
        // 主线程轮询
        // ────────────────────────────────────────

        /// <summary>尝试从队列中取出一个已解码的消息包（主线程调用）</summary>
        public bool TryDequeue(out ReceivedPacket packet)
        {
            return _receivedPackets.TryDequeue(out packet);
        }

        /// <summary>尝试从队列中取出断线事件（主线程调用，保证线程安全）</summary>
        public bool TryDequeueDisconnect(out string reason)
        {
            return _disconnectQueue.TryDequeue(out reason);
        }

        // ────────────────────────────────────────
        // 断开与清理
        // ────────────────────────────────────────

        private void HandleDisconnect(string reason)
        {
            if (!_isRunning) return;
            _isRunning = false;
            Debug.Log($"[TcpChannel] 断开: {reason}");
            _disconnectQueue.Enqueue(reason);
            // 兼容性触发（建议仅在单线程调试时使用，业务层请通过 NetworkManager 主线程派发）
            OnDisconnected?.Invoke(reason);
        }

        public void Disconnect()
        {
            _isRunning = false;
            while (_disconnectQueue.TryDequeue(out _)) { }
            Cleanup();
            Debug.Log("[TcpChannel] 已主动断开");
        }

        private void Cleanup()
        {
            _isRunning = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
