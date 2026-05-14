using System;
using System.Collections.Generic;
using Game.Logic.Action.Config;
using UnityEditor;

namespace Game.Editor.ActionConfig
{
    public static class ActionTagOptions
    {
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
            return AssetDatabase.LoadAssetAtPath<ActionTagConfigAsset>(path);
        }

        private static string[] ToUniqueArray(List<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            List<string> tags = new();
            foreach (string tag in source)
            {
                if (string.IsNullOrWhiteSpace(tag) || tags.Contains(tag))
                {
                    continue;
                }

                tags.Add(tag);
            }

            return tags.ToArray();
        }
    }
}
