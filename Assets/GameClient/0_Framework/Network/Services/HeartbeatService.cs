using System;
using Google.Protobuf;
using Game.Framework;
using Game.Network.Protocol;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 心跳管理器
    ///
    /// 职责：
    ///   1. 定时发送心跳包（默认 30 秒）
    ///   2. 收到心跳响应后计算 RTT
    ///   3. 超时检测（连续 N 次无响应 → 判定断线）
    /// </summary>
    public class HeartbeatService
    {
        // ── 配置 ────────────────────────────────
        private const float HeartbeatInterval     = 30f;  // 秒
        private const int   MaxMissedHeartbeats   = 3;    // 超时判定次数

        // ── 状态 ────────────────────────────────
        private float _timer;
        private int   _missedCount;
        private long  _lastSendTime; // 用于计算 RTT

        // ── 依赖 ────────────────────────────────
        private readonly TcpChannel        _tcp;
        private readonly MessageDispatcher _dispatcher;

        // ── 回调 ────────────────────────────────
        public event Action OnTimeout;

        // ── RTT 统计 ────────────────────────────
        public int CurrentRttMs { get; private set; }

        public HeartbeatService(TcpChannel tcp, MessageDispatcher dispatcher)
        {
            _tcp        = tcp;
            _dispatcher = dispatcher;

            // 注册心跳响应处理
            _dispatcher.Register<S2C_Heartbeat>(MsgId.Heartbeat, OnHeartbeatResponse);
        }

        /// <summary>由 NetworkManager.Update 每帧调用</summary>
        public void Update(float deltaTime)
        {
            if (!_tcp.IsConnected) return;

            _timer += deltaTime;
            if (_timer >= HeartbeatInterval)
            {
                _timer = 0f;
                SendHeartbeat();
            }
        }

        private void SendHeartbeat()
        {
            _lastSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _missedCount++;

            GLog.Info(LogTags.Heartbeat, $"发送心跳... (ClientTime: {_lastSendTime})");

            var msg = new C2S_Heartbeat
            {
                ClientTime = _lastSendTime
            };
            _tcp.Send(MsgId.Heartbeat, msg.ToByteArray());

            // 检查超时
            if (_missedCount >= MaxMissedHeartbeats)
            {
                GLog.Warning(LogTags.Heartbeat, $"心跳超时（连续 {_missedCount} 次无响应）");
                OnTimeout?.Invoke();
            }
        }

        private void OnHeartbeatResponse(S2C_Heartbeat response)
        {
            _missedCount = 0; // 收到响应，重置计数

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            CurrentRttMs = (int)(now - _lastSendTime);

            EventCenter.Publish(new HeartbeatResponseEvent
            {
                RttMs      = CurrentRttMs,
                ServerTime = response.ServerTime
            });
        }

        public void Reset()
        {
            // 通过将 timer 设置为间隔时间，保证连接成功后的第一帧 Update 立即发送一次心跳
            _timer       = HeartbeatInterval;
            _missedCount = 0;
            CurrentRttMs = 0;
        }

        public void Dispose()
        {
            _dispatcher.Unregister<S2C_Heartbeat>(MsgId.Heartbeat, OnHeartbeatResponse);
        }
    }
}
