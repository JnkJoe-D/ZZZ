using UnityEngine;
using Game.Framework;

namespace Game.Framework
{
    /// <summary>
    /// 热更新进度日志记录器
    /// 挂在 GameRoot 上，用于在没有 UI 的情况下通过 Console 观察热更流程
    /// 验证通过后可以删除
    /// </summary>
    public class HotUpdateConsoleLogger : MonoBehaviour
    {
        private void Start()
        {
            EventCenter.Subscribe<HotUpdateCheckStartEvent>(OnCheckStart);
            EventCenter.Subscribe<HotUpdateRequireConfirmEvent>(OnFoundUpdate);
            EventCenter.Subscribe<HotUpdateProgressEvent>(OnProgress);
            EventCenter.Subscribe<HotUpdateCompletedEvent>(OnCompleted);
            EventCenter.Subscribe<HotUpdateFailedEvent>(OnFailed);
            EventCenter.Subscribe<ResourceInitializedEvent>(OnInited);
        }

        private void OnDestroy()
        {
            EventCenter.Unsubscribe<HotUpdateCheckStartEvent>(OnCheckStart);
            EventCenter.Unsubscribe<HotUpdateRequireConfirmEvent>(OnFoundUpdate);
            EventCenter.Unsubscribe<HotUpdateProgressEvent>(OnProgress);
            EventCenter.Unsubscribe<HotUpdateCompletedEvent>(OnCompleted);
            EventCenter.Unsubscribe<HotUpdateFailedEvent>(OnFailed);
            EventCenter.Unsubscribe<ResourceInitializedEvent>(OnInited);
        }

        private void OnCheckStart(HotUpdateCheckStartEvent e)
        {
            GLog.Info(LogTags.Resource, "开始检查资源更新...");
        }

        private void OnFoundUpdate(HotUpdateRequireConfirmEvent e)
        {
            GLog.Info(LogTags.Resource, $"发现新资源！文件数: {e.FileCount}, 总大小: {e.TotalDownloadBytes / 1024f / 1024f:F2} MB");
        }

        private void OnProgress(HotUpdateProgressEvent e)
        {
            GLog.Info(LogTags.Resource, $"下载进度: {e.Progress * 100:F1}% ({e.CurrentDownloadCount}/{e.TotalDownloadCount})");
        }

        private void OnCompleted(HotUpdateCompletedEvent e)
        {
            GLog.Info(LogTags.Resource, $"热更新完成！是否有更新: {e.HasUpdate}");
        }

        private void OnFailed(HotUpdateFailedEvent e)
        {
            GLog.Error(LogTags.Resource, $"热更新失败！原因: {e.Reason}, 详情: {e.Message}");
        }

        private void OnInited(ResourceInitializedEvent e)
        {
            GLog.Info(LogTags.Resource, "资源系统加载就绪，可以开始游戏逻辑。");
        }
    }
}
