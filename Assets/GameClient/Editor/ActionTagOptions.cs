using System;
using System.Collections.Generic;
using Game.Logic;
using UnityEditor;

namespace Game.Editor.ActionConfig
{
    [InitializeOnLoad]
    public static class ActionTagOptions
    {
        private static ActionTagConfigAsset _cachedConfig;
        private static string[] _cachedComboWindowTags;
        private static string[] _cachedTargetTags;
        private static string[] _cachedEventTags;

        static ActionTagOptions()
        {
            EditorApplication.projectChanged += InvalidateCache;
        }

        public static void InvalidateCache()
        {
            _cachedConfig = null;
            _cachedComboWindowTags = null;
            _cachedTargetTags = null;
            _cachedEventTags = null;
        }

        public static string[] GetComboWindowTags()
        {
            if (_cachedComboWindowTags != null && _cachedConfig != null)
            {
                return _cachedComboWindowTags;
            }

            ActionTagConfigAsset config = LoadConfig();
            _cachedComboWindowTags = ToUniqueArray(config?.availableComboWindowTags);
            return _cachedComboWindowTags;
        }

        public static string[] GetTargetTags()
        {
            if (_cachedTargetTags != null && _cachedConfig != null)
            {
                return _cachedTargetTags;
            }

            ActionTagConfigAsset config = LoadConfig();
            _cachedTargetTags = ToUniqueArray(config?.availableTargetTags);
            return _cachedTargetTags;
        }

        public static string[] GetEventTags()
        {
            if (_cachedEventTags != null && _cachedConfig != null)
            {
                return _cachedEventTags;
            }

            ActionTagConfigAsset config = LoadConfig();
            _cachedEventTags = ToUniqueArray(config?.availableEventTags);
            return _cachedEventTags;
        }

        private static ActionTagConfigAsset LoadConfig()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            string[] guids = AssetDatabase.FindAssets("t:ActionTagConfigAsset");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:SkillTagConfig");
            }

            if (guids.Length == 0)
            {
                return null;
            }

            Array.Sort(guids, StringComparer.Ordinal);
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _cachedConfig = AssetDatabase.LoadAssetAtPath<ActionTagConfigAsset>(path);
            return _cachedConfig;
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
}
