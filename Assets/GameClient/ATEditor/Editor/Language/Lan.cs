using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    public static class Lan
    {
        public static readonly Dictionary<string, Type> AllLanguages = new Dictionary<string, Type>();
        private static string _lan;
        private const string PREF_KEY = "SkillEditor_Language";
        private const string DEFAULT_LAN = "简体中文";

        // Load method to be called on Window Enable
        public static void Load()
        {
            AllLanguages.Clear();
            
            // Find all implementations of ILanguages
            var type = typeof(ILanguages);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var t in types)
            {
                var nameAtt = (NameAttribute)Attribute.GetCustomAttribute(t, typeof(NameAttribute));
                var name = nameAtt != null ? nameAtt.Name : t.Name;
                AllLanguages[name] = t;
            }

            // Load saved language or default
            _lan = EditorPrefs.GetString(PREF_KEY, DEFAULT_LAN);
            
            // Should fallback if saved language is not found?
            if (!AllLanguages.ContainsKey(_lan))
            {
                if (AllLanguages.ContainsKey(DEFAULT_LAN)) _lan = DEFAULT_LAN;
                else if (AllLanguages.Count > 0) _lan = AllLanguages.Keys.First();
            }

            RefreshLanguage();
        }

        public static string CurrentLanguage => _lan;

        public static void SetLanguage(string key)
        {
            if (AllLanguages.ContainsKey(key))
            {
                _lan = key;
                EditorPrefs.SetString(PREF_KEY, key);
                RefreshLanguage();
            }
        }

        private static void RefreshLanguage()
        {
            if (string.IsNullOrEmpty(_lan) || !AllLanguages.TryGetValue(_lan, out var type)) return;

            // Get all static fields from the target language class
            var srcFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var srcDict = new Dictionary<string, string>();
            foreach (var f in srcFields)
            {
                var val = f.GetValue(null);
                if (val != null) srcDict[f.Name] = val.ToString();
            }

            // Update static fields in Lan class
            var destFields = typeof(Lan).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in destFields)
            {
                if (srcDict.TryGetValue(f.Name, out var val))
                {
                    f.SetValue(null, val);
                }
            }
        }

        // ================== Localized Fields ==================
        // These fields will be overwritten by RefreshLanguage()
        
        public static string ImportFromJson = "Import JSON";
        public static string ExportToJson = "Export JSON";
        public static string Save = "Save";
        public static string Settings = "Settings";
        
        public static string ImportPanelTitle = "Import Skill Config";
        public static string ExportPanelTitle = "Export Skill Config";
        
        public static string SettingsPanelTitle = "Settings";
        public static string SettingsWarning = "No valid Skill Editor state found";
        
        public static string SettingsPrecisionLabel = "Precision & Frame Control";
        public static string SettingsFrameRateLabel = "Logic Frame Rate";
        public static string SettingsTimeStepModeLabel = "Time Step Mode";
        public static string SettingsFrameSnapLabel = "Force Frame Snap (Auto)";
        public static string SettingsSnapEnabledLabel = "Enable Magnetic Snap";
        public static string SettingsSnapIntervalLabel = "Current Snap Interval: ";
        public static string SettingsDynamicStep = "Dynamic";
        public static string SettingsDefaultPreviewTargetLabel = "Default Preview Target";
        public static string PreviewSpeedMultiplier = "Preview Speed Multiplier";

        public static string AddTrackMenuItem = "AddTrack";

        public static string LanguageLabel = "Language";

        // Toolbar
        public static string Play = "Play";
        public static string Pause = "Pause";
        public static string StopTooltip = "Stop and Reset";
        public static string Zoom = "Zoom";
        public static string EditorTitle = "Skill Editor";
        public static string PreviewTarget = "Preview Target";
        public static string CreateDefaultCharacter = "Create Default";
        // Warning
        public static string PreviewTargetWarning = "[ATEditor] Please set a preview target to enable timeline seeking.";
    }
}
