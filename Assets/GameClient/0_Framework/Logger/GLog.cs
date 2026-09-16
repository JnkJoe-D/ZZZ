using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Game.Framework
{
    /// <summary>
    /// 全局日志入口（静态门面）。所有业务代码仅通过此类输出日志。
    ///
    /// 纯 C# 实现，不直接依赖 UnityEngine。
    /// Unity 相关的输出桥接由 UnityLogHandler 提供，通过 GLogBootstrap 自动注册。
    ///
    /// 编译剥离策略（通过 [Conditional] 特性实现）：
    ///   Trace   — 仅在定义 ENABLE_LOG_TRACE 时包含
    ///   Debug   — 在编辑器 (UNITY_EDITOR) 或定义 ENABLE_LOG_DEBUG 时包含
    ///   Info    — 在编辑器、开发版构建 (DEVELOPMENT_BUILD) 或定义 ENABLE_LOG 时包含
    ///   Warning — 始终包含（可运行时过滤）
    ///   Error   — 始终包含
    ///   Fatal   — 始终包含
    /// </summary>
    public static class GLog
    {
        // ── 处理器管理 ──
        // 使用 volatile 数组引用实现 copy-on-write，Log 热路径无需加锁
        private static volatile ILogHandler[] _handlers = Array.Empty<ILogHandler>();
        private static readonly List<ILogHandler> _handlerList = new List<ILogHandler>(4);
        private static readonly object _handlerLock = new object();

        // ── 标签过滤 ──
        private static readonly HashSet<string> _disabledTags = new HashSet<string>();
        private static readonly object _tagLock = new object();

        // ── 全局级别 ──
        private static LogLevel _minLevel = LogLevel.Trace;

        // ═══════════════════════════════════════
        //  配置 API
        // ═══════════════════════════════════════

        /// <summary>
        /// 添加日志输出处理器。
        /// </summary>
        public static void AddHandler(ILogHandler handler)
        {
            if (handler == null) return;
            lock (_handlerLock)
            {
                _handlerList.Add(handler);
                _handlers = _handlerList.ToArray();
            }
        }

        /// <summary>
        /// 移除日志输出处理器。
        /// </summary>
        public static void RemoveHandler(ILogHandler handler)
        {
            if (handler == null) return;
            lock (_handlerLock)
            {
                _handlerList.Remove(handler);
                _handlers = _handlerList.ToArray();
            }
        }

        /// <summary>
        /// 设置全局最低日志级别。低于此级别的日志在运行时被丢弃。
        /// </summary>
        public static void SetMinLevel(LogLevel level) => _minLevel = level;

        /// <summary>获取当前全局最低日志级别。</summary>
        public static LogLevel MinLevel => _minLevel;

        /// <summary>
        /// 禁用指定标签的日志输出（运行时动态过滤）。
        /// </summary>
        public static void DisableTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            lock (_tagLock) { _disabledTags.Add(tag); }
        }

        /// <summary>
        /// 重新启用指定标签的日志输出。
        /// </summary>
        public static void EnableTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            lock (_tagLock) { _disabledTags.Remove(tag); }
        }

        /// <summary>
        /// 查询指定标签是否处于启用状态。
        /// </summary>
        public static bool IsTagEnabled(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return true;
            lock (_tagLock) { return !_disabledTags.Contains(tag); }
        }

        /// <summary>
        /// 刷新所有处理器的缓冲区，确保已缓存的日志条目全部写出。
        /// </summary>
        public static void Flush()
        {
            var snapshot = _handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i].Flush(); }
                catch { /* 刷新失败不应影响其他处理器 */ }
            }
        }

        /// <summary>
        /// 关闭所有处理器并释放资源。在应用退出时调用。
        /// </summary>
        public static void Shutdown()
        {
            lock (_handlerLock)
            {
                for (int i = 0; i < _handlerList.Count; i++)
                {
                    try { _handlerList[i].Dispose(); }
                    catch { /* 释放失败不应阻塞后续清理 */ }
                }
                _handlerList.Clear();
                _handlers = Array.Empty<ILogHandler>();
            }
        }

        // ═══════════════════════════════════════
        //  日志 API
        // ═══════════════════════════════════════

        /// <summary>
        /// 极细粒度追踪日志。仅在定义 ENABLE_LOG_TRACE 编译宏时包含。
        /// </summary>
        [Conditional("ENABLE_LOG_TRACE")]
        public static void Trace(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Trace, tag, message, file, line);
        }

        /// <summary>
        /// 调试日志。在编辑器或定义 ENABLE_LOG_DEBUG 编译宏时包含。
        /// </summary>
        [Conditional("ENABLE_LOG_DEBUG")]
        [Conditional("UNITY_EDITOR")]
        public static void Debug(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Debug, tag, message, file, line);
        }

        /// <summary>
        /// 常规信息日志。在编辑器、开发版构建或定义 ENABLE_LOG 编译宏时包含。
        /// </summary>
        [Conditional("ENABLE_LOG")]
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Info, tag, message, file, line);
        }

        /// <summary>
        /// 警告日志。始终编译包含，可运行时通过级别/标签过滤。
        /// </summary>
        public static void Warning(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Warning, tag, message, file, line);
        }

        /// <summary>
        /// 错误日志。始终编译包含。
        /// </summary>
        public static void Error(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Error, tag, message, file, line);
        }

        /// <summary>
        /// 致命错误日志。始终编译包含。
        /// </summary>
        public static void Fatal(string tag, string message,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(LogLevel.Fatal, tag, message, file, line);
        }

        /// <summary>
        /// 记录异常。始终编译包含，不受 [Conditional] 剥离影响。
        /// </summary>
        public static void Exception(string tag, Exception exception,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (exception == null) return;
            var msg = $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
            LogInternal(LogLevel.Error, tag, msg, file, line);
        }

        // ═══════════════════════════════════════
        //  内部实现
        // ═══════════════════════════════════════

        private static void LogInternal(LogLevel level, string tag, string message,
            string file, int line)
        {
            // 级别过滤（快速路径，无锁）
            if (level < _minLevel) return;

            // 标签过滤（仅在有禁用标签时才加锁检查）
            if (!string.IsNullOrEmpty(tag) && _disabledTags.Count > 0)
            {
                lock (_tagLock)
                {
                    if (_disabledTags.Contains(tag)) return;
                }
            }

            var entry = new LogEntry(level, tag, message, DateTime.Now, file, line);

            // 分发给所有处理器（无锁读取 volatile 数组快照）
            var snapshot = _handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].Handle(in entry);
                }
                catch (Exception ex)
                {
                    // 处理器异常不应影响其他处理器和调用方
                    // 使用系统诊断输出作为最后防线，避免递归调用 GLog
                    System.Diagnostics.Debug.WriteLine(
                        $"[GLog] Handler '{snapshot[i].GetType().Name}' threw: {ex.Message}");
                }
            }
        }
    }
}
