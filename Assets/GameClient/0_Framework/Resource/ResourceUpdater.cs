using System;
using System.Collections;
using UnityEngine;
using YooAsset;
using Game.Framework;

namespace Game.Framework
{
    /// <summary>
    /// 热更新流程管理器
    /// 
    /// 负责 HostPlayMode 下的完整更新流程：
    ///   ① 请求版本号 → ② 更新 Manifest → ③ 创建下载器 → ④ 下载文件
    /// 
    /// 通过 EventCenter 广播进度，UI 层只需监听事件即可更新进度条，
    /// 业务层调用 GameRoot.InitAssets 时等待此流程完成再继续。
    /// </summary>
    public class ResourceUpdater
    {
        private readonly ResourcePackage _package;
        private readonly ResourceConfig  _config;

        public bool IsSuccessful { get; private set; }

        public ResourceUpdater(ResourcePackage package, ResourceConfig config)
        {
            _package = package;
            _config  = config;
        }

        /// <summary>
        /// 执行完整热更流程（协程）
        /// 成功时发布 HotUpdateCompletedEvent，失败时发布 HotUpdateFailedEvent
        /// </summary>
        public IEnumerator Run()
        {
            EventCenter.Publish(new HotUpdateCheckStartEvent());
            EventCenter.Publish(new HotUpdateStatusEvent { StatusText = "正在检查版本更新...", Progress = 0.1f });

            // ── Step 1: 请求最新版本号 ────────────────────────
            var versionOp = _package.RequestPackageVersionAsync();
            yield return versionOp;

            if (versionOp.Status != EOperationStatus.Succeed)
            {
                PublishFail(HotUpdateFailReason.VersionRequestFailed, versionOp.Error);
                yield break;
            }

            var packageVersion = versionOp.PackageVersion;
            GLog.Info(LogTags.Resource, $"最新版本: {packageVersion}");

            // ── 更新远程服务版本信息，使后续 Manifest 和 Bundle 请求追加版本子目录 ────
            ResourceManager.Instance.SetRemoteVersion(packageVersion);

            // ── Step 2: 更新资源清单 ──────────────────────────
            var manifestOp = _package.UpdatePackageManifestAsync(packageVersion);
            while (!manifestOp.IsDone)
            {
                EventCenter.Publish(new HotUpdateStatusEvent { StatusText = "正在更新资源清单...", Progress = 0.2f + manifestOp.Progress * 0.8f });
                yield return null;
            }

            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                PublishFail(HotUpdateFailReason.ManifestUpdateFailed, manifestOp.Error);
                yield break;
            }

            // ── Step 3: 创建下载器，检查需要下载的文件数量 ────
            var downloader = _package.CreateResourceDownloader(
                downloadingMaxNumber: 10,
                failedTryAgain: _config.downloadRetryCount
            );

            if (downloader.TotalDownloadCount == 0)
            {
                GLog.Info(LogTags.Resource, "无需更新，资源已是最新");
                IsSuccessful = true;
                EventCenter.Publish(new HotUpdateStatusEvent { StatusText = "资源已是最新，准备就绪...", Progress = 1f });
                EventCenter.Publish(new HotUpdateCompletedEvent { HasUpdate = false });
                yield break;
            }

            // 通知 UI 发现需要更新的文件，并挂起等待用户确认
            bool userConfirmed = false;
            
            EventCenter.Publish(new HotUpdateRequireConfirmEvent
            {
                FileCount          = downloader.TotalDownloadCount,
                TotalDownloadBytes = downloader.TotalDownloadBytes,
                ConfirmAction      = () => { userConfirmed = true; }
            });

            // 挂起协程，直到 UI 层调用 ConfirmAction
            yield return new WaitUntil(() => userConfirmed);

            // ── Step 4: 开始下载 ──────────────────────────────
            downloader.DownloadErrorCallback  = OnDownloadError;
            downloader.DownloadUpdateCallback = OnDownloadProgress;
            downloader.BeginDownload();
            yield return downloader;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                PublishFail(HotUpdateFailReason.DownloadFailed, "一个或多个文件下载失败");
                yield break;
            }

            GLog.Info(LogTags.Resource, "热更完成！");
            IsSuccessful = true;
            EventCenter.Publish(new HotUpdateCompletedEvent { HasUpdate = true });
        }

        // ── 回调 ──────────────────────────────────────────────

        private void OnDownloadProgress(DownloadUpdateData data)
        {
            EventCenter.Publish(new HotUpdateProgressEvent
            {
                TotalDownloadCount   = data.TotalDownloadCount,
                CurrentDownloadCount = data.CurrentDownloadCount,
                TotalDownloadBytes   = data.TotalDownloadBytes,
                CurrentDownloadBytes = data.CurrentDownloadBytes,
            });
        }

        private void OnDownloadError(DownloadErrorData data)
        {
            GLog.Warning(LogTags.Resource, $"文件下载失败: {data.FileName} | {data.ErrorInfo}");
            // 注意：失败文件会在 failedTryAgain 次重试后才真正报错，此处仅记录日志
        }

        private static void PublishFail(HotUpdateFailReason reason, string msg)
        {
            GLog.Error(LogTags.Resource, $"热更失败 [{reason}]: {msg}");
            EventCenter.Publish(new HotUpdateFailedEvent { Reason = reason, Message = msg });
        }
    }
}
