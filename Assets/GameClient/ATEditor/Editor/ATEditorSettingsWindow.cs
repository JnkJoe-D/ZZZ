using UnityEngine;
using UnityEditor;
using System;

namespace ATEditor.Editor
{
    public class ATEditorSettingsWindow : EditorWindow
    {
        private ATEditorState _state;
        private Action _onSettingsChanged;

        public static void Show(ATEditorState state, Action onSettingsChanged)
        {
            var window = GetWindow<ATEditorSettingsWindow>(Lan.SettingsPanelTitle);
            window._state = state;
            window._onSettingsChanged = onSettingsChanged;
            window.minSize = new Vector2(250, 150);
            window.Show();
        }

        private void OnGUI()
        {
            if (_state == null)
            {
                EditorGUILayout.HelpBox(Lan.SettingsWarning, MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            GUILayout.Label(Lan.SettingsPrecisionLabel, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Frame Rate Selection
            EditorGUI.BeginChangeCheck();
            
            // Fixed values to match labels for now
            int[] frameRateValues = { 15, 30, 60 };
            string[] frameRateLabels = { "15 FPS", "30 FPS", "60 FPS" }; 
            
            _state.frameRate = EditorGUILayout.IntPopup(Lan.SettingsFrameRateLabel, _state.frameRate, frameRateLabels, frameRateValues);
            
            // Time Step Mode
            _state.timeStepMode = (TimeStepMode)EditorGUILayout.EnumPopup(Lan.SettingsTimeStepModeLabel, _state.timeStepMode);
            
            // Frame Snap Status (Read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle(Lan.SettingsFrameSnapLabel, _state.useFrameSnap);
            EditorGUI.EndDisabledGroup();
            
            // Magnet Snap Toggle
            _state.snapEnabled = EditorGUILayout.Toggle(Lan.SettingsSnapEnabledLabel, _state.snapEnabled);

            EditorGUILayout.Space();
            GUILayout.Label(Lan.SettingsSnapIntervalLabel + (_state.useFrameSnap ? $"{_state.SnapInterval:F4}s" : Lan.SettingsDynamicStep), EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
            {
                _onSettingsChanged?.Invoke();
            }

            EditorGUILayout.Space();
            GUILayout.Label(Lan.PreviewSpeedMultiplier, EditorStyles.boldLabel);
            _state.previewSpeedMultiplier = EditorGUILayout.Slider(_state.previewSpeedMultiplier, 0.1f, 3f);

            EditorGUILayout.Space();
            GUILayout.Label("导出/导入设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_state.DefaultExportDirectory, EditorStyles.helpBox);
            if (GUILayout.Button("选择默认目录", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("选择默认导出目录", _state.DefaultExportDirectory, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _state.DefaultExportDirectory = path;
                    _onSettingsChanged?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label(Lan.SettingsDefaultPreviewTargetLabel, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            GameObject currentPrefab = null;
            if (!string.IsNullOrEmpty(_state.DefaultPreviewCharacterPath))
            {
                currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_state.DefaultPreviewCharacterPath);
            }
            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(currentPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                _state.DefaultPreviewCharacterPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : "";
                _onSettingsChanged?.Invoke();
            }

            // Language Selection
            EditorGUI.BeginChangeCheck();

            var languages = System.Linq.Enumerable.ToArray(Lan.AllLanguages.Keys);
            int currentIndex = System.Array.IndexOf(languages, _state.Language);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(Lan.LanguageLabel, currentIndex, languages);

            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < languages.Length)
                {
                    _state.Language = languages[newIndex];
                    _onSettingsChanged?.Invoke();
                }
            }
        }
    }
}
