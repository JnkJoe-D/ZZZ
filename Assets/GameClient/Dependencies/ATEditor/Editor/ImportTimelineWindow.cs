using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace ATEditor.Editor
{
    /// <summary>
    /// 动作时间轴导入窗口（支持分类/工作区树状分级检索）
    /// </summary>
    public class ImportTimelineWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabs = new string[] { "从 Asset(SO) 导入", "从 JSON 导入" };

        private string searchString = "";
        private string rootSoDir;
        private string rootJsonDir;

        // 当前选中的工作区
        private string selectedWorkspaceId;
        private ATWorkspaceDefinition selectedWorkspace;

        private string[] allJsonPaths = new string[0];
        private string[] allSOPaths = new string[0];
        private List<string> filteredJsonPaths = new List<string>();
        private List<string> filteredSOPaths = new List<string>();

        private string preferredSelectedFileName;
        private Action<ActionTimeline, string> onTimelineSelected;

        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private int selectedIndex = -1;
        private int lastHoveredIndex = -1;
        private bool needsScrollToSelection = true;

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
            window.minSize = new Vector2(650, 480);
            window.titleContent = new GUIContent("Import Timeline");
            window.onTimelineSelected = onSelected;
            window.rootSoDir = soDir;
            window.rootJsonDir = jsonDir;

            if (!string.IsNullOrEmpty(initialSelectedPath))
            {
                window.preferredSelectedFileName = Path.GetFileNameWithoutExtension(initialSelectedPath);
                window.selectedTab = initialSelectedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
            else
            {
                window.preferredSelectedFileName = null;
            }

            // 初始化默认选中当前激活的工作区
            var db = ATEditorWorkspaceDatabase.Instance;
            string currentActiveId = EditorPrefs.GetString("ATEditor_ActiveWorkspaceId", "Player_Ellen");
            var targetWs = db.GetWorkspaceById(currentActiveId);
            if (targetWs == null && db.Workspaces.Count > 0)
            {
                targetWs = db.Workspaces[0];
            }

            window.SelectWorkspace(targetWs);
            window.needsScrollToSelection = true;
            window.ShowUtility();
        }

        private void SelectWorkspace(ATWorkspaceDefinition ws)
        {
            selectedWorkspace = ws;
            selectedWorkspaceId = ws?.Id;
            LoadFilesForCurrentWorkspace();
        }

        private void LoadFilesForCurrentWorkspace()
        {
            allSOPaths = new string[0];
            allJsonPaths = new string[0];

            if (selectedWorkspace != null && !string.IsNullOrEmpty(selectedWorkspace.FolderName))
            {
                string soFolder = Path.Combine(rootSoDir, selectedWorkspace.FolderName).Replace("\\", "/");
                string jsonFolder = Path.Combine(rootJsonDir, selectedWorkspace.FolderName).Replace("\\", "/");

                if (Directory.Exists(soFolder))
                {
                    allSOPaths = Directory.GetFiles(soFolder, "*.asset", SearchOption.AllDirectories);
                }
                if (Directory.Exists(jsonFolder))
                {
                    allJsonPaths = Directory.GetFiles(jsonFolder, "*.json", SearchOption.AllDirectories);
                }
            }

            FilterFiles();
        }

        private void FilterFiles()
        {
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
            EditorGUILayout.BeginHorizontal();

            // 1. 左栏：分类与工作区选择器 (180px)
            DrawLeftWorkspacePanel();

            // 分割线
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // 2. 右栏：文件列表与搜索
            DrawRightFileListPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftWorkspacePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(180));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("角色工作区", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, EditorStyles.helpBox);

            var db = ATEditorWorkspaceDatabase.Instance;
            var categories = db.GetCategories();

            foreach (var category in categories)
            {
                var list = db.GetWorkspacesByCategory(category);
                if (list.Count == 0 && category != "Player" && category != "Monster" && category != "Common") continue;

                EditorGUILayout.LabelField($"▾ {category}", EditorStyles.boldLabel);

                foreach (var ws in list)
                {
                    bool isSelected = selectedWorkspace != null && selectedWorkspace.Id == ws.Id;
                    GUIStyle style = isSelected ? new GUIStyle("SelectionRect") : EditorStyles.label;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    string label = string.IsNullOrEmpty(ws.DisplayName) ? ws.Id : ws.DisplayName;
                    if (GUILayout.Button(label, style, GUILayout.Height(20)))
                    {
                        if (selectedWorkspace?.Id != ws.Id)
                        {
                            SelectWorkspace(ws);
                            GUI.FocusControl(null);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightFileListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // 1. 顶部 Tab 切换
            int currentTab = GUILayout.Toolbar(selectedTab, tabs);
            if (currentTab != selectedTab)
            {
                List<string> oldFiltered = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
                if (selectedIndex >= 0 && selectedIndex < oldFiltered.Count)
                {
                    preferredSelectedFileName = Path.GetFileNameWithoutExtension(oldFiltered[selectedIndex]);
                }
                
                selectedTab = currentTab;
                needsScrollToSelection = true;
                TrySelectFileName(preferredSelectedFileName);
                GUI.FocusControl(null);
            }
            EditorGUILayout.Space(2);

            DrawCommonListArea();

            EditorGUILayout.EndVertical();
        }

        private void DrawCommonListArea()
        {
            List<string> activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            
            HandleKeyboard(activePaths);

            // 1. 顶部搜索栏
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

            // 2. 状态与提示栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string wsName = selectedWorkspace != null ? selectedWorkspace.DisplayName : "未选定工作区";
            GUILayout.Label($"工作区: {wsName} (共 {activePaths.Count} 个动作)", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EnsureStyles();

            // 3. 列表展示区域
            if (needsScrollToSelection && selectedIndex >= 0 && activePaths.Count > 0)
            {
                float itemY = selectedIndex * 20;
                float scrollViewHeight = position.height - 110;
                rightScrollPos.y = Mathf.Max(0, itemY - scrollViewHeight / 2);
                needsScrollToSelection = false;
            }
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
            
            int currentHoveredIndex = -1;
            Vector2 mousePos = Event.current.mousePosition;

            if (activePaths.Count == 0)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("当前工作区目录下未找到动作资产。\n保存或导出新时间轴时，将自动归档至该工作区子目录。", MessageType.Info);
            }

            for (int i = 0; i < activePaths.Count; i++)
            {
                string filePath = activePaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                Rect rowRect = EditorGUILayout.GetControlRect(false, 20);
                bool isHovered = rowRect.Contains(mousePos);
                if (isHovered)
                {
                    currentHoveredIndex = i;
                }
                
                // 绘制选中或悬停高亮背景
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

                // 使用缓存样式绘制文本与图标
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

            if (Event.current.type == EventType.MouseMove && currentHoveredIndex != lastHoveredIndex)
            {
                lastHoveredIndex = currentHoveredIndex;
                Repaint();
            }

            // 4. 底部信息栏
            EditorGUILayout.BeginHorizontal("box");
            if (selectedIndex >= 0 && selectedIndex < activePaths.Count)
            {
                string selPath = activePaths[selectedIndex];
                GUILayout.Label(Path.GetFileNameWithoutExtension(selPath), GUILayout.Width(160));
                GUILayout.FlexibleSpace();
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                string displayPath = selPath.Replace("\\", "/");
                GUILayout.Label(displayPath, pathStyle);
            }
            else
            {
                GUILayout.Label("未选中资产");
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
