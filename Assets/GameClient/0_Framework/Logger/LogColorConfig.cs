using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Framework
{
    /// <summary>
    /// 日志模块标签颜色配置与预烘焙缓存管理器。
    /// 
    /// 核心特性：
    /// 1. 预烘焙缓存（Pre-baked Cache）：所有富文本颜色标签提前格式化为字符串常驻内存，
    ///    UnityLogHandler 每帧调用时仅做一次只读 Dictionary 查表，零 GC 堆分配。
    /// 2. 默认专业色板：为 LogTags 常量类中所有核心标签提供符合暗黑与明亮控制台的高对比度配色。
    /// 3. 本地偏好持久化：在编辑器下自定义的颜色保存在 EditorPrefs 中，不污染项目资产文件。
    /// 4. 独立性与兜底：若某标签未配置颜色，平滑回退至纯文本 [Tag] 格式。
    /// </summary>
    public static class LogColorConfig
    {
        private const string PrefsPrefix = "GLog_TagColor_";

        // 默认配色字典 (Tag -> Hex)
        private static readonly Dictionary<string, string> _defaultColors = new Dictionary<string, string>
        {
            // ── 框架层 (冷调青绿/灰蓝/皇家蓝) ──
            [LogTags.Framework]    = "#868E96", // 灰蓝
            [LogTags.GameRoot]     = "#4DABF7", // 浅天蓝
            [LogTags.Resource]     = "#20C997", // 碧绿
            [LogTags.Scene]        = "#0CA678", // 森林绿
            [LogTags.Pool]         = "#63E6BE", // 薄荷绿
            [LogTags.FSM]          = "#339AF0", // 亮蓝
            [LogTags.Event]        = "#748FFC", // 靛蓝
            [LogTags.Config]       = "#38D9A9", // 水绿
            [LogTags.Audio]        = "#A9E34B", // 黄绿

            // ── 网络层 (科技蓝/深青) ──
            [LogTags.Network]      = "#22B8CF", // 亮青
            [LogTags.Tcp]          = "#1C7ED6", // 深蓝
            [LogTags.Udp]          = "#15AABF", // 湖蓝
            [LogTags.Heartbeat]    = "#1098AD", // 深青

            // ── 玩法层 (暖色红/橙/洋红/鲜绿) ──
            [LogTags.Combat]       = "#FF6B6B", // 珊瑚红
            [LogTags.Action]       = "#FF922B", // 活力橙
            [LogTags.Input]        = "#FCC419", // 金黄
            [LogTags.Skill]        = "#FA5252", // 鲜红
            [LogTags.Buff]         = "#E64980", // 洋红
            [LogTags.AI]           = "#BE4BDB", // 浅紫
            [LogTags.Team]         = "#FD7E14", // 橙黄
            [LogTags.Monster]      = "#E03131", // 暗红
            [LogTags.Player]       = "#51CF66", // 鲜绿

            // ── 表现层 (暖黄/丁香紫/亮粉) ──
            [LogTags.UI]           = "#FFD43B", // 明黄
            [LogTags.Camera]       = "#DA77F2", // 浅紫罗兰
            [LogTags.VFX]          = "#F783AC", // 亮粉
            [LogTags.Anim]         = "#E599F7", // 丁香紫

            // ── 编辑器工具 ──
            [LogTags.ATEditor]     = "#ADB5BD", // 银灰
            [LogTags.BehaviorTree] = "#9775FA", // 淡紫
        };

        // 运行时预烘焙缓存 (Tag -> "<color=#RRGGBB>[Tag]</color>")
        private static readonly Dictionary<string, string> _prebakedCache = new Dictionary<string, string>(64);
        private static readonly object _lock = new object();

        static LogColorConfig()
        {
            RebuildAllCache();
        }

        /// <summary>
        /// 获取指定标签的着色显示字符串。
        /// 内部返回已预先烘焙的常量字符串（如 "<color=#FF6B6B>[Combat]</color>"），零 GC 开销。
        /// </summary>
        public static string GetColoredTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return string.Empty;

            lock (_lock)
            {
                if (_prebakedCache.TryGetValue(tag, out var baked))
                {
                    return baked;
                }

                // 首次遇到的自定义字符串标签：生成默认无色并缓存
                string fallback = $"[{tag}]";
                _prebakedCache[tag] = fallback;
                return fallback;
            }
        }

        /// <summary>
        /// 获取指定标签当前生效的 Color 对象。
        /// </summary>
        public static Color GetTagColor(string tag)
        {
            string hex = GetTagColorHex(tag);
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                return c;
            }
            return Color.white;
        }

        /// <summary>
        /// 获取指定标签的十六进制颜色字符串（如 "#FF6B6B"）。
        /// </summary>
        public static string GetTagColorHex(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "#FFFFFF";

#if UNITY_EDITOR
            string customHex = EditorPrefs.GetString(PrefsPrefix + tag, null);
            if (!string.IsNullOrEmpty(customHex))
            {
                return customHex;
            }
#endif

            if (_defaultColors.TryGetValue(tag, out var defaultHex))
            {
                return defaultHex;
            }

            return "#FFFFFF";
        }

        /// <summary>
        /// 获取指定标签内置的默认颜色。
        /// </summary>
        public static Color GetDefaultColor(string tag)
        {
            if (_defaultColors.TryGetValue(tag, out var hex) &&
                ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                return c;
            }
            return Color.white;
        }

        /// <summary>
        /// 检查指定标签是否被用户自定义修改过颜色。
        /// </summary>
        public static bool HasCustomColor(string tag)
        {
#if UNITY_EDITOR
            return EditorPrefs.HasKey(PrefsPrefix + tag);
#else
            return false;
#endif
        }

        /// <summary>
        /// 设置指定标签的颜色。修改将即时更新预烘焙缓存，并在编辑器下保存到 EditorPrefs。
        /// </summary>
        public static void SetTagColor(string tag, Color color)
        {
            if (string.IsNullOrEmpty(tag)) return;

            string hex = "#" + ColorUtility.ToHtmlStringRGB(color);

#if UNITY_EDITOR
            EditorPrefs.SetString(PrefsPrefix + tag, hex);
#endif

            BakeTag(tag, hex);
        }

        /// <summary>
        /// 将指定标签的颜色重置回默认配置。
        /// </summary>
        public static void ResetTagColor(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

#if UNITY_EDITOR
            EditorPrefs.DeleteKey(PrefsPrefix + tag);
#endif

            string defaultHex = _defaultColors.TryGetValue(tag, out var hex) ? hex : "#FFFFFF";
            BakeTag(tag, defaultHex);
        }

        /// <summary>
        /// 将所有标签颜色重置回默认配置。
        /// </summary>
        public static void ResetAllToDefault()
        {
#if UNITY_EDITOR
            foreach (var kv in _defaultColors)
            {
                EditorPrefs.DeleteKey(PrefsPrefix + kv.Key);
            }
#endif
            RebuildAllCache();
        }

        // ═══════════════════════════════════════
        //  内部烘焙
        // ═══════════════════════════════════════

        private static void RebuildAllCache()
        {
            lock (_lock)
            {
                _prebakedCache.Clear();
                foreach (var kv in _defaultColors)
                {
                    string tag = kv.Key;
                    string hex = GetTagColorHex(tag);
                    _prebakedCache[tag] = $"<color={hex}>[{tag}]</color>";
                }
            }
        }

        private static void BakeTag(string tag, string hex)
        {
            lock (_lock)
            {
                _prebakedCache[tag] = $"<color={hex}>[{tag}]</color>";
            }
        }
    }
}
