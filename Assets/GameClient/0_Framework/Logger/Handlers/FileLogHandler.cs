using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 文件日志输出处理器。
    /// 采用生产者-消费者模型，通过专用后台线程异步写入文件，不阻塞主线程和渲染循环。
    /// 仅在打包后的程序中默认启用（通过 GLogBootstrap 配置），避免在编辑器内频繁读写磁盘。
    ///
    /// 特性：
    /// 1. 异步无锁排队：主线程仅将格式化后的字符串 Enqueue 并通知信号量，极小开销。
    /// 2. 日志滚动归档：每次启动创建新的独立日志文件，自动清理超过保留数量的历史旧日志。
    /// 3. 安全退出保障：Dispose 和 Flush 时会等待队列中剩余日志全部持久化到磁盘。
    /// 4. 异常隔离保护：内部所有文件 I/O 均包含防御性捕获，绝不向外抛出异常影响游戏运行。
    /// </summary>
    public sealed class FileLogHandler : ILogHandler
    {
        private readonly string _logDirectory;
        private readonly int _maxArchiveFiles;
        private readonly LogLevel _minLevel;
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly Thread _workerThread;

        private volatile bool _isDisposed;
        private StreamWriter _writer;
        private string _currentFilePath;

        /// <summary>
        /// 获取当前写入的日志文件绝对路径。
        /// </summary>
        public string CurrentFilePath => _currentFilePath;

        /// <summary>
        /// 是否允许在 Unity 编辑器中保存日志文件。默认 false（仅打包真机保存）。
        /// </summary>
        public bool EnableInEditor { get; set; }

        /// <summary>
        /// 构造文件日志处理器。
        /// </summary>
        /// <param name="logDirectory">日志输出目录。若为 null 则使用 Application.persistentDataPath/Logs</param>
        /// <param name="maxArchiveFiles">最多保留的历史日志文件数，超出自动删除最旧的文件</param>
        /// <param name="minLevel">写入文件的最低日志级别，默认 Info</param>
        /// <param name="enableInEditor">是否在编辑器中启用，默认 false</param>
        public FileLogHandler(
            string logDirectory = null,
            int maxArchiveFiles = 7,
            LogLevel minLevel = LogLevel.Info,
            bool enableInEditor = false)
        {
            _maxArchiveFiles = Math.Max(1, maxArchiveFiles);
            _minLevel = minLevel;
            EnableInEditor = enableInEditor;

            if (string.IsNullOrEmpty(logDirectory))
            {
                try
                {
                    _logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                }
                catch
                {
                    _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                }
            }
            else
            {
                _logDirectory = logDirectory;
            }

            // 若在编辑器内且未显式开启，则不启动工作线程
            if (Application.isEditor && !EnableInEditor)
            {
                return;
            }

            InitLogFile();

            _workerThread = new Thread(WriteLoop)
            {
                Name = "GLog_FileWriter",
                IsBackground = true
            };
            _workerThread.Start();
        }

        public void Handle(in LogEntry entry)
        {
            if (_isDisposed) return;
            if (Application.isEditor && !EnableInEditor) return;
            if (entry.Level < _minLevel) return;

            // 文件日志使用详细格式（包含文件名与行号）
            string formatted = entry.FormatDetailed();
            _logQueue.Enqueue(formatted);
            _signal.Set();
        }

        public void Flush()
        {
            if (_isDisposed || _writer == null) return;

            // 触发写入并等待队列排空
            _signal.Set();

            // 简单自旋等待队列写空（最多 500ms）
            int timeoutMs = 500;
            int elapsed = 0;
            while (!_logQueue.IsEmpty && elapsed < timeoutMs)
            {
                Thread.Sleep(10);
                elapsed += 10;
            }

            lock (_signal)
            {
                try { _writer?.Flush(); }
                catch { /* 忽略刷新错误 */ }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // 唤醒后台线程退出
            _signal.Set();

            if (_workerThread != null && _workerThread.IsAlive)
            {
                try
                {
                    // 最多等待 1 秒排空写入
                    _workerThread.Join(1000);
                }
                catch { /* 忽略 Join 异常 */ }
            }

            // 处理可能残留的少量日志
            DrainRemainingLogs();

            lock (_signal)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { /* 忽略释放异常 */ }
            }

            _signal.Dispose();
        }

        // ═══════════════════════════════════════
        //  内部工作逻辑
        // ═══════════════════════════════════════

        private void InitLogFile()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // 历史归档清理
                CleanOldArchives();

                // 创建当前运行的日志文件
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"log_{timestamp}.txt";
                _currentFilePath = Path.Combine(_logDirectory, fileName);

                var fileStream = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(fileStream, new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                // 写入文件头环境信息
                _writer.WriteLine("================================================================================");
                _writer.WriteLine($"[GLog Session Start] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                _writer.WriteLine($"Platform: {Application.platform} | Unity: {Application.unityVersion}");
                _writer.WriteLine($"Device: {SystemInfo.deviceModel} | OS: {SystemInfo.operatingSystem}");
                _writer.WriteLine("================================================================================");
                _writer.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileLogHandler] Init failed: {ex.Message}");
            }
        }

        private void CleanOldArchives()
        {
            try
            {
                var files = Directory.GetFiles(_logDirectory, "log_*.txt");
                if (files.Length <= _maxArchiveFiles) return;

                // 按创建时间升序排列，删除超出数量的最早文件
                Array.Sort(files, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
                int toDelete = files.Length - _maxArchiveFiles;
                for (int i = 0; i < toDelete; i++)
                {
                    try { File.Delete(files[i]); }
                    catch { /* 忽略单个文件删除失败 */ }
                }
            }
            catch
            {
                // 忽略归档清理异常
            }
        }

        private void WriteLoop()
        {
            while (!_isDisposed)
            {
                _signal.WaitOne(500); // 每 500ms 或有新消息时唤醒

                WriteBatch();
            }

            // 退出循环后做最后一次排空
            WriteBatch();
        }

        private void WriteBatch()
        {
            if (_writer == null) return;

            bool wroteAny = false;
            while (_logQueue.TryDequeue(out string logLine))
            {
                try
                {
                    _writer.WriteLine(logLine);
                    wroteAny = true;
                }
                catch
                {
                    // 写入异常不抛出
                    break;
                }
            }

            if (wroteAny)
            {
                try { _writer.Flush(); }
                catch { /* 忽略刷新错误 */ }
            }
        }

        private void DrainRemainingLogs()
        {
            if (_writer == null) return;
            try
            {
                while (_logQueue.TryDequeue(out string logLine))
                {
                    _writer.WriteLine(logLine);
                }
                _writer.Flush();
            }
            catch { /* 忽略退出时写入错误 */ }
        }
    }
}
