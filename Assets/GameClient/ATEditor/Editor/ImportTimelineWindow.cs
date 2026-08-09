using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace ATEditor.Editor
{
    public class ImportTimelineWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabs = new string[] { "从 Asset(SO) 导入", "从 JSON 导入" };

        private string searchString = "";
        private string[] allJsonPaths = new string[0];
        private string[] allSOPaths = new string[0];
        private List<string> filteredJsonPaths = new List<string>();
        private List<string> filteredSOPaths = new List<string>();

        private string preferredSelectedFileName;
        
        private Action<ActionTimeline, string> onTimelineSelected;
        private Vector2 scrollPos;
        private int selectedIndex = -1;
        private int lastHoveredIndex = -1;
        private bool needsScrollToSelection = true;
        private int previousTab = -1;

        private static readonly Color SelectedColor = new Color(0.17f, 0.36f, 0.53f, 1f);
        private static readonly Color SelectedHoverColor = new Color(0.22f, 0.44f, 0.65f, 1f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color HoverAccentColor = new Color(0.35f, 0.65f, 1f, 0.35f);

        private GUIStyle normalLabelStyle;
        private GUIStyle selectedLabelStyle;

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void EnsureStyles()
        {
            if (normalLabelStyle == null)
            {
                normalLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(4, 0, 0, 0)
                };
            }
            if (selectedLabelStyle == null)
            {
                selectedLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(4, 0, 0, 0),
                    normal = { textColor = Color.white }
                };
            }
        }

        public static void Show(string soDir, string jsonDir, Action<ActionTimeline, string> onSelected, string initialSelectedPath = null)
        {
            var window = GetWindow<ImportTimelineWindow>(true, "导入 ActionTimeline", true);
            window.minSize = new Vector2(350, 450);
            window.titleContent = new GUIContent("Import Timeline");
            window.onTimelineSelected = onSelected;
            
            if (!string.IsNullOrEmpty(initialSelectedPath))
            {
                window.preferredSelectedFileName = Path.GetFileNameWithoutExtension(initialSelectedPath);
                window.selectedTab = initialSelectedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
            else
            {
                window.preferredSelectedFileName = null;
            }

            window.needsScrollToSelection = true;
            window.LoadFiles(soDir, jsonDir);
            window.ShowUtility();
        }

        private void LoadFiles(string soDir, string jsonDir)
        {
            if (Directory.Exists(soDir))
            {
                allSOPaths = Directory.GetFiles(soDir, "*.asset", SearchOption.TopDirectoryOnly);
            }
            if (Directory.Exists(jsonDir))
            {
                allJsonPaths = Directory.GetFiles(jsonDir, "*.json", SearchOption.TopDirectoryOnly);
            }
            FilterFiles();
        }

        private void FilterFiles()
        {
            // 保存当前的选中名字，用于跨 Tab 同步
            List<string> currentFiltered = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            if (selectedIndex >= 0 && selectedIndex < currentFiltered.Count)
            {
                preferredSelectedFileName = Path.GetFileNameWithoutExtension(currentFiltered[selectedIndex]);
            }

            filteredSOPaths.Clear();
            foreach (var path in allSOPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(searchString) || fileName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredSOPaths.Add(path);
                }
            }

            filteredJsonPaths.Clear();
            foreach (var path in allJsonPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(searchString) || fileName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredJsonPaths.Add(path);
                }
            }

            TrySelectFileName(preferredSelectedFileName);
        }

        private void OnGUI()
        {
            int currentTab = GUILayout.Toolbar(selectedTab, tabs);
            if (currentTab != selectedTab)
            {
                // Tab 切换时同步选中项的名字，以实现跨类型高亮定位
                List<string> oldFiltered = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
                if (selectedIndex >= 0 && selectedIndex < oldFiltered.Count)
                {
                    preferredSelectedFileName = Path.GetFileNameWithoutExtension(oldFiltered[selectedIndex]);
                }
                
                selectedTab = currentTab;
                needsScrollToSelection = true;
                TrySelectFileName(preferredSelectedFileName);
                GUI.FocusControl(null); // 取消搜索框焦点等
            }
            EditorGUILayout.Space();

            DrawCommonListArea();
        }

        private void DrawCommonListArea()
        {
            List<string> activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            
            HandleKeyboard(activePaths);

            // 1. Top Search Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.FindStyle("ToolbarSeachTextField");
            if (searchStyle == null) searchStyle = EditorStyles.textField;
            searchString = GUILayout.TextField(searchString, searchStyle);
            if (EditorGUI.EndChangeCheck())
            {
                FilterFiles();
                activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            }
            
            GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton");
            if (cancelStyle == null) cancelStyle = EditorStyles.miniButton;
            if (GUILayout.Button("", cancelStyle))
            {
                searchString = "";
                FilterFiles();
                activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 2. Mock Tabs (Assets / Scene)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(true, "Files", EditorStyles.toolbarButton, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EnsureStyles();

            // 3. List Area
            if (needsScrollToSelection && selectedIndex >= 0 && activePaths.Count > 0)
            {
                float itemY = selectedIndex * 18;
                float scrollViewHeight = position.height - 80;
                scrollPos.y = Mathf.Max(0, itemY - scrollViewHeight / 2);
                needsScrollToSelection = false;
            }
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            int currentHoveredIndex = -1;
            Vector2 mousePos = Event.current.mousePosition;

            for (int i = 0; i < activePaths.Count; i++)
            {
                string filePath = activePaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                Rect rowRect = EditorGUILayout.GetControlRect(false, 18);
                bool isHovered = rowRect.Contains(mousePos);
                if (isHovered)
                {
                    currentHoveredIndex = i;
                }
                
                // Draw selection / hover background
                if (i == selectedIndex)
                {
                    EditorGUI.DrawRect(rowRect, isHovered ? SelectedHoverColor : SelectedColor);
                }
                else if (isHovered)
                {
                    EditorGUI.DrawRect(rowRect, HoverColor);
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 2.5f, rowRect.height), HoverAccentColor);
                }

                // Handle Mouse Events (单击直接确认导入)
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
                {
                    selectedIndex = i;
                    ConfirmSelection(activePaths);
                    e.Use();
                }

                // Draw Text & Icon using cached styles
                GUIStyle labelStyle = (i == selectedIndex) ? selectedLabelStyle : normalLabelStyle;
                GUIContent content = EditorGUIUtility.IconContent(selectedTab == 0 ? "ScriptableObject Icon" : "TextAsset Icon");
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

            // 仅在鼠标移动且悬停项发生变化时触发重绘（零静态能耗开销）
            if (Event.current.type == EventType.MouseMove && currentHoveredIndex != lastHoveredIndex)
            {
                lastHoveredIndex = currentHoveredIndex;
                Repaint();
            }

            // 4. Bottom Info Bar
            EditorGUILayout.BeginHorizontal("box");
            if (selectedIndex >= 0 && selectedIndex < activePaths.Count)
            {
                string selPath = activePaths[selectedIndex];
                GUILayout.Label(Path.GetFileNameWithoutExtension(selPath), GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                string displayPath = selPath.Replace("\\", "/");
                GUILayout.Label(displayPath, pathStyle);
            }
            else
            {
                GUILayout.Label("None");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void HandleKeyboard(List<string> activePaths)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.DownArrow)
                {
                    selectedIndex = Mathf.Min(selectedIndex + 1, activePaths.Count - 1);
                    needsScrollToSelection = true;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    selectedIndex = Mathf.Max(selectedIndex - 1, 0);
                    needsScrollToSelection = true;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    ConfirmSelection(activePaths);
                    e.Use();
                }
            }
        }

        private void ConfirmSelection(List<string> activePaths)
        {
            if (selectedIndex >= 0 && selectedIndex < activePaths.Count)
            {
                string path = activePaths[selectedIndex];
                ActionTimeline timeline = null;
                
                if (selectedTab == 0)
                {
                    // SO 导入
                    string relativePath = path.Replace("\\", "/");
                    if (relativePath.StartsWith(Application.dataPath))
                    {
                        relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                    }
                    timeline = AssetDatabase.LoadAssetAtPath<ActionTimeline>(relativePath);
                }
                else
                {
                    // JSON 导入
                    timeline = SerializationUtility.ImportFromJsonPath(path);
                }
                
                onTimelineSelected?.Invoke(timeline, path);
                Close();
            }
        }

        private bool TrySelectFileName(string targetFileName)
        {
            selectedIndex = -1;
            if (string.IsNullOrEmpty(targetFileName))
            {
                return false;
            }

            List<string> activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            for (int i = 0; i < activePaths.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(activePaths[i]);
                if (string.Equals(fileName, targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
