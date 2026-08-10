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
            ActionTagConfigAsset config = LoadConfig();
            return ToUniqueArray(config?.availableComboWindowTags);
        }

        public static string[] GetTargetTags()
        {
            ActionTagConfigAsset config = LoadConfig();
            return ToUniqueArray(config?.availableTargetTags);
        }

        public static string[] GetEventTags()
        {
            ActionTagConfigAsset config = LoadConfig();
            return ToUniqueArray(config?.availableEventTags);
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
