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
        private GUIStyle tabStyle;
        private GUIStyle tabSelectedStyle;
        private GUIStyle footerPathStyle;
        private GUIStyle footerHintStyle;

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
            if (tabStyle == null)
            {
                tabStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
                };
            }
            if (tabSelectedStyle == null)
            {
                tabSelectedStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
            if (footerPathStyle == null)
            {
                footerPathStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) }
                };
            }
            if (footerHintStyle == null)
            {
                footerHintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 1f) }
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
            EnsureStyles();

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
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MinWidth(0));

            // 1. 顶部 Tab 切换（严格 50% / 50% 均匀分配宽度，彻底解决 GUILayout.Toolbar 挤压问题）
            DrawTabs();
            EditorGUILayout.Space(2);

            DrawCommonListArea();

            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            Rect tabRect = EditorGUILayout.GetControlRect(false, 26);
            if (tabRect.width <= 0) return;

            int count = tabs.Length;
            float btnWidth = tabRect.width / count;
            Vector2 mousePos = Event.current.mousePosition;

            for (int i = 0; i < count; i++)
            {
                Rect btnRect = new Rect(tabRect.x + i * btnWidth, tabRect.y, btnWidth, tabRect.height);
                bool isSelected = (selectedTab == i);
                bool isHovered = btnRect.Contains(mousePos);

                EditorGUIUtility.AddCursorRect(btnRect, MouseCursor.Link);

                // 绘制背景
                if (isSelected)
                {
                    EditorGUI.DrawRect(btnRect, isHovered ? SelectedHoverColor : SelectedColor);
                    // 底部高亮指示条
                    EditorGUI.DrawRect(new Rect(btnRect.x, btnRect.yMax - 2, btnRect.width, 2), new Color(0.35f, 0.75f, 1f, 1f));
                }
                else
                {
                    EditorGUI.DrawRect(btnRect, isHovered ? new Color(1f, 1f, 1f, 0.08f) : new Color(0.18f, 0.18f, 0.18f, 0.6f));
                }

                // 细分割线
                Handles.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                Handles.DrawLine(new Vector3(btnRect.x, btnRect.y), new Vector3(btnRect.xMax, btnRect.y));
                Handles.DrawLine(new Vector3(btnRect.x, btnRect.yMax), new Vector3(btnRect.xMax, btnRect.yMax));
                if (i > 0)
                {
                    Handles.DrawLine(new Vector3(btnRect.x, btnRect.y), new Vector3(btnRect.x, btnRect.yMax));
                }

                // 图标与文本
                GUIContent icon = EditorGUIUtility.IconContent(i == 0 ? "ScriptableObject Icon" : "TextAsset Icon");
                GUIContent content = new GUIContent(" " + tabs[i], icon != null ? icon.image : null);

                GUI.Label(btnRect, content, isSelected ? tabSelectedStyle : tabStyle);

                // 点击切换 Tab
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHovered)
                {
                    if (selectedTab != i)
                    {
                        SwitchTab(i);
                    }
                    Event.current.Use();
                }
            }
        }

        private void SwitchTab(int newTab)
        {
            List<string> oldFiltered = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
            if (selectedIndex >= 0 && selectedIndex < oldFiltered.Count)
            {
                preferredSelectedFileName = Path.GetFileNameWithoutExtension(oldFiltered[selectedIndex]);
            }

            selectedTab = newTab;
            needsScrollToSelection = true;
            TrySelectFileName(preferredSelectedFileName);
            GUI.FocusControl(null);
            Repaint();
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

                // Handle Mouse Events (单击选中高亮，双击确认导入，右键上下文菜单)
                Event e = Event.current;
                if (isHovered)
                {
                    // 1. 左键单击与双击
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        selectedIndex = i;
                        preferredSelectedFileName = fileName;
                        GUI.FocusControl(null);

                        if (e.clickCount >= 2)
                        {
                            ConfirmSelection(activePaths);
                        }
                        else
                        {
                            Repaint();
                        }
                        e.Use();
                    }
                    // 2. 右键点击弹出上下文菜单
                    else if ((e.type == EventType.MouseDown && e.button == 1) || e.type == EventType.ContextClick)
                    {
                        selectedIndex = i;
                        preferredSelectedFileName = fileName;
                        Repaint();

                        ShowItemContextMenu(filePath, fileName, activePaths);
                        e.Use();
                    }
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

            // 4. 底部操作与信息栏 (采用固定高度与精准裁剪，彻底防止长路径撑爆布局)
            Rect footerRect = EditorGUILayout.GetControlRect(false, 28);
            EditorGUI.DrawRect(footerRect, new Color(0.18f, 0.18f, 0.18f, 0.9f));
            Handles.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);
            Handles.DrawLine(new Vector3(footerRect.x, footerRect.y), new Vector3(footerRect.xMax, footerRect.y));

            float padding = 6f;
            float contentX = footerRect.x + padding;
            float contentWidth = footerRect.width - padding * 2;

            if (selectedIndex >= 0 && selectedIndex < activePaths.Count)
            {
                string selPath = activePaths[selectedIndex];
                string fileName = Path.GetFileNameWithoutExtension(selPath);
                string displayPath = selPath.Replace("\\", "/");

                float btnWidth = 100f;
                float btnHeight = 20f;
                float btnY = footerRect.y + (footerRect.height - btnHeight) / 2;
                Rect btnRect = new Rect(footerRect.xMax - padding - btnWidth, btnY, btnWidth, btnHeight);

                // 导入按钮（始终贴靠在右下角，绝不被挤出视野）
                if (GUI.Button(btnRect, "导入选定动作"))
                {
                    ConfirmSelection(activePaths);
                }

                // 左侧动作名称与中间自适应路径
                float leftAvailableWidth = Mathf.Max(50f, btnRect.x - contentX - 10f);

                GUIContent nameContent = new GUIContent(fileName, fileName);
                float nameWidth = Mathf.Min(180f, Mathf.Max(80f, EditorStyles.boldLabel.CalcSize(nameContent).x + 4));
                if (nameWidth > leftAvailableWidth * 0.45f)
                {
                    nameWidth = leftAvailableWidth * 0.45f;
                }

                Rect nameRect = new Rect(contentX, footerRect.y, nameWidth, footerRect.height);
                GUI.Label(nameRect, nameContent, EditorStyles.boldLabel);

                float pathX = nameRect.xMax + 6f;
                float pathWidth = Mathf.Max(0f, btnRect.x - pathX - 8f);
                Rect pathRect = new Rect(pathX, footerRect.y, pathWidth, footerRect.height);

                GUI.Label(pathRect, new GUIContent(displayPath, displayPath), footerPathStyle);
            }
            else
            {
                Rect hintRect = new Rect(contentX, footerRect.y, contentWidth, footerRect.height);
                GUI.Label(hintRect, "未选中资产 (单击选中，双击或右键导入)", footerHintStyle);
            }
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
                else if (e.keyCode == KeyCode.F2 && selectedIndex >= 0 && selectedIndex < activePaths.Count)
                {
                    string selPath = activePaths[selectedIndex];
                    string selName = Path.GetFileNameWithoutExtension(selPath);
                    RenameActionDialog.Open(selName, newName => PerformRename(selPath, selName, newName));
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Delete && selectedIndex >= 0 && selectedIndex < activePaths.Count)
                {
                    string selPath = activePaths[selectedIndex];
                    string selName = Path.GetFileNameWithoutExtension(selPath);
                    PerformDelete(selPath, selName);
                    e.Use();
                }
            }
        }

        private void ShowItemContextMenu(string filePath, string fileName, List<string> activePaths)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("导入 (Import)"), false, () =>
            {
                ConfirmSelection(activePaths);
            });

            menu.AddItem(new GUIContent("重命名 (Rename)..."), false, () =>
            {
                RenameActionDialog.Open(fileName, newName =>
                {
                    PerformRename(filePath, fileName, newName);
                });
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("删除 (Delete)..."), false, () =>
            {
                PerformDelete(filePath, fileName);
            });

            menu.ShowAsContext();
        }

        private void GetDualPaths(string currentPath, out string soPath, out string jsonPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(currentPath);
            var ws = selectedWorkspace ?? ATEditorWorkspaceDatabase.Instance.GetWorkspaceByAssetPath(currentPath);
            string folderName = ws != null && !string.IsNullOrEmpty(ws.FolderName) ? ws.FolderName : "";

            string soFolder = Path.Combine(rootSoDir, folderName).Replace('\\', '/');
            string jsonFolder = Path.Combine(rootJsonDir, folderName).Replace('\\', '/');

            string normalized = currentPath.Replace('\\', '/');

            string subRelDir = "";
            if (!string.IsNullOrEmpty(soFolder) && normalized.StartsWith(soFolder, StringComparison.OrdinalIgnoreCase))
            {
                string rel = Path.GetRelativePath(soFolder, normalized);
                subRelDir = Path.GetDirectoryName(rel)?.Replace('\\', '/');
            }
            else if (!string.IsNullOrEmpty(jsonFolder) && normalized.StartsWith(jsonFolder, StringComparison.OrdinalIgnoreCase))
            {
                string rel = Path.GetRelativePath(jsonFolder, normalized);
                subRelDir = Path.GetDirectoryName(rel)?.Replace('\\', '/');
            }

            string targetSoDir = string.IsNullOrEmpty(subRelDir) ? soFolder : Path.Combine(soFolder, subRelDir).Replace('\\', '/');
            string targetJsonDir = string.IsNullOrEmpty(subRelDir) ? jsonFolder : Path.Combine(jsonFolder, subRelDir).Replace('\\', '/');

            soPath = Path.Combine(targetSoDir, fileName + ".asset").Replace('\\', '/');
            jsonPath = Path.Combine(targetJsonDir, fileName + ".json").Replace('\\', '/');
        }

        private static string ToRelativeAssetPath(string fullPath)
        {
            string normalized = fullPath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }
            return null;
        }

        private void PerformRename(string currentPath, string oldFileName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(oldFileName, newName, StringComparison.OrdinalIgnoreCase))
                return;

            GetDualPaths(currentPath, out string soPath, out string jsonPath);

            string newSoPath = Path.Combine(Path.GetDirectoryName(soPath), newName + ".asset").Replace('\\', '/');
            string newJsonPath = Path.Combine(Path.GetDirectoryName(jsonPath), newName + ".json").Replace('\\', '/');

            // 检查目标文件是否已存在
            if (File.Exists(newSoPath) || File.Exists(newJsonPath))
            {
                EditorUtility.DisplayDialog("重命名失败", $"目标动作 '{newName}' 已存在，请更换其他名称。", "确定");
                return;
            }

            try
            {
                bool soRenamed = false;
                bool jsonRenamed = false;

                // 1. 重命名 SO 资产
                if (File.Exists(soPath))
                {
                    string relSoPath = ToRelativeAssetPath(soPath);
                    if (!string.IsNullOrEmpty(relSoPath))
                    {
                        string error = AssetDatabase.RenameAsset(relSoPath, newName);
                        if (string.IsNullOrEmpty(error))
                        {
                            soRenamed = true;
                            // 更新资产内部 Object.name
                            string updatedRelSo = Path.Combine(Path.GetDirectoryName(relSoPath), newName + ".asset").Replace('\\', '/');
                            var asset = AssetDatabase.LoadAssetAtPath<ActionTimeline>(updatedRelSo);
                            if (asset != null)
                            {
                                asset.name = newName;
                                EditorUtility.SetDirty(asset);
                            }
                        }
                    }

                    if (!soRenamed)
                    {
                        File.Move(soPath, newSoPath);
                        if (File.Exists(soPath + ".meta")) File.Move(soPath + ".meta", newSoPath + ".meta");
                    }
                }

                // 2. 重命名 JSON 资产
                if (File.Exists(jsonPath))
                {
                    string relJsonPath = ToRelativeAssetPath(jsonPath);
                    if (!string.IsNullOrEmpty(relJsonPath))
                    {
                        string error = AssetDatabase.RenameAsset(relJsonPath, newName);
                        if (string.IsNullOrEmpty(error))
                        {
                            jsonRenamed = true;
                        }
                    }

                    if (!jsonRenamed)
                    {
                        File.Move(jsonPath, newJsonPath);
                        if (File.Exists(jsonPath + ".meta")) File.Move(jsonPath + ".meta", newJsonPath + ".meta");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 3. 同步更新打开中的主窗口状态
                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    var editorWindow = EditorWindow.GetWindow<ATEditorWindow>();
                    if (editorWindow != null && editorWindow.State != null)
                    {
                        string curr = editorWindow.State.currentFilePath?.Replace('\\', '/');
                        if (!string.IsNullOrEmpty(curr) && (string.Equals(curr, soPath, StringComparison.OrdinalIgnoreCase) || string.Equals(curr, jsonPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            editorWindow.State.currentFilePath = selectedTab == 0 ? newSoPath : newJsonPath;
                            if (editorWindow.State.currentTimeline != null)
                            {
                                editorWindow.State.currentTimeline.name = newName;
                            }
                            editorWindow.Repaint();
                        }
                    }
                }

                // 4. 刷新当前工作区列表并选中更新后的项
                preferredSelectedFileName = newName;
                LoadFilesForCurrentWorkspace();
                TrySelectFileName(newName);
                needsScrollToSelection = true;
                Repaint();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("重命名失败", $"发生异常: {ex.Message}", "确定");
            }
        }

        private void PerformDelete(string currentPath, string fileName)
        {
            GetDualPaths(currentPath, out string soPath, out string jsonPath);

            bool soExists = File.Exists(soPath);
            bool jsonExists = File.Exists(jsonPath);

            string fileListStr = "";
            if (soExists) fileListStr += $"• SO 资产: {Path.GetFileName(soPath)}\n";
            if (jsonExists) fileListStr += $"• JSON 资产: {Path.GetFileName(jsonPath)}\n";
            if (!soExists && !jsonExists) fileListStr += $"• 目标文件: {Path.GetFileName(currentPath)}\n";

            bool confirm = EditorUtility.DisplayDialog(
                "⚠️ 确认删除动作资产",
                $"确定要彻底删除动作资产【{fileName}】吗？\n\n该操作将同时永久删除磁盘上的双轨资产文件：\n{fileListStr}\n注意：此操作无法撤销！",
                "确认删除",
                "取消");

            if (!confirm) return;

            try
            {
                if (soExists)
                {
                    string rel = ToRelativeAssetPath(soPath);
                    if (!string.IsNullOrEmpty(rel)) AssetDatabase.DeleteAsset(rel);
                    else
                    {
                        File.Delete(soPath);
                        if (File.Exists(soPath + ".meta")) File.Delete(soPath + ".meta");
                    }
                }

                if (jsonExists)
                {
                    string rel = ToRelativeAssetPath(jsonPath);
                    if (!string.IsNullOrEmpty(rel)) AssetDatabase.DeleteAsset(rel);
                    else
                    {
                        File.Delete(jsonPath);
                        if (File.Exists(jsonPath + ".meta")) File.Delete(jsonPath + ".meta");
                    }
                }

                AssetDatabase.Refresh();

                // 如果主编辑器当前恰好载入了此动作，重置为空白状态
                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    var editorWindow = EditorWindow.GetWindow<ATEditorWindow>();
                    if (editorWindow != null && editorWindow.State != null)
                    {
                        string curr = editorWindow.State.currentFilePath?.Replace('\\', '/');
                        if (!string.IsNullOrEmpty(curr) && (string.Equals(curr, soPath, StringComparison.OrdinalIgnoreCase) || string.Equals(curr, jsonPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            editorWindow.ResetToBlankTimeline();
                            editorWindow.Repaint();
                        }
                    }
                }

                preferredSelectedFileName = null;
                LoadFilesForCurrentWorkspace();
                var activePaths = selectedTab == 0 ? filteredSOPaths : filteredJsonPaths;
                if (selectedIndex >= activePaths.Count)
                {
                    selectedIndex = activePaths.Count - 1;
                }
                Repaint();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("删除失败", $"发生异常: {ex.Message}", "确定");
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

    /// <summary>
    /// 动作资产重命名弹窗
    /// </summary>
    public class RenameActionDialog : EditorWindow
    {
        private string oldName;
        private string newName;
        private Action<string> onConfirm;
        private bool isFirstFocus = true;

        public static void Open(string currentName, Action<string> onConfirmCallback)
        {
            var window = CreateInstance<RenameActionDialog>();
            window.titleContent = new GUIContent("重命名动作资产");
            window.oldName = currentName;
            window.newName = currentName;
            window.onConfirm = onConfirmCallback;
            window.minSize = new Vector2(360, 120);
            window.maxSize = new Vector2(360, 120);

            Rect mainRect = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(mainRect.x + (mainRect.width - 360) * 0.5f, mainRect.y + (mainRect.height - 120) * 0.5f, 360, 120);
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"重命名动作: {oldName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            GUI.SetNextControlName("RenameInputField");
            newName = EditorGUILayout.TextField("新动作名称", newName);

            if (isFirstFocus)
            {
                EditorGUI.FocusTextInControl("RenameInputField");
                isFirstFocus = false;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool canConfirm = !string.IsNullOrWhiteSpace(newName) && newName != oldName && newName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

            if (GUILayout.Button("取消", GUILayout.Width(80)))
            {
                Close();
                return;
            }

            EditorGUI.BeginDisabledGroup(!canConfirm);
            if (GUILayout.Button("确定", GUILayout.Width(80)) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && canConfirm))
            {
                string confirmedName = newName.Trim();
                Close();
                onConfirm?.Invoke(confirmedName);
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }
    }
}
