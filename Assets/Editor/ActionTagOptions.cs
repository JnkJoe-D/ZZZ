using System;
using System.Collections.Generic;
using ATEditor;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ActionConfig
{
    [InitializeOnLoad]
    public static class ActionTagOptions
    {
        private const string DefaultConfigPath = "Assets/GameClient/Dependencies/ATEditor/Settings/ActionTagConfig.asset";

        private static ATEditor.ActionTagConfig _cachedConfig;
        private static List<RouteWindow> _cachedRouteWindows;
        private static string[] _cachedRouteWindowLabels;
        private static string[] _cachedComboWindowTags;
        private static string[] _cachedTargetTags;
        private static string[] _cachedEventTags;

        private static int _routeWindowsHash;
        private static int _targetTagsHash;
        private static int _eventTagsHash;

        static ActionTagOptions()
        {
            EditorApplication.projectChanged += InvalidateCache;
            Undo.undoRedoPerformed += InvalidateCache;
            AssemblyReloadEvents.afterAssemblyReload += InvalidateCache;
        }

        public static void InvalidateCache()
        {
            _cachedConfig = null;
            _cachedRouteWindows = null;
            _cachedRouteWindowLabels = null;
            _cachedComboWindowTags = null;
            _cachedTargetTags = null;
            _cachedEventTags = null;
            _routeWindowsHash = 0;
            _targetTagsHash = 0;
            _eventTagsHash = 0;
        }

        /// <summary>
        /// 获取配置的所有 RouteWindow 对象列表。
        /// </summary>
        public static IReadOnlyList<RouteWindow> GetRouteWindows()
        {
            UpdateRouteWindowsCacheIfNeeded();
            return _cachedRouteWindows;
        }

        /// <summary>
        /// 获取配置的所有 RouteWindow 的显示标签列表，格式为 "[RouteWindow的具体子类名称]routewindow.tag"。
        /// </summary>
        public static string[] GetRouteWindowDisplayOptions()
        {
            UpdateRouteWindowsCacheIfNeeded();
            return _cachedRouteWindowLabels;
        }

        /// <summary>
        /// 根据目标 RouteWindow 查找配置中匹配项（子类类型相同且 Tag 相同）。
        /// </summary>
        public static RouteWindow FindMatchingWindow(RouteWindow target)
        {
            if (target == null) return null;
            var list = GetRouteWindows();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Matches(target))
                {
                    return list[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 根据 Tag 查找配置中的第一个匹配 RouteWindow 项。
        /// </summary>
        public static RouteWindow FindMatchingWindowByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            var list = GetRouteWindows();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].Tag, tag, StringComparison.Ordinal))
                {
                    return list[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有已配置窗口的 Tag 字符串数组。
        /// </summary>
        public static string[] GetComboWindowTags()
        {
            UpdateRouteWindowsCacheIfNeeded();
            return _cachedComboWindowTags;
        }

        public static string[] GetTargetTags()
        {
            ATEditor.ActionTagConfig config = LoadConfig();
            int currentHash = ComputeListHash(config?.availableTargetTags);

            if (_cachedTargetTags != null && currentHash == _targetTagsHash)
            {
                return _cachedTargetTags;
            }

            _targetTagsHash = currentHash;
            _cachedTargetTags = ToUniqueArray(config?.availableTargetTags);
            return _cachedTargetTags;
        }

        public static string[] GetEventTags()
        {
            ATEditor.ActionTagConfig config = LoadConfig();
            int currentHash = ComputeListHash(config?.availableEventTags);

            if (_cachedEventTags != null && currentHash == _eventTagsHash)
            {
                return _cachedEventTags;
            }

            _eventTagsHash = currentHash;
            _cachedEventTags = ToUniqueArray(config?.availableEventTags);
            return _cachedEventTags;
        }

        public static ATEditor.ActionTagConfig LoadConfig()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            _cachedConfig = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTagConfig>(DefaultConfigPath);
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            string[] guids = AssetDatabase.FindAssets("t:ActionTagConfig");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:ActionTagConfigAsset");
            }

            if (guids.Length == 0)
            {
                return null;
            }

            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _cachedConfig = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTagConfig>(path);
                if (_cachedConfig != null)
                {
                    break;
                }
            }

            return _cachedConfig;
        }

        private static void UpdateRouteWindowsCacheIfNeeded()
        {
            ATEditor.ActionTagConfig config = LoadConfig();
            int currentHash = ComputeRouteWindowsHash(config?.availableRouteWindows);

            if (_cachedRouteWindows != null && currentHash == _routeWindowsHash)
            {
                return;
            }

            _routeWindowsHash = currentHash;

            var sourceList = config?.availableRouteWindows;
            if (sourceList == null || sourceList.Count == 0)
            {
                _cachedRouteWindows = new List<RouteWindow>();
                _cachedRouteWindowLabels = Array.Empty<string>();
                _cachedComboWindowTags = Array.Empty<string>();
                return;
            }

            _cachedRouteWindows = new List<RouteWindow>(sourceList.Count);
            List<string> labels = new List<string>(sourceList.Count);
            List<string> tags = new List<string>(sourceList.Count);

            for (int i = 0; i < sourceList.Count; i++)
            {
                RouteWindow window = sourceList[i];
                if (window == null || string.IsNullOrWhiteSpace(window.Tag)) continue;

                _cachedRouteWindows.Add(window);
                labels.Add(window.EditorLabel); // 格式为 "[RouteWindow的具体子类名称]routewindow.tag"

                if (!tags.Contains(window.Tag))
                {
                    tags.Add(window.Tag);
                }
            }

            _cachedRouteWindowLabels = labels.ToArray();
            _cachedComboWindowTags = tags.ToArray();
        }

        private static int ComputeRouteWindowsHash(List<RouteWindow> list)
        {
            if (list == null) return 0;
            unchecked
            {
                int hash = (list.Count * 397) ^ 19;
                for (int i = 0; i < list.Count; i++)
                {
                    RouteWindow item = list[i];
                    if (item != null)
                    {
                        hash = (hash * 31) ^ item.GetType().GetHashCode();
                        hash = (hash * 31) ^ (item.Tag != null ? StringComparer.Ordinal.GetHashCode(item.Tag) : 0);
                    }
                }
                return hash;
            }
        }

        private static int ComputeListHash(List<string> list)
        {
            if (list == null) return 0;
            unchecked
            {
                int hash = (list.Count * 397) ^ 17;
                for (int i = 0; i < list.Count; i++)
                {
                    string item = list[i];
                    hash = (hash * 31) ^ (item != null ? item.GetHashCode() : 0);
                }
                return hash;
            }
        }

        private static string[] ToUniqueArray(List<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> tags = new(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                string tag = source[i];
                if (!string.IsNullOrWhiteSpace(tag) && !tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags.ToArray();
        }
    }

    /// <summary>
    /// 资产导入处理器：当 ActionTagConfig 在磁盘上被保存、修改或外部拉取时，立即清除缓存。
    /// </summary>
    internal sealed class ActionTagConfigPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i];
                if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                    (path.IndexOf("ActionTag", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     path.IndexOf("TagConfig", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    ActionTagOptions.InvalidateCache();
                    return;
                }
            }

            for (int i = 0; i < deletedAssets.Length; i++)
            {
                string path = deletedAssets[i];
                if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                    (path.IndexOf("ActionTag", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     path.IndexOf("TagConfig", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    ActionTagOptions.InvalidateCache();
                    return;
                }
            }
        }
    }
}
