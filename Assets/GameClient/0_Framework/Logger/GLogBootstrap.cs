using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// GLog 自动初始化与生命周期引导器。
    /// 在 Unity 启动及进入 PlayMode 时自动完成默认 Handler 的装配和退出时的资源清理。
    ///
    /// 初始化策略：
    /// 1. 编辑器模式：注册 UnityLogHandler，方便在 Console 查看着色日志。
    /// 2. 打包运行（Standalone/Mobile）：注册 UnityLogHandler 并异步挂载 FileLogHandler，
    ///    将日志持久化到 Application.persistentDataPath/Logs/。
    /// 3. 全局捕获：挂载未捕获的原生异常（LogType.Exception），确保崩溃栈写入本地文件。
    /// 4. 退出清理：监听 Application.quitting 自动 Flush 并释放所有 Handler。
    /// </summary>
    public static class GLogBootstrap
    {
        private static bool _isInitialized;
        private static FileLogHandler _activeFileHandler;

        /// <summary>
        /// 是否已完成初始化。
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 当前激活的文件日志处理器（若未启用则为 null）。
        /// </summary>
        public static FileLogHandler ActiveFileHandler => _activeFileHandler;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitOnEditorLoad()
        {
            Init();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnRuntimeLoad()
        {
            Init();
        }

        /// <summary>
        /// 显式初始化 GLog。多次调用幂等。
        /// </summary>
        public static void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // 1. 挂载 Unity 控制台输出
            GLog.AddHandler(new UnityLogHandler());

            // 2. 打包构建环境下自动挂载文件日志（编辑器下默认不产生文件，保持磁盘整洁）
#if !UNITY_EDITOR
            try
            {
                _activeFileHandler = new FileLogHandler();
                GLog.AddHandler(_activeFileHandler);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GLogBootstrap] Failed to initialize FileLogHandler: {ex.Message}");
            }
#endif

            // 3. 监听 Unity 未捕获异常并记录
            Application.logMessageReceivedThreaded += OnUnityLogReceived;

            // 4. 注册进程退出清理
            Application.quitting += OnApplicationQuit;

            // 5. 初始日志级别设置
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GLog.SetMinLevel(LogLevel.Trace);
#else
            GLog.SetMinLevel(LogLevel.Info);
#endif

            // 6. 桥接独立依赖中间件（ATEditor 与 MAnimSystem）的日志输出至 GLog（零耦合插槽）
            ATEditor.ATLog.LogHandler        = msg => GLog.Info(LogTags.ATEditor, msg);
            ATEditor.ATLog.WarningHandler    = msg => GLog.Warning(LogTags.ATEditor, msg);
            ATEditor.ATLog.ErrorHandler      = msg => GLog.Error(LogTags.ATEditor, msg);
            ATEditor.ATLog.ExceptionHandler  = (msg, ex) => GLog.Exception(LogTags.ATEditor, ex);

            MAnimSystem.MAnimLog.LogHandler     = msg => GLog.Info(LogTags.Anim, msg);
            MAnimSystem.MAnimLog.WarningHandler = msg => GLog.Warning(LogTags.Anim, msg);
            MAnimSystem.MAnimLog.ErrorHandler   = msg => GLog.Error(LogTags.Anim, msg);
        }

        private static void OnUnityLogReceived(string condition, string stackTrace, LogType type)
        {
            // 仅对原生未捕获异常（LogType.Exception）做兜底记录
            // GLog 自身的 Error/Fatal 走的是 Debug.LogError，不会触发 LogType.Exception
            if (type == LogType.Exception)
            {
                string msg = string.IsNullOrEmpty(stackTrace)
                    ? condition
                    : $"{condition}\n{stackTrace}";

                // 直接分发给文件处理器记录崩溃，防止递归调用 UnityLogHandler
                _activeFileHandler?.Handle(new LogEntry(
                    LogLevel.Fatal,
                    LogTags.Framework,
                    $"[Unhandled Exception] {msg}",
                    DateTime.Now,
                    "",
                    0
                ));
            }
        }

        private static void OnApplicationQuit()
        {
            Application.logMessageReceivedThreaded -= OnUnityLogReceived;
            Application.quitting -= OnApplicationQuit;

            // 刷新并关闭
            GLog.Flush();
            GLog.Shutdown();

            _activeFileHandler = null;
            _isInitialized = false;
        }
    }
}
