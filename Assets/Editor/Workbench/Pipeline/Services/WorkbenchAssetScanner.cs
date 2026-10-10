using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;
using Game.GamePlay;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 资产扫描与索引服务
    /// 统一消费 EffectiveWorkbenchConfig 与 WorkbenchPathResolver，
    /// 提供角色所属 Action 资产与时间轴资产的多候选安全扫描与聚合索引构建。
    /// </summary>
    public static class WorkbenchAssetScanner
    {
        /// <summary>
        /// 扫描指定角色归属的所有 ActionConfigAsset 列表
        /// </summary>
        public static List<ActionAssetIndexItem> ScanActions(EffectiveWorkbenchConfig config, SharedWorkspaceDefinition ws)
        {
            var results = new List<ActionAssetIndexItem>();
            if (config == null || ws == null) return results;

            var visitedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. 优先使用生效配置指定的 ActionConfigDirectory
            ScanActionsInDirectory(config.ActionConfigDirectory, results, visitedGuids);

            // 2. 结合 WorkbenchPathResolver 探测其他可能存在的历史兼容候选目录
            var candidateDirs = WorkbenchPathResolver.ResolveActionConfigCandidateDirectories(ws.FolderName, ws.Category);
            foreach (var dir in candidateDirs)
            {
                if (string.Equals(dir, config.ActionConfigDirectory, StringComparison.OrdinalIgnoreCase)) continue;
                ScanActionsInDirectory(dir, results, visitedGuids);
            }

            // 按动作名称字母升序排序
            return results.OrderBy(a => a.ActionName).ToList();
        }

        /// <summary>
        /// 扫描指定角色归属的所有 Timeline 资产（JSON 与 SO）
        /// </summary>
        public static List<TimelineAssetIndexItem> ScanTimelines(EffectiveWorkbenchConfig config)
        {
            var results = new List<TimelineAssetIndexItem>();
            if (config == null) return results;

            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. 扫描 Timeline JSON 目录
            var jsonDirs = new List<string>();
            if (!string.IsNullOrEmpty(config.TimelineJsonDirectory)) jsonDirs.Add(config.TimelineJsonDirectory);

            foreach (var dir in jsonDirs)
            {
                if (Directory.Exists(dir))
                {
                    string[] jsonFiles = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var file in jsonFiles)
                    {
                        string unityPath = file.Replace('\\', '/');
                        if (visitedPaths.Add(unityPath))
                        {
                            string guid = AssetDatabase.AssetPathToGUID(unityPath);
                            results.Add(new TimelineAssetIndexItem(guid, unityPath, isJson: true));
                        }
                    }
                }
            }

            // 2. 扫描 Timeline SO 目录
            var soDirs = new List<string>();
            if (!string.IsNullOrEmpty(config.TimelineAssetDirectory)) soDirs.Add(config.TimelineAssetDirectory);

            foreach (var dir in soDirs)
            {
                if (Directory.Exists(dir))
                {
                    string[] guids = AssetDatabase.FindAssets("t:ActionTimeline", new[] { dir });
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path) && visitedPaths.Add(path))
                        {
                            results.Add(new TimelineAssetIndexItem(guid, path, isJson: false));
                        }
                    }
                }
            }

            return results.OrderBy(t => t.TimelineName).ThenBy(t => t.IsJson ? 0 : 1).ToList();
        }

        /// <summary>
        /// 为指定角色构建完整的业务上下文聚合根
        /// </summary>
        public static WorkbenchCharacterContext BuildContext(
            SharedWorkspaceDefinition ws,
            TaskOverrideParams taskParams = null)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            var effectiveConfig = EffectiveConfigResolver.Resolve(ws, taskParams);

            LubanCharacterInfo lubanInfo = null;
            if (!string.IsNullOrEmpty(effectiveConfig.LubanCharacterId))
            {
                LubanCharacterSourceAdapter.TryGetCharacterByName(effectiveConfig.LubanCharacterId, out lubanInfo);
            }

            var actions = ScanActions(effectiveConfig, ws);
            var timelines = ScanTimelines(effectiveConfig);

            return new WorkbenchCharacterContext(ws, effectiveConfig, lubanInfo, actions, timelines);
        }

        private static void ScanActionsInDirectory(
            string dir,
            List<ActionAssetIndexItem> results,
            HashSet<string> visitedGuids)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            string[] guids = AssetDatabase.FindAssets("t:ActionConfigAsset", new[] { dir });
            foreach (var guid in guids)
            {
                if (!visitedGuids.Add(guid)) continue;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(path);
                if (asset != null)
                {
                    results.Add(new ActionAssetIndexItem(guid, path, asset));
                }
            }
        }
    }
}
