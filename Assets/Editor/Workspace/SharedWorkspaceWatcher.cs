using System;
using UnityEditor;

namespace Game.Editor.Workspace
{
    /// <summary>
    /// 公共角色工作区文件变动监听与事件中心 (Shared Workspace Watcher)
    /// 当 SharedWorkspaces.json 发生导入、外部修改或保存时，广播变更事件，
    /// 驱动 ATEditor 及其他新工作台工具实现零延时双向感知。
    /// </summary>
    public class SharedWorkspaceWatcher : AssetPostprocessor
    {
        /// <summary>
        /// 当公共工作区 JSON 数据发生变动时触发
        /// </summary>
        public static event Action OnWorkspacesFileChanged;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            string targetRelPath = SharedWorkspaceFileIO.EffectiveRelativePath.Replace('\\', '/');

            bool changed = false;

            if (importedAssets != null)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    if (string.Equals(importedAssets[i].Replace('\\', '/'), targetRelPath, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed && deletedAssets != null)
            {
                for (int i = 0; i < deletedAssets.Length; i++)
                {
                    if (string.Equals(deletedAssets[i].Replace('\\', '/'), targetRelPath, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 手动或程序化广播数据变更通知
        /// </summary>
        public static void NotifyChanged()
        {
            try
            {
                OnWorkspacesFileChanged?.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SharedWorkspaceWatcher] 广播变更事件异常: {ex.Message}");
            }
        }
    }
}
