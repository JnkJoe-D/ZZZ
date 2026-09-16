namespace Game.Framework
{
    /// <summary>
    /// 日志级别枚举。数值越大，严重程度越高。
    /// 低于当前全局最低级别的日志将被丢弃。
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>极细粒度追踪，仅在定义 ENABLE_LOG_TRACE 时编译包含</summary>
        Trace = 0,
        /// <summary>一般调试信息，在编辑器或定义 ENABLE_LOG_DEBUG 时编译包含</summary>
        Debug = 1,
        /// <summary>常规运行信息，在编辑器、开发版构建或定义 ENABLE_LOG 时编译包含</summary>
        Info = 2,
        /// <summary>潜在问题警告，始终编译包含</summary>
        Warning = 3,
        /// <summary>运行时错误，始终编译包含</summary>
        Error = 4,
        /// <summary>致命错误，程序即将崩溃，始终编译包含</summary>
        Fatal = 5,
        /// <summary>关闭所有日志输出</summary>
        Off = 6,
    }
}
