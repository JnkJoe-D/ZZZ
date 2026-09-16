using System;
using Game.Framework;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 自动重连服务
    ///
    /// 策略：指数退避（1s → 2s → 4s → 8s → 16s → 30s 封顶）
    /// 最大尝试次数：3 次
    /// 重连成功后自动发送 Reconnect 请求恢复会话
    /// </summary>
    public class ReconnectService
    {
        // ── 配置 ────────────────────────────────
        private const float InitialDelay    = 3f;
        private const float MaxDelay        = 30f;
        private const int   MaxAttempts     = 3;

        // ── 状态 ────────────────────────────────
        private bool  _isReconnecting;
        private int   _attempt;
        private float _currentDelay;
        private float _timer;

        // ── 依赖 ────────────────────────────────
        private string _host;
        private int    _port;
        private readonly TcpChannel _tcp;

        // ── 回调 ────────────────────────────────
        public event Action OnReconnectSuccess;
        public event Action OnReconnectFailed;

        public bool  IsReconnecting => _isReconnecting;

        public ReconnectService(TcpChannel tcp)
        {
            _tcp = tcp;
        }

        /// <summary>启动重连流程</summary>
        public void Start(string host, int port)
        {
            if (_isReconnecting) return;

            _host           = host;
            _port           = port;
            _isReconnecting = true;
            _attempt        = 0;
            _currentDelay   = InitialDelay;
            _timer          = 0f;

            GLog.Info(LogTags.Heartbeat, "开始自动重连...");
        }

        /// <summary>由 NetworkManager.Update 每帧调用</summary>
        public void Update(float deltaTime)
        {
            if (!_isReconnecting) return;

            _timer += deltaTime;
            if (_timer < _currentDelay) return;

            _timer = 0f;
            _attempt++;

            // 超过最大次数
            if (_attempt > MaxAttempts)
            {
                _isReconnecting = false;
                GLog.Error(LogTags.Heartbeat, $"重连失败，已尝试 {MaxAttempts} 次");
                EventCenter.Publish(new NetReconnectFailedEvent { TotalAttempts = MaxAttempts });
                OnReconnectFailed?.Invoke();
                return;
            }

            // 广播重连中事件
            EventCenter.Publish(new NetReconnectingEvent
            {
                Attempt     = _attempt,
                WaitSeconds = _currentDelay
            });

            GLog.Info(LogTags.Heartbeat, $"尝试第 {_attempt} 次重连...");

            try
            {
                _tcp.Connect(_host, _port);

                if (_tcp.IsConnected)
                {
                    _isReconnecting = false;
                    GLog.Info(LogTags.Heartbeat, "重连成功！");
                    EventCenter.Publish(new NetReconnectedEvent());
                    OnReconnectSuccess?.Invoke();
                    return;
                }
            }
            catch (Exception e)
            {
                GLog.Warning(LogTags.Heartbeat, $"第 {_attempt} 次失败: {e.Message}");
            }

            // 指数退避
            _currentDelay = Mathf.Min(_currentDelay * 2f, MaxDelay);
        }

        /// <summary>取消重连</summary>
        public void Stop()
        {
            _isReconnecting = false;
        }
    }
}
