using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// UDP 快速通道（帧同步专用）
    ///
    /// 特性：
    ///   - 每个 UDP 数据报 = 一个完整的 GamePackage（无粘包问题）
    ///   - 容忍丢包，保持最新（旧帧数据到达后直接丢弃）
    ///   - 独立接收线程 + ConcurrentQueue
    ///   - 与 TCP 使用相同的 PacketCodec 编码格式
    /// </summary>
    public class UdpChannel : IDisposable
    {
        private const int UdpBufferSize = 4096; // UDP 单包上限（足够帧同步数据）

        private UdpClient   _udpClient;
        private Thread      _receiveThread;
        private volatile bool _isRunning;

        private IPEndPoint  _serverEndPoint;

        public bool IsConnected => _udpClient != null && _isRunning;

        // ── 已接收的消息队列 ────────────────────
        public struct ReceivedPacket
        {
            public ushort MsgId;
            public uint   Sequence;
            public byte[] Payload;
        }
        private readonly ConcurrentQueue<ReceivedPacket> _receivedPackets = new();

        // ── 序列号 ─────────────────────────────
        private int _sendSequence;

        // ── 最新序号（丢弃乱序旧包） ───────────────
        private uint _latestReceivedSequence;

        // ────────────────────────────────────────
        // 连接
        // ────────────────────────────────────────

        /// <summary>
        /// 初始化 UDP 通道
        /// </summary>
        /// <param name="host">服务器IP</param>
        /// <param name="port">服务器UDP端口</param>
        public void Connect(string host, int port)
        {
            if (IsConnected)
            {
                GLog.Warning(LogTags.Udp, "已连接");
                return;
            }

            _serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            _udpClient = new UdpClient();
            _udpClient.Connect(_serverEndPoint);

            _isRunning    = true;
            _sendSequence = 0;
            _latestReceivedSequence = 0;

            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name         = "UdpReceiveThread"
            };
            _receiveThread.Start();

            GLog.Info(LogTags.Udp, $"UDP 通道就绪: {host}:{port}");
        }

        // ────────────────────────────────────────
        // 发送
        // ────────────────────────────────────────

        /// <summary>发送消息（线程安全）</summary>
        public void Send(ushort msgId, byte[] payload)
        {
            if (!IsConnected) return;

            var seq = Interlocked.Increment(ref _sendSequence);
            var data = PacketCodec.Encode(msgId, (uint)seq, payload);

            try
            {
                _udpClient.Send(data, data.Length);
            }
            catch (Exception e)
            {
                GLog.Warning(LogTags.Udp, $"UDP 发送失败: {e.Message}");
            }
        }

        // ────────────────────────────────────────
        // 接收（后台线程）
        // ────────────────────────────────────────

        private void ReceiveLoop()
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (_isRunning)
                {
                    byte[] data = _udpClient.Receive(ref remoteEp);

                    if (data.Length < PacketCodec.HeaderSize)
                        continue;

                    if (!PacketCodec.DecodeUdp(data, data.Length,
                        out ushort msgId, out uint seq, out byte[] payload))
                    {
                        continue;
                    }

                    // ── 单调序号过滤：丢弃迟到的乱序旧包 ──
                    if (_latestReceivedSequence > 0 && seq <= _latestReceivedSequence)
                    {
                        continue;
                    }
                    _latestReceivedSequence = seq;

                    _receivedPackets.Enqueue(new ReceivedPacket
                    {
                        MsgId    = msgId,
                        Sequence = seq,
                        Payload  = payload
                    });
                }
            }
            catch (SocketException)
            {
                // 正常关闭
            }
            catch (ObjectDisposedException)
            {
                // 正常关闭
            }
            catch (Exception e)
            {
                if (_isRunning)
                {
                    GLog.Error(LogTags.Udp, $"接收异常: {e}");
                }
            }
        }

        // ────────────────────────────────────────
        // 主线程轮询
        // ────────────────────────────────────────

        public bool TryDequeue(out ReceivedPacket packet)
        {
            return _receivedPackets.TryDequeue(out packet);
        }

        // ────────────────────────────────────────
        // 关闭
        // ────────────────────────────────────────

        public void Disconnect()
        {
            _isRunning = false;
            try { _udpClient?.Close(); } catch { }
            _udpClient = null;
            _latestReceivedSequence = 0;
            while (_receivedPackets.TryDequeue(out _)) { }
            GLog.Info(LogTags.Udp, "已关闭");
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
