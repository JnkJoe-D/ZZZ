using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace ATEditor.Editor
{
    public class JsonFileSelectionWindow : EditorWindow
    {
        private string searchString = "";
        private string[] allJsonPaths;
        private List<string> filteredPaths = new List<string>();
        private string preferredSelectedPath;
        
        private Action<string> onFileSelected;
        private Vector2 scrollPos;
        private int selectedIndex = -1;

        public static void Show(string directory, Action<string> onSelected, string initialSelectedPath = null)
        {
            var window = GetWindow<JsonFileSelectionWindow>(true, "Select JSON", true);
            window.minSize = new Vector2(350, 450);
            window.titleContent = new GUIContent("Select JSON");
            window.onFileSelected = onSelected;
            window.preferredSelectedPath = NormalizePath(initialSelectedPath);
            window.LoadFiles(directory);
            window.ShowUtility();
        }

        private void LoadFiles(string directory)
        {
            if (Directory.Exists(directory))
            {
                allJsonPaths = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            }
            else
            {
                allJsonPaths = new string[0];
            }
            FilterFiles();
        }

        private void FilterFiles()
        {
            string previouslySelectedPath = GetSelectedPath();
            filteredPaths.Clear();
            foreach (var path in allJsonPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(searchString) || fileName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredPaths.Add(path);
                }
            }

            if (!TrySelectPath(previouslySelectedPath))
            {
                TrySelectPath(preferredSelectedPath);
            }
        }

        private void OnGUI()
        {
            HandleKeyboard();

            // 1. Top Search Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.FindStyle("ToolbarSeachTextField");
            if (searchStyle == null) searchStyle = EditorStyles.textField;
            searchString = GUILayout.TextField(searchString, searchStyle);
            if (EditorGUI.EndChangeCheck())
            {
                FilterFiles();
            }
            
            GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton");
            if (cancelStyle == null) cancelStyle = EditorStyles.miniButton;
            if (GUILayout.Button("", cancelStyle))
            {
                searchString = "";
                FilterFiles();
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 2. Mock Tabs (Assets / Scene)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(true, "Files", EditorStyles.toolbarButton, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 3. List Area
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            for (int i = 0; i < filteredPaths.Count; i++)
            {
                string filePath = filteredPaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                Rect rowRect = EditorGUILayout.GetControlRect(false, 16);
                
                // Draw selection highlight
                if (i == selectedIndex)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0.17f, 0.36f, 0.53f)); // Unity's selection blue
                }

                // Handle Mouse Events
                Event e = Event.current;
                if (e.type == EventType.MouseDown && rowRect.Contains(e.mousePosition))
                {
                    selectedIndex = i;
                    if (e.clickCount == 2)
                    {
                        ConfirmSelection();
                    }
                    else
                    {
                        Repaint();
                    }
                }

                // Draw Text & Icon
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(2, 0, 0, 0) };
                if (i == selectedIndex) labelStyle.normal.textColor = Color.white;
                
                GUIContent content = EditorGUIUtility.IconContent("TextAsset Icon");
                if (content != null && content.image != null)
                {
                    content.text = "  " + fileName;
                    GUI.Label(rowRect, content, labelStyle);
                }
                else
                {
                    GUI.Label(rowRect, fileName, labelStyle);
                }
            }
            EditorGUILayout.EndScrollView();

            // 4. Bottom Info Bar
            EditorGUILayout.BeginHorizontal("box");
            if (selectedIndex >= 0 && selectedIndex < filteredPaths.Count)
            {
                string selPath = filteredPaths[selectedIndex];
                GUILayout.Label(Path.GetFileNameWithoutExtension(selPath), GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                // 简化路径以便显示，例如将 D:/... 转为 Assets/... 或者是直接显示
                string displayPath = selPath.Replace("\\", "/");
                GUILayout.Label(displayPath, pathStyle);
            }
            else
            {
                GUILayout.Label("None");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void HandleKeyboard()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.DownArrow)
                {
                    selectedIndex = Mathf.Min(selectedIndex + 1, filteredPaths.Count - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    selectedIndex = Mathf.Max(selectedIndex - 0, 0);
                    if (selectedIndex > 0) selectedIndex--; // Fix math
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    ConfirmSelection();
                    e.Use();
                }
            }
        }

        private void ConfirmSelection()
        {
            if (selectedIndex >= 0 && selectedIndex < filteredPaths.Count)
            {
                onFileSelected?.Invoke(filteredPaths[selectedIndex]);
                Close();
            }
        }

        private string GetSelectedPath()
        {
            if (selectedIndex < 0 || selectedIndex >= filteredPaths.Count)
            {
                return null;
            }

            return filteredPaths[selectedIndex];
        }

        private bool TrySelectPath(string path)
        {
            selectedIndex = -1;
            string normalizedTargetPath = NormalizePath(path);
            if (string.IsNullOrEmpty(normalizedTargetPath))
            {
                return false;
            }

            for (int i = 0; i < filteredPaths.Count; i++)
            {
                if (string.Equals(NormalizePath(filteredPaths[i]), normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(path).Replace('\\', '/');
            }
            catch
            {
                return path.Replace('\\', '/');
            }
        }
    }
}
