using System;
using System.Diagnostics;

namespace ATEditor
{
    /// <summary>
    /// ATEditor 内部独立日志门面。
    /// 完全自包含，不依赖任何外部业务框架（对 Game.Framework 零耦合）。
    /// 默认回退至 UnityEngine.Debug，支持宿主工程在启动时注入委托插槽桥接统一日志体系。
    /// </summary>
    public static class ATLog
    {
        // 外部可注入的委托插槽
        public static Action<string> LogHandler        = msg => UnityEngine.Debug.Log($"[ATEditor] {msg}");
        public static Action<string> WarningHandler    = msg => UnityEngine.Debug.LogWarning($"[ATEditor] {msg}");
        public static Action<string> ErrorHandler      = msg => UnityEngine.Debug.LogError($"[ATEditor] {msg}");
        public static Action<string, Exception> ExceptionHandler = (msg, ex) => UnityEngine.Debug.LogError($"[ATEditor] {msg}\n{ex}");

        /// <summary>常规信息日志（受编译宏控制）</summary>
        [Conditional("ENABLE_LOG")]
        [Conditional("UNITY_EDITOR")]
        public static void Info(string message) => LogHandler?.Invoke(message);

        /// <summary>警告日志（始终保留）</summary>
        public static void Warning(string message) => WarningHandler?.Invoke(message);

        /// <summary>错误日志（始终保留）</summary>
        public static void Error(string message) => ErrorHandler?.Invoke(message);

        /// <summary>异常捕获记录（始终保留）</summary>
        public static void Exception(Exception exception, string message = "") =>
            ExceptionHandler?.Invoke(message, exception);
    }
}
