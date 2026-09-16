using System;

namespace Game.Framework
{
    /// <summary>
    /// 日志条目值对象。使用 readonly struct 避免堆分配。
    /// 在 GLog 内部构造，传递给各 ILogHandler 进行输出。
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>日志级别</summary>
        public readonly LogLevel Level;
        /// <summary>模块标签</summary>
        public readonly string Tag;
        /// <summary>日志消息内容</summary>
        public readonly string Message;
        /// <summary>日志产生时的时间戳</summary>
        public readonly DateTime Timestamp;
        /// <summary>调用方源文件路径（由编译器自动注入）</summary>
        public readonly string CallerFile;
        /// <summary>调用方源文件行号（由编译器自动注入）</summary>
        public readonly int CallerLine;

        public LogEntry(LogLevel level, string tag, string message,
            DateTime timestamp, string callerFile, int callerLine)
        {
            Level = level;
            Tag = tag ?? string.Empty;
            Message = message ?? string.Empty;
            Timestamp = timestamp;
            CallerFile = callerFile ?? string.Empty;
            CallerLine = callerLine;
        }

        /// <summary>
        /// 获取当前日志级别的简短大写字符串表示。
        /// </summary>
        public string GetLevelString()
        {
            switch (Level)
            {
                case LogLevel.Trace:   return "TRACE";
                case LogLevel.Debug:   return "DEBUG";
                case LogLevel.Info:    return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error:   return "ERROR";
                case LogLevel.Fatal:   return "FATAL";
                default:               return "?";
            }
        }

        /// <summary>
        /// 格式化为标准日志字符串：[HH:mm:ss.fff] [LEVEL] [Tag] Message
        /// </summary>
        public string Format()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] [{GetLevelString()}] [{Tag}] {Message}";
        }

        /// <summary>
        /// 格式化为指定标签展示样式（如带富文本颜色的标签）的字符串。
        /// </summary>
        public string FormatWithCustomTag(string tagDisplay)
        {
            return $"[{Timestamp:HH:mm:ss.fff}] [{GetLevelString()}] {tagDisplay} {Message}";
        }

        /// <summary>
        /// 格式化为包含调用位置信息的详细字符串（用于文件持久化，保证无富文本纯文本输出）。
        /// </summary>
        public string FormatDetailed()
        {
            // 提取文件名（不含完整路径）
            var fileName = CallerFile;
            var lastSlash = CallerFile.LastIndexOf('/');
            if (lastSlash < 0) lastSlash = CallerFile.LastIndexOf('\\');
            if (lastSlash >= 0) fileName = CallerFile.Substring(lastSlash + 1);

            return $"[{Timestamp:HH:mm:ss.fff}] [{GetLevelString()}] [{Tag}] {Message}  ({fileName}:{CallerLine})";
        }
    }
}
