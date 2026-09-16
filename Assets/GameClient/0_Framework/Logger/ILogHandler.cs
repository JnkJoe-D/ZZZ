using System;

namespace Game.Framework
{
    /// <summary>
    /// 日志输出处理器接口。
    /// 实现此接口以自定义日志的输出目标（Unity 控制台、文件、远程服务器等）。
    /// </summary>
    public interface ILogHandler : IDisposable
    {
        /// <summary>
        /// 处理一条日志条目。
        /// 实现方必须保证此方法不抛出异常（内部自行捕获处理）。
        /// </summary>
        /// <param name="entry">日志条目（以 in 引用传递避免拷贝）</param>
        void Handle(in LogEntry entry);

        /// <summary>
        /// 刷新缓冲区，确保所有已缓存的日志条目被写出。
        /// </summary>
        void Flush();
    }
}
