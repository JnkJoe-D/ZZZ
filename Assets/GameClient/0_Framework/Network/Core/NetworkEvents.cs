using Game.Framework;

namespace Game.Framework
{
    // ============================================================
    // 网络状态事件
    // ============================================================

    /// <summary>TCP 连接成功</summary>
    public struct NetConnectedEvent : IGameEvent
    {
        public string Host;
        public int    Port;
    }

    /// <summary>TCP 连接断开</summary>
    public struct NetDisconnectedEvent : IGameEvent
    {
        public DisconnectReason Reason;
        public string           Message;
    }

    public enum DisconnectReason
    {
        /// <summary>主动断开</summary>
        Manual,
        /// <summary>网络异常（IOException）</summary>
        NetworkError,
        /// <summary>心跳超时</summary>
        HeartbeatTimeout,
        /// <summary>服务端踢下线</summary>
        ServerKick,
    }

    /// <summary>正在尝试重连</summary>
    public struct NetReconnectingEvent : IGameEvent
    {
        /// <summary>当前重连次数</summary>
        public int Attempt;
        /// <summary>下次重连等待秒数</summary>
        public float WaitSeconds;
    }

    /// <summary>重连成功</summary>
    public struct NetReconnectedEvent : IGameEvent { }

    /// <summary>重连失败（超过最大重试次数）</summary>
    public struct NetReconnectFailedEvent : IGameEvent
    {
        public int TotalAttempts;
    }

    // ============================================================
    // 网络数据事件
    // ============================================================

    /// <summary>收到心跳响应（含 RTT 统计）</summary>
    public struct HeartbeatResponseEvent : IGameEvent
    {
        /// <summary>本次往返时间（毫秒）</summary>
        public int RttMs;
        /// <summary>服务器时间戳</summary>
        public long ServerTime;
    }

    /// <summary>
    /// 收到错误响应（服务端发来的通用错误码）
    /// </summary>
    public struct ServerErrorEvent : IGameEvent
    {
        public int    Code;
        public string Message;
    }
}
