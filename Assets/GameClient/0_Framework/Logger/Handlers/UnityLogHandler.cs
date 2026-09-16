using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// Unity 控制台日志处理器。
    /// 将 GLog 的日志条目桥接到 UnityEngine.Debug，保留 Unity Console 原生的着色与过滤能力。
    ///
    /// 映射关系：
    ///   Trace / Debug / Info → Debug.Log
    ///   Warning              → Debug.LogWarning
    public sealed class UnityLogHandler : ILogHandler
    {
        public void Handle(in LogEntry entry)
        {
            string coloredTag = LogColorConfig.GetColoredTag(entry.Tag);
            var formatted = entry.FormatWithCustomTag(coloredTag);

            switch (entry.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formatted);
                    break;

                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formatted);
                    break;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(formatted);
                    break;
            }
        }

        public void Flush()
        {
            // Unity Console 不需要显式刷新
        }

        public void Dispose()
        {
            // 无需释放资源
        }
    }
}
