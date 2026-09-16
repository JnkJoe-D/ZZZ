using System;
using System.Collections.Generic;
using UnityEditor;
using Game.GamePlay;
namespace Game.Editor.ActionConfig
{
    [InitializeOnLoad]
    public static class ActionTagOptions
    {
        private static ATEditor.ActionTagConfig _cachedConfig;
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
            if (_cachedComboWindowTags != null)
            {
                return _cachedComboWindowTags;
            }

            ATEditor.ActionTagConfig config = LoadConfig();
            _cachedComboWindowTags = ToUniqueArray(config?.availableComboWindowTags);
            return _cachedComboWindowTags;
        }

        public static string[] GetTargetTags()
        {
            if (_cachedTargetTags != null)
            {
                return _cachedTargetTags;
            }

            ATEditor.ActionTagConfig config = LoadConfig();
            _cachedTargetTags = ToUniqueArray(config?.availableTargetTags);
            return _cachedTargetTags;
        }

        public static string[] GetEventTags()
        {
            if (_cachedEventTags != null)
            {
                return _cachedEventTags;
            }

            ATEditor.ActionTagConfig config = LoadConfig();
            _cachedEventTags = ToUniqueArray(config?.availableEventTags);
            return _cachedEventTags;
        }

        private static ATEditor.ActionTagConfig LoadConfig()
        {
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
