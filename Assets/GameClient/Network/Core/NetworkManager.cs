using System;
using Game.Framework;
using Game.Network.Protocol;
using Google.Protobuf;
using UnityEngine;

namespace Game.Network
{
    /// <summary>
    /// 网络总管理器
    ///
    /// 职责：
    ///   1. 统一管理 TCP / UDP 双通道的生命周期
    ///   2. 驱动消息分发（主线程安全）
    ///   3. 驱动心跳和重连服务
    ///   4. 提供业务层的统一发送 API
    ///
    /// 由 GameRoot 创建并在 Update 中驱动：
    ///   _networkManager = new NetworkManager();
    ///   _networkManager.Initialize(host, tcpPort, udpPort);
    ///   _networkManager.Update();  // 每帧调用
    /// </summary>
    public class NetworkManager : Game.Framework.Singleton<NetworkManager>
    {
        // ── 子模块 ─────────────────────────────
        private TcpChannel        _tcp;
        private UdpChannel        _udp;
        private MessageDispatcher _dispatcher;
        private HeartbeatService  _heartbeat;
        private ReconnectService  _reconnect;

        // ── 连接信息 ────────────────────────────
        private string _host;
        private int    _tcpPort;
        private int    _udpPort;
        private string _token; // 用于重连时恢复会话

        // ── 状态 ────────────────────────────────
        public bool IsTcpConnected => _tcp != null && _tcp.IsConnected;
        public bool IsUdpConnected => _udp != null && _udp.IsConnected;
        public int  CurrentRttMs   => _heartbeat?.CurrentRttMs ?? -1;

        // ── 消息分发器（业务层可直接注册 Handler）
        public MessageDispatcher Dispatcher => _dispatcher;

        // ────────────────────────────────────────
        // 初始化
        // ────────────────────────────────────────

        public void Initialize(string host, int tcpPort, int udpPort)
        {
            _host       = host;
            _tcpPort    = tcpPort;
            _udpPort    = udpPort;
            _dispatcher = new MessageDispatcher();
            _tcp        = new TcpChannel();
            _udp        = new UdpChannel();
            _heartbeat  = new HeartbeatService(_tcp, _dispatcher);
            _reconnect  = new ReconnectService(_tcp);

            // 注册错误消息处理
            _dispatcher.Register<CommonResponse>(MsgId.Error, OnServerError);

            // TCP 断线事件
            _tcp.OnDisconnected += OnTcpDisconnected;

            // 重连回调
            _reconnect.OnReconnectSuccess += OnReconnectSuccess;

            Debug.Log($"[NetworkManager] 初始化完成 -> {host}:{tcpPort} (TCP), {udpPort} (UDP)");
        }

        // ────────────────────────────────────────
        // 连接
        // ────────────────────────────────────────

        /// <summary>连接 TCP 通道</summary>
        public void ConnectTcp()
        {
            _reconnect?.Stop(); // 手动请求连接时，停止任何正在进行的重连流程

            try
            {
                _tcp.Connect(_host, _tcpPort);
                _heartbeat.Reset();
                EventCenter.Publish(new NetConnectedEvent { Host = _host, Port = _tcpPort });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkManager] TCP 连接尝试失败: {e.Message}");
                // 复用断线逻辑，抛出异常并直接进入重连循环
                OnTcpDisconnected(e.Message);
            }
        }

        /// <summary>主动断开 TCP 连接</summary>
        public void DisconnectTcp()
        {
            _reconnect?.Stop(); // 停止重连服务
            _tcp?.Disconnect();
            // 注意：TcpChannel.Disconnect 内部不触发 OnDisconnected 事件，
            // 这样就不会进入自动重连循环。
            
            EventCenter.Publish(new NetDisconnectedEvent
            {
                Reason = DisconnectReason.Manual,
                Message = "玩家主动断开连接"
            });
        }

        /// <summary>连接 UDP 通道（通常在进入帧同步战斗时调用）</summary>
        public void ConnectUdp()
        {
            _udp.Connect(_host, _udpPort);
        }

        /// <summary>断开 UDP 通道（战斗结束时调用）</summary>
        public void DisconnectUdp()
        {
            _udp.Disconnect();
        }

        // ────────────────────────────────────────
        // 发送 API
        // ────────────────────────────────────────

        /// <summary>通过 TCP 发送 Protobuf 消息（可靠通道）</summary>
        public void SendTcp<T>(ushort msgId, T message) where T : IMessage
        {
            _tcp?.Send(msgId, message.ToByteArray());
        }

        /// <summary>通过 TCP 发送空消息体</summary>
        public void SendTcp(ushort msgId)
        {
            _tcp?.Send(msgId, Array.Empty<byte>());
        }

        /// <summary>通过 UDP 发送 Protobuf 消息（帧同步通道）</summary>
        public void SendUdp<T>(ushort msgId, T message) where T : IMessage
        {
            _udp?.Send(msgId, message.ToByteArray());
        }

        /// <summary>通过 UDP 发送原始字节</summary>
        public void SendUdpRaw(ushort msgId, byte[] payload)
        {
            _udp?.Send(msgId, payload);
        }

        // ────────────────────────────────────────
        // Token 管理（重连用）
        // ────────────────────────────────────────

        /// <summary>登录成功后设置 Token</summary>
        public void SetToken(string token) { _token = token; }

        // ────────────────────────────────────────
        // 主循环驱动（GameRoot.Update 调用）
        // ────────────────────────────────────────

        public void Update()
        {
            float dt = Time.deltaTime;

            // ── 驱动重连 ───────────────────────
            if (_reconnect.IsReconnecting)
            {
                _reconnect.Update(dt);
                return; // 重连中不处理其他消息
            }

            // ── 驱动心跳 ───────────────────────
            _heartbeat?.Update(dt);

            // ── 处理 TCP 消息（每帧最多处理 100 条，防卡主线程）
            int tcpCount = 0;
            while (tcpCount < 100 && _tcp.TryDequeue(out var tcpPacket))
            {
                _dispatcher.Dispatch(tcpPacket.MsgId, tcpPacket.Payload);
                tcpCount++;
            }

            // ── 处理 UDP 消息（帧同步，每帧全部消费）
            while (_udp.IsConnected && _udp.TryDequeue(out var udpPacket))
            {
                _dispatcher.Dispatch(udpPacket.MsgId, udpPacket.Payload);
            }
        }

        // ────────────────────────────────────────
        // 内部事件处理
        // ────────────────────────────────────────

        private void OnTcpDisconnected(string reason)
        {
            EventCenter.Publish(new NetDisconnectedEvent
            {
                Reason  = DisconnectReason.NetworkError,
                Message = reason
            });

            // 自动启动重连
            _reconnect.Start(_host, _tcpPort);
        }

        private void OnReconnectSuccess()
        {
            _heartbeat.Reset();

            // 如果有 Token，发送重连请求恢复会话
            if (!string.IsNullOrEmpty(_token))
            {
                var msg = new C2S_Reconnect { Token = _token };
                SendTcp(MsgId.Reconnect, msg);
                Debug.Log("[NetworkManager] 已发送重连请求");
            }
        }

        private void OnServerError(CommonResponse response)
        {
            EventCenter.Publish(new ServerErrorEvent
            {
                Code    = response.Code,
                Message = response.Message
            });
            Debug.LogWarning($"[NetworkManager] 服务端错误: [{response.Code}] {response.Message}");
        }

        // ────────────────────────────────────────
        // 关闭
        // ────────────────────────────────────────

        public void Shutdown()
        {
            _reconnect?.Stop();
            _heartbeat?.Dispose();
            _tcp?.Dispose();
            _udp?.Dispose();
            _dispatcher?.ClearAll();
            Debug.Log("[NetworkManager] 已关闭");
        }
    }
}
