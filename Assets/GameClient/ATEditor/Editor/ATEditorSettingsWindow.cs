using UnityEngine;
using UnityEditor;
using System;
using System.IO;

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
            
            // Asset Directory
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SO 保存目录:", GUILayout.Width(80));
            EditorGUILayout.LabelField(_state.DefaultAssetDirectory, EditorStyles.helpBox);
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                string defaultPath = !string.IsNullOrEmpty(_state.DefaultAssetDirectory) ? _state.DefaultAssetDirectory : Application.dataPath;
                string path = EditorUtility.OpenFolderPanel("选择默认 Asset 保存目录", defaultPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _state.DefaultAssetDirectory = path;
                    _onSettingsChanged?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Json Directory
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("JSON 目录:", GUILayout.Width(80));
            EditorGUILayout.LabelField(_state.DefaultJsonDirectory, EditorStyles.helpBox);
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                string defaultPath = !string.IsNullOrEmpty(_state.DefaultJsonDirectory) ? _state.DefaultJsonDirectory : Application.dataPath;
                string path = EditorUtility.OpenFolderPanel("选择默认 JSON 保存目录", defaultPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _state.DefaultJsonDirectory = path;
                    _onSettingsChanged?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label("数据同步 (双轨存储)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("一键同步 JSON -> SO", GUILayout.Height(25)))
            {
                BatchSyncJsonToSO();
            }
            if (GUILayout.Button("一键同步 SO -> JSON", GUILayout.Height(25)))
            {
                BatchSyncSOToJson();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("一键同步会自动检索源目录中的文件，并在目标目录中更新或创建对应文件，不涉及删除，绝对安全。", MessageType.Info);

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

        private void BatchSyncJsonToSO()
        {
            if (string.IsNullOrWhiteSpace(_state.DefaultJsonDirectory) || string.IsNullOrWhiteSpace(_state.DefaultAssetDirectory))
            {
                EditorUtility.DisplayDialog("同步失败", "请先配置 JSON 和 SO 的保存目录。", "确定");
                return;
            }

            string jsonDir = _state.DefaultJsonDirectory.Trim();
            string assetDir = _state.DefaultAssetDirectory.Trim();

            if (!Directory.Exists(jsonDir)) Directory.CreateDirectory(jsonDir);
            if (!Directory.Exists(assetDir)) Directory.CreateDirectory(assetDir);

            string[] jsonFiles = Directory.GetFiles(jsonDir, "*.json");
            if (jsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 JSON 文件。", "确定");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("确认同步", $"即将读取 {jsonDir} 下的 {jsonFiles.Length} 个 JSON 文件，并在 {assetDir} 生成或更新 SO 资产。\n这不会删除任何文件，是否继续？", "确认同步", "取消");
            if (!confirm) return;

            int count = 0;
            try
            {
                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("同步中", $"正在同步 {Path.GetFileName(jsonFiles[i])}", (float)i / jsonFiles.Length);
                    ActionTimeline timeline = SerializationUtility.ImportFromJsonPath(jsonFiles[i]);
                    if (timeline != null)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(jsonFiles[i]);
                        string assetPath = Path.Combine(assetDir, fileName + ".asset").Replace("\\", "/");
                        SerializationUtility.ExportToSO(timeline, assetPath);
                        count++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("同步完成", $"成功同步 {count}/{jsonFiles.Length} 个文件至 SO 目录！", "好的");
            }
        }

        private void BatchSyncSOToJson()
        {
            if (string.IsNullOrWhiteSpace(_state.DefaultJsonDirectory) || string.IsNullOrWhiteSpace(_state.DefaultAssetDirectory))
            {
                EditorUtility.DisplayDialog("同步失败", "请先配置 JSON 和 SO 的保存目录。", "确定");
                return;
            }

            string jsonDir = _state.DefaultJsonDirectory.Trim();
            string assetDir = _state.DefaultAssetDirectory.Trim();

            if (!Directory.Exists(jsonDir)) Directory.CreateDirectory(jsonDir);
            if (!Directory.Exists(assetDir)) Directory.CreateDirectory(assetDir);

            string[] assetFiles = Directory.GetFiles(assetDir, "*.asset");
            if (assetFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 SO 资产文件。", "确定");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("确认同步", $"即将读取 {assetDir} 下的 {assetFiles.Length} 个 SO 资产，并在 {jsonDir} 生成或更新 JSON 文件。\n这不会删除任何文件，是否继续？", "确认同步", "取消");
            if (!confirm) return;

            int count = 0;
            try
            {
                for (int i = 0; i < assetFiles.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("同步中", $"正在同步 {Path.GetFileName(assetFiles[i])}", (float)i / assetFiles.Length);
                    // 为了跨平台，确保路径在 AssetDatabase 中可用
                    string relativePath = assetFiles[i].Replace("\\", "/");
                    if (relativePath.StartsWith(Application.dataPath)) {
                        relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                    }
                    ActionTimeline timeline = AssetDatabase.LoadAssetAtPath<ActionTimeline>(relativePath);
                    
                    if (timeline != null)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assetFiles[i]);
                        string jsonPath = Path.Combine(jsonDir, fileName + ".json").Replace("\\", "/");
                        SerializationUtility.ExportToJson(timeline, jsonPath);
                        count++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("同步完成", $"成功同步 {count}/{assetFiles.Length} 个文件至 JSON 目录！", "好的");
            }
        }
    }
}
