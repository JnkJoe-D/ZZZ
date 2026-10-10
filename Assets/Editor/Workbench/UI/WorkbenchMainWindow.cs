using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 主窗口入口 (Workbench Main Window)
    /// 集成概览看板 (Overview)、动作资产 (Actions)、时间轴诊断 (Timelines) 与统一配置中心 (Settings)。
    /// 遵循严格固定布局与抗跳变规范，保证高保真交互体验。
    /// </summary>
    public class WorkbenchMainWindow : EditorWindow
    {
        private const string LastWorkspacePrefKey = "ZZZ_Workbench_LastSelectedWorkspaceId";

        private static readonly string[] TabTitles = new[]
        {
            "概览看板",
            "映射对齐看板",
            "配表动作源",
            "动作资产",
            "时间轴诊断",
            "统一配置"
        };

        // 数据与上下文缓存
        private SharedWorkspaceData _sharedData;
        private string _selectedWorkspaceId;
        private int _selectedCategoryIndex = 0; // 0: 全部
        private WorkbenchCharacterContext _currentContext;
        private int _activeTab = 0;

        // 缓存的角色筛选列表
        private readonly List<SharedWorkspaceDefinition> _filteredWorkspaces = new List<SharedWorkspaceDefinition>();
        private string[] _workspaceDisplayNames = Array.Empty<string>();
        private string[] _categoryNames = Array.Empty<string>();

        [MenuItem("Window/Workbench/配置与资产工作台", priority = 2000)]
        [MenuItem("Tools/Workbench/配置与资产工作台", priority = 2000)]
        public static void OpenWindow()
        {
            var window = GetWindow<WorkbenchMainWindow>("配置与资产工作台");
            window.minSize = new Vector2(880f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDataAndContext();
        }

        /// <summary>
        /// 加载公共工作区数据并重建当前角色的业务上下文
        /// </summary>
        public void LoadDataAndContext()
        {
            _sharedData = SharedWorkspaceFileIO.Load();
            _sharedData.EnsureDefaults();

            // 1. 构建分类下拉列表
            var cats = new List<string> { "全部分类" };
            cats.AddRange(_sharedData.Categories);
            _categoryNames = cats.ToArray();
            if (_selectedCategoryIndex >= _categoryNames.Length)
            {
                _selectedCategoryIndex = 0;
            }

            // 2. 筛选对应分类的角色列表
            UpdateFilteredWorkspaces();

            // 3. 恢复选定角色 ID
            if (string.IsNullOrEmpty(_selectedWorkspaceId))
            {
                _selectedWorkspaceId = EditorPrefs.GetString(LastWorkspacePrefKey, string.Empty);
            }

            // 若历史 ID 不在当前筛选结果中，优先选中首个
            if (_filteredWorkspaces.Count > 0)
            {
                var matched = _filteredWorkspaces.FirstOrDefault(w => string.Equals(w.Id, _selectedWorkspaceId, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    _selectedWorkspaceId = _filteredWorkspaces[0].Id;
                }
            }
            else
            {
                _selectedWorkspaceId = string.Empty;
            }

            // 4. 重建当前角色上下文
            RebuildCurrentContext();
        }

        private void UpdateFilteredWorkspaces()
        {
            _filteredWorkspaces.Clear();
            if (_sharedData?.Workspaces == null) return;

            string selectedCat = _selectedCategoryIndex == 0 ? null : _categoryNames[_selectedCategoryIndex];

            foreach (var ws in _sharedData.Workspaces)
            {
                if (string.IsNullOrEmpty(selectedCat) || string.Equals(ws.Category, selectedCat, StringComparison.OrdinalIgnoreCase))
                {
                    _filteredWorkspaces.Add(ws);
                }
            }

            _workspaceDisplayNames = _filteredWorkspaces
                .Select(w => $"[{w.Category}] {w.DisplayName}")
                .ToArray();
        }

        private void RebuildCurrentContext()
        {
            if (string.IsNullOrEmpty(_selectedWorkspaceId) || _sharedData == null)
            {
                _currentContext = null;
                return;
            }

            var ws = _sharedData.FindById(_selectedWorkspaceId);
            if (ws == null)
            {
                _currentContext = null;
                return;
            }

            try
            {
                _currentContext = WorkbenchAssetScanner.BuildContext(ws);
                EditorPrefs.SetString(LastWorkspacePrefKey, _selectedWorkspaceId);
                WorkbenchActionMappingView.InvalidateCache();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Workbench] 构建角色上下文异常: {ex.Message}\n{ex.StackTrace}");
                _currentContext = null;
            }
        }

        private void OnGUI()
        {
            if (_sharedData == null)
            {
                LoadDataAndContext();
            }

            // 1. 顶部固定上下文工具栏（固定高度 28px）
            DrawTopContextBar();

            // 2. 主 Tab 导航栏（固定高度 28px）
            DrawNavigationTabBar();

            EditorGUILayout.Space(2f);

            // 3. 主视图区域
            DrawMainContentArea();

            // 4. 底部固定状态栏（固定高度 22px）
            DrawBottomStatusBar();
        }

        /// <summary>
        /// 绘制顶部固定上下文工具栏（固定尺寸，杜绝跳变）
        /// </summary>
        private void DrawTopContextBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28f));
            {
                GUILayout.Space(4f);

                // 分类筛选
                EditorGUILayout.LabelField("分类:", EditorStyles.miniLabel, GUILayout.Width(35f));
                int newCatIndex = EditorGUILayout.Popup(_selectedCategoryIndex, _categoryNames, EditorStyles.toolbarPopup, GUILayout.Width(100f));
                if (newCatIndex != _selectedCategoryIndex)
                {
                    _selectedCategoryIndex = newCatIndex;
                    UpdateFilteredWorkspaces();
                    if (_filteredWorkspaces.Count > 0)
                    {
                        _selectedWorkspaceId = _filteredWorkspaces[0].Id;
                    }
                    RebuildCurrentContext();
                }

                GUILayout.Space(6f);

                // 角色选择下拉
                EditorGUILayout.LabelField("角色:", EditorStyles.miniLabel, GUILayout.Width(35f));
                int selectedIndex = _filteredWorkspaces.FindIndex(w => string.Equals(w.Id, _selectedWorkspaceId, StringComparison.OrdinalIgnoreCase));
                if (selectedIndex < 0 && _filteredWorkspaces.Count > 0) selectedIndex = 0;

                int newIndex = EditorGUILayout.Popup(selectedIndex, _workspaceDisplayNames, EditorStyles.toolbarPopup, GUILayout.Width(220f));
                if (newIndex != selectedIndex && newIndex >= 0 && newIndex < _filteredWorkspaces.Count)
                {
                    _selectedWorkspaceId = _filteredWorkspaces[newIndex].Id;
                    RebuildCurrentContext();
                }

                GUILayout.Space(8f);

                // Luban 绑定徽标 (固定 80px 宽)
                if (_currentContext != null)
                {
                    bool hasLuban = _currentContext.LubanInfo != null;
                    WorkbenchUIStyles.DrawStatusBadge(hasLuban, hasLuban ? "Luban 已绑" : "Luban 未绑", 80f);
                }

                GUILayout.FlexibleSpace();

                // 快速定位资产目录按钮 (固定 95px 宽)
                if (_currentContext?.EffectiveConfig != null)
                {
                    string actionDir = _currentContext.EffectiveConfig.ActionConfigDirectory;
                    bool dirExists = !string.IsNullOrEmpty(actionDir) && System.IO.Directory.Exists(actionDir);
                    GUI.enabled = dirExists;
                    if (GUILayout.Button("📁 资产目录", EditorStyles.toolbarButton, GUILayout.Width(95f)))
                    {
                        var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(actionDir);
                        if (folderObj != null)
                        {
                            EditorGUIUtility.PingObject(folderObj);
                            Selection.activeObject = folderObj;
                        }
                    }
                    GUI.enabled = true;
                }

                // 重新扫描与刷新按钮 (固定 75px 宽)
                if (GUILayout.Button("🔄 刷新", EditorStyles.toolbarButton, GUILayout.Width(75f)))
                {
                    AssetDatabase.Refresh();
                    SkillTableDataSourceAdapter.ClearCache();
                    WorkbenchActionMappingView.InvalidateCache();
                    LoadDataAndContext();
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制主 Tab 导航栏（固定高度 28px）
        /// </summary>
        private void DrawNavigationTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28f));
            {
                _activeTab = GUILayout.Toolbar(_activeTab, TabTitles, EditorStyles.toolbarButton, GUILayout.Height(26f));
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制核心内容视口
        /// </summary>
        private void DrawMainContentArea()
        {
            if (_sharedData == null || _sharedData.Workspaces.Count == 0)
            {
                EditorGUILayout.HelpBox("工作区配置数据为空，请检查或在设置中初始化工作区。", MessageType.Warning);
                return;
            }

            if (_currentContext == null && _activeTab != 5)
            {
                EditorGUILayout.HelpBox("未选定有效角色，请在顶部上下文栏选择角色，或切换至 [统一配置] Tab。", MessageType.Info);
                return;
            }

            switch (_activeTab)
            {
                case 0: // 概览看板
                    WorkbenchOverviewView.Draw(_currentContext, LoadDataAndContext);
                    break;
                case 1: // 映射对齐看板
                    WorkbenchActionMappingView.Draw(_currentContext, LoadDataAndContext);
                    break;
                case 2: // 配表动作源
                    WorkbenchSkillsView.Draw(_currentContext, LoadDataAndContext);
                    break;
                case 3: // 动作资产
                    WorkbenchActionsView.Draw(_currentContext, LoadDataAndContext);
                    break;
                case 4: // 时间轴诊断
                    WorkbenchTimelinesView.Draw(_currentContext, LoadDataAndContext);
                    break;
                case 5: // 统一配置
                    WorkbenchSettingsView.Draw(_currentContext, LoadDataAndContext);
                    break;
            }
        }

        /// <summary>
        /// 绘制底部固定高度状态栏（固定 22px 高）
        /// </summary>
        private void DrawBottomStatusBar()
        {
            Rect barRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(22f));
            EditorGUI.DrawRect(barRect, new Color(0.14f, 0.14f, 0.14f, 1f));
            {
                GUILayout.Space(6f);

                if (_currentContext != null)
                {
                    GUILayout.Label($"当前角色: {_currentContext.Workspace.DisplayName} ({_currentContext.Workspace.Id})", EditorStyles.miniLabel, GUILayout.Width(220f));
                    GUILayout.Label($"|   动作: {_currentContext.ActionCount} (完全绑定: {_currentContext.FullyLinkedActionCount})", EditorStyles.miniLabel, GUILayout.Width(190f));
                    GUILayout.Label($"|   时间轴: {_currentContext.Timelines.Count}", EditorStyles.miniLabel, GUILayout.Width(130f));
                    GUILayout.Label($"|   配置策略: {_currentContext.EffectiveConfig.ConflictPolicy}", EditorStyles.miniLabel, GUILayout.Width(150f));
                }
                else
                {
                    GUILayout.Label("未就绪 (无选定角色上下文)", EditorStyles.miniLabel, GUILayout.Width(200f));
                }

                GUILayout.FlexibleSpace();

                // 右侧状态提示
                GUILayout.Label("● 就绪", EditorStyles.miniBoldLabel, GUILayout.Width(50f));
                GUILayout.Space(6f);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
