using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// 动作路由窗口与时间轴匹配校验工具。
    /// 用于检查对比动作文件中的所有路由（包括自身派生路由与通用路由集）所要求的 RouteWindow，
    /// 是否都能在其关联的时间轴配置（SO 资产或 JSON TextAsset）中找到对应的 RouteWindowClip 窗口。
    /// </summary>
    public sealed class ActionTimelineWindowValidatorWindow : EditorWindow
    {
        #region 数据结构定义

        public enum RouteMatchStatus
        {
            [InspectorName("完美匹配")]
            Matched,
            [InspectorName("时间轴缺失窗口")]
            MissingWindow,
            [InspectorName("窗口类型不匹配")]
            TypeMismatch,
            [InspectorName("时间轴仍为旧版标签")]
            LegacyTimelineTag,
            [InspectorName("未配置时间轴")]
            MissingTimeline,
            [InspectorName("无需窗口约束")]
            NoWindowRequired
        }

        public sealed class TimelineWindowInfo
        {
            public RouteWindowClip Clip;
            public RouteWindow Window;
            public string Tag;
            public string WindowTypeName;
            public float StartTime;
            public float Duration;
            public float EndTime => StartTime + Duration;
            public bool IsLegacyComboTag;
            public int MatchedRouteCount;
        }

        public sealed class RouteMatchItem
        {
            public string RouteSource; // 自身派生路由 / 路由集: XXX
            public ScriptableObject SourceAsset;
            public int RouteIndex;
            public string TargetName; // 目标动作 / 事件名
            public string TriggerTypeName;
            public RouteWindow RequiredWindow;
            public string RequiredTag;
            public string RequiredWindowTypeName;
            public RouteMatchStatus Status;
            public TimelineWindowInfo MatchedTimelineWindow;
            public string DetailMessage;
        }

        public sealed class ActionReport
        {
            public ActionConfigAsset ActionAsset;
            public string ActionName;
            public string AssetPath;
            public string CharacterName;

            // 时间轴资产
            public ActionTimeline ResolvedTimeline;
            public UnityEngine.Object TimelineSourceAsset; // actionTimelineSO 或 TimelineAsset
            public string TimelineSourceDesc;
            public bool HasTimeline => ResolvedTimeline != null;
            public float TimelineDuration => ResolvedTimeline != null ? ResolvedTimeline.Duration : 0f;

            // 时间轴中配置的窗口
            public List<TimelineWindowInfo> TimelineWindows = new();

            // 动作包含的所有路由对比项
            public List<RouteMatchItem> Routes = new();

            // 统计指标
            public int TotalRouteCount => Routes.Count;
            public int MatchedCount => Routes.Count(r => r.Status == RouteMatchStatus.Matched || r.Status == RouteMatchStatus.NoWindowRequired);
            public int MissingWindowCount => Routes.Count(r => r.Status == RouteMatchStatus.MissingWindow);
            public int TypeMismatchCount => Routes.Count(r => r.Status == RouteMatchStatus.TypeMismatch);
            public int LegacyTagCount => Routes.Count(r => r.Status == RouteMatchStatus.LegacyTimelineTag);
            public int UnusedTimelineWindowCount => TimelineWindows.Count(w => w.MatchedRouteCount == 0);

            public bool HasErrors => !HasTimeline || MissingWindowCount > 0;
            public bool HasWarnings => TypeMismatchCount > 0 || LegacyTagCount > 0 || UnusedTimelineWindowCount > 0;
            public bool IsClean => !HasErrors && !HasWarnings;
        }

        #endregion

        #region 常量与状态字段

        private const string DefaultSearchPath = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role";

        [SerializeField]
        private string _searchPath = DefaultSearchPath;

        // 单个动作检查模式
        [SerializeField]
        private ActionConfigAsset _singleActionAsset;
        private ActionReport _singleActionReport;

        // 批量扫描模式
        private readonly List<ActionReport> _batchReports = new();
        private int _totalScannedActions;
        private int _totalScannedRoutes;
        private bool _hasScanned;

        // UI 状态与过滤
        private Vector2 _mainScrollPos;
        private Vector2 _singleScrollPos;
        private string _searchFilter = string.Empty;
        private int _characterFilterIndex = 0;
        private string[] _characterFilterOptions = { "全部角色" };
        private int _statusFilterIndex = 0;
        private readonly string[] _statusFilterOptions =
        {
            "全部动作",
            "仅显示有异常 (错误或警告)",
            "仅缺失时间轴 (Missing Timeline)",
            "仅时间轴缺失窗口 (Missing Window)",
            "仅窗口类型不匹配 (Type Mismatch)",
            "仅存在未引用窗口 (Unused Window)"
        };

        private readonly Dictionary<ActionConfigAsset, bool> _reportFoldouts = new();
        private int _selectedTab = 0; // 0: 批量扫描, 1: 单个动作对比

        #endregion

        #region 菜单与入口

        [MenuItem("Tools/Action/检查动作路由窗口与时间轴匹配", false, 101)]
        public static void Open()
        {
            var window = GetWindow<ActionTimelineWindowValidatorWindow>("时间轴窗口匹配检查");
            window.minSize = new Vector2(820, 520);
            window.Show();
        }

        #endregion

        #region GUI 绘制

        private void OnGUI()
        {
            DrawHeader();
            DrawTabSelector();

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            if (_selectedTab == 0)
            {
                // 批量扫描视图
                DrawBatchScannerToolbar();
                DrawBatchStatistics();
                DrawBatchFilterBar();
                DrawBatchResultsList();
            }
            else
            {
                // 单个动作深度对比视图
                DrawSingleActionInspector();
            }

            EditorGUILayout.EndScrollView();

            DrawBottomBar();
        }

        // ────────────────── 顶部标头 ──────────────────

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIStyle titleStyle = new(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        alignment = TextAnchor.MiddleLeft
                    };
                    GUILayout.Label("⚔️ 动作路由窗口与时间轴匹配校验工具", titleStyle);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("🔄 刷新", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        if (_selectedTab == 0 && _hasScanned)
                        {
                            ExecuteBatchScan();
                        }
                        else if (_singleActionAsset != null)
                        {
                            _singleActionReport = AnalyzeAction(_singleActionAsset);
                        }
                    }
                }

                EditorGUILayout.LabelField(
                    "说明：校验动作（及其引用的通用路由集）中每条路由要求的 RouteWindow 是否在对应的时间轴中存在同名、同类型的 RouteWindowClip 窗口。",
                    EditorStyles.wordWrappedMiniLabel
                );
            }
        }

        // ────────────────── 标签页切换 ──────────────────

        private void DrawTabSelector()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                int newTab = GUILayout.Toolbar(_selectedTab, new[] { "📂 目录批量检索 (Batch Scan)", "🎯 单个动作深度对比 (Single Action)" }, GUILayout.Height(28));
                if (newTab != _selectedTab)
                {
                    _selectedTab = newTab;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.Space(2);
        }

        #endregion

        #region 单个动作对比视图 (Single Action Inspector)

        private void DrawSingleActionInspector()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🎯 单个动作资产深度诊断", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("选择动作资产:", GUILayout.Width(90));
                    var prevAction = _singleActionAsset;
                    _singleActionAsset = (ActionConfigAsset)EditorGUILayout.ObjectField(_singleActionAsset, typeof(ActionConfigAsset), false);

                    if (_singleActionAsset != prevAction)
                    {
                        if (_singleActionAsset != null)
                        {
                            _singleActionReport = AnalyzeAction(_singleActionAsset);
                        }
                        else
                        {
                            _singleActionReport = null;
                        }
                    }

                    if (GUILayout.Button("重新诊断", GUILayout.Width(80)))
                    {
                        if (_singleActionAsset != null)
                        {
                            _singleActionReport = AnalyzeAction(_singleActionAsset);
                        }
                    }
                }
            }

            if (_singleActionAsset == null)
            {
                EditorGUILayout.HelpBox("请拖拽或选择一个 ActionConfigAsset 动作资产，或者在【目录批量检索】中点击任意动作项右侧的【🔍 详细对比】按钮直接载入。", MessageType.Info);
                return;
            }

            if (_singleActionReport == null)
            {
                _singleActionReport = AnalyzeAction(_singleActionAsset);
            }

            DrawActionReportDetail(_singleActionReport, isSingleView: true);
        }

        #endregion

        #region 批量检索视图 (Batch Scanner)

        private void DrawBatchScannerToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("检索目录:", GUILayout.Width(65));
                    _searchPath = EditorGUILayout.TextField(_searchPath);

                    if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                    {
                        string abs = EditorUtility.OpenFolderPanel("选择角色动作配置目录", _searchPath, "");
                        if (!string.IsNullOrEmpty(abs))
                        {
                            string rel = MakeRelativePath(abs);
                            if (!string.IsNullOrEmpty(rel))
                            {
                                _searchPath = rel;
                            }
                        }
                    }

                    if (GUILayout.Button("重置默认", GUILayout.Width(70)))
                    {
                        _searchPath = DefaultSearchPath;
                    }
                }

                EditorGUILayout.Space(2);
                if (GUILayout.Button("🚀 一键检索目录下所有动作路由与时间轴窗口匹配", GUILayout.Height(30)))
                {
                    ExecuteBatchScan();
                }
            }
        }

        private void DrawBatchStatistics()
        {
            if (!_hasScanned) return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"已扫描动作: {_totalScannedActions} 个", EditorStyles.boldLabel, GUILayout.Width(140));
                EditorGUILayout.LabelField($"已检查路由: {_totalScannedRoutes} 条", EditorStyles.boldLabel, GUILayout.Width(140));

                int errActionCount = _batchReports.Count(r => r.HasErrors);
                int warnActionCount = _batchReports.Count(r => !r.HasErrors && r.HasWarnings);
                int cleanActionCount = _batchReports.Count(r => r.IsClean);

                if (errActionCount == 0 && warnActionCount == 0)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✅ 完美！所有动作的路由窗口均在时间轴中正确配置", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    if (errActionCount > 0)
                    {
                        GUI.color = new Color(1f, 0.4f, 0.4f);
                        EditorGUILayout.LabelField($"❌ 错误动作: {errActionCount} 个", EditorStyles.boldLabel, GUILayout.Width(130));
                        GUI.color = Color.white;
                    }

                    if (warnActionCount > 0)
                    {
                        GUI.color = new Color(1f, 0.8f, 0.3f);
                        EditorGUILayout.LabelField($"⚠️ 警告动作: {warnActionCount} 个", EditorStyles.boldLabel, GUILayout.Width(130));
                        GUI.color = Color.white;
                    }

                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"● 正常动作: {cleanActionCount} 个", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawBatchFilterBar()
        {
            if (!_hasScanned || _batchReports.Count == 0) return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("搜索:", GUILayout.Width(35));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(170));

                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    _searchFilter = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.Space(8);
                GUILayout.Label("角色:", GUILayout.Width(35));
                _characterFilterIndex = EditorGUILayout.Popup(_characterFilterIndex, _characterFilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(110));

                GUILayout.Space(8);
                GUILayout.Label("状态:", GUILayout.Width(35));
                _statusFilterIndex = EditorGUILayout.Popup(_statusFilterIndex, _statusFilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(190));

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    SetAllFoldouts(true);
                }
                if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    SetAllFoldouts(false);
                }
            }
        }

        private void DrawBatchResultsList()
        {
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("请点击上方的【一键检索目录下所有动作路由与时间轴窗口匹配】开始扫描。", MessageType.Info);
                return;
            }

            var filtered = FilterReports(_batchReports).ToList();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("当前筛选条件下未找到任何匹配的动作配置。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);

            for (int i = 0; i < filtered.Count; i++)
            {
                var report = filtered[i];
                DrawActionReportCard(report);
            }
        }

        #endregion

        #region 报告卡片与详情绘制

        private void DrawActionReportCard(ActionReport report)
        {
            bool foldout = _reportFoldouts.TryGetValue(report.ActionAsset, out bool f) && f;
            bool newFoldout = foldout;

            Color headerColor = report.HasErrors
                ? new Color(1f, 0.85f, 0.85f)
                : report.HasWarnings
                    ? new Color(1f, 0.95f, 0.8f)
                    : new Color(0.85f, 1f, 0.85f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 卡片标题栏
                using (new EditorGUILayout.HorizontalScope())
                {
                    string statusIcon = report.HasErrors ? "❌" : report.HasWarnings ? "⚠️" : "✅";
                    string title = $"{statusIcon}  {report.ActionName}   [{report.CharacterName}]";

                    GUI.color = report.HasErrors ? new Color(1f, 0.3f, 0.3f) : report.HasWarnings ? new Color(0.9f, 0.6f, 0.1f) : Color.white;
                    newFoldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldout);
                    GUI.color = Color.white;

                    if (newFoldout != foldout)
                    {
                        _reportFoldouts[report.ActionAsset] = newFoldout;
                    }

                    GUILayout.FlexibleSpace();

                    // 异常简述标签
                    if (!report.HasTimeline)
                    {
                        DrawBadge("无时间轴", Color.magenta);
                    }
                    if (report.MissingWindowCount > 0)
                    {
                        DrawBadge($"缺窗口 x{report.MissingWindowCount}", Color.red);
                    }
                    if (report.TypeMismatchCount > 0)
                    {
                        DrawBadge($"类型不符 x{report.TypeMismatchCount}", new Color(1f, 0.5f, 0f));
                    }
                    if (report.UnusedTimelineWindowCount > 0)
                    {
                        DrawBadge($"未引窗口 x{report.UnusedTimelineWindowCount}", Color.gray);
                    }

                    GUILayout.Space(8);

                    // 快捷操作按钮
                    if (GUILayout.Button("🔍 详细对比", EditorStyles.miniButton, GUILayout.Width(75)))
                    {
                        _singleActionAsset = report.ActionAsset;
                        _singleActionReport = report;
                        _selectedTab = 1;
                        GUI.FocusControl(null);
                    }

                    if (GUILayout.Button("📌 动作", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        EditorGUIUtility.PingObject(report.ActionAsset);
                    }

                    if (report.TimelineSourceAsset != null && GUILayout.Button("⏱️ 时间轴", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(report.TimelineSourceAsset);
                    }
                }

                // 展开内容
                if (newFoldout)
                {
                    EditorGUILayout.Space(2);
                    DrawActionReportDetail(report, isSingleView: false);
                }
            }
            EditorGUILayout.Space(2);
        }

        private void DrawActionReportDetail(ActionReport report, bool isSingleView)
        {
            // 资产来源与时间轴信息
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("所属角色:", GUILayout.Width(65));
                    EditorGUILayout.SelectableLabel(report.CharacterName, EditorStyles.boldLabel, GUILayout.Height(18), GUILayout.Width(120));

                    EditorGUILayout.LabelField("动作路径:", GUILayout.Width(65));
                    EditorGUILayout.SelectableLabel(report.AssetPath, EditorStyles.miniLabel, GUILayout.Height(18));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("时间轴资产:", GUILayout.Width(75));

                    if (report.HasTimeline)
                    {
                        string sourceLabel = report.TimelineSourceDesc;
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField($"● {sourceLabel}", EditorStyles.boldLabel, GUILayout.Width(220));
                        GUI.color = Color.white;

                        EditorGUILayout.LabelField($"时长: {report.TimelineDuration:F2}s", GUILayout.Width(90));
                        EditorGUILayout.LabelField($"配置窗口数: {report.TimelineWindows.Count} 个", GUILayout.Width(130));

                        if (report.TimelineSourceAsset != null && GUILayout.Button("定位时间轴资产", EditorStyles.miniButton, GUILayout.Width(100)))
                        {
                            EditorGUIUtility.PingObject(report.TimelineSourceAsset);
                        }
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("❌ 未配置时间轴！(actionTimelineSO 与 TimelineAsset 均为空)", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                }
            }

            EditorGUILayout.Space(3);

            // 1. 时间轴窗口概览表
            if (report.HasTimeline)
            {
                DrawTimelineWindowsTable(report);
            }

            EditorGUILayout.Space(4);

            // 2. 路由窗口比对列表
            DrawRoutesComparisonTable(report);

            // 3. 诊断与建议
            DrawDiagnosticSuggestions(report);
        }

        private void DrawTimelineWindowsTable(ActionReport report)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("⏱️ 时间轴配置窗口列表 (Timeline RouteWindowClips):", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"共 {report.TimelineWindows.Count} 个片段", EditorStyles.miniLabel, GUILayout.Width(80));
                }

                if (report.TimelineWindows.Count == 0)
                {
                    EditorGUILayout.LabelField("（当前时间轴中未配置任何 RouteWindowClip 窗口片段）", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                // 表头
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("窗口 Tag", EditorStyles.boldLabel, GUILayout.Width(130));
                    GUILayout.Label("窗口类型", EditorStyles.boldLabel, GUILayout.Width(160));
                    GUILayout.Label("起止时间区间", EditorStyles.boldLabel, GUILayout.Width(130));
                    GUILayout.Label("持续时长", EditorStyles.boldLabel, GUILayout.Width(80));
                    GUILayout.Label("路由引用状态", EditorStyles.boldLabel, GUILayout.Width(120));
                    GUILayout.FlexibleSpace();
                }

                // 表体
                for (int i = 0; i < report.TimelineWindows.Count; i++)
                {
                    var win = report.TimelineWindows[i];
                    Color rowBg = i % 2 == 0 ? new Color(0f, 0f, 0f, 0.05f) : Color.clear;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Tag
                        EditorGUILayout.SelectableLabel(win.Tag, EditorStyles.boldLabel, GUILayout.Height(18), GUILayout.Width(130));

                        // 类型
                        string typeDisplay = win.IsLegacyComboTag ? $"{win.WindowTypeName} (旧版标签)" : win.WindowTypeName;
                        EditorGUILayout.LabelField(typeDisplay, EditorStyles.miniLabel, GUILayout.Width(160));

                        // 时间区间
                        EditorGUILayout.LabelField($"[{win.StartTime:F2}s ~ {win.EndTime:F2}s]", EditorStyles.miniLabel, GUILayout.Width(130));

                        // 持续时长
                        EditorGUILayout.LabelField($"{win.Duration:F2}s", EditorStyles.miniLabel, GUILayout.Width(80));

                        // 路由引用状态
                        if (win.MatchedRouteCount > 0)
                        {
                            GUI.color = Color.green;
                            EditorGUILayout.LabelField($"● 已被 {win.MatchedRouteCount} 条路由引用", EditorStyles.miniLabel, GUILayout.Width(120));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUI.color = new Color(0.9f, 0.7f, 0.2f);
                            EditorGUILayout.LabelField("○ 未被任何路由引用", EditorStyles.miniLabel, GUILayout.Width(120));
                            GUI.color = Color.white;
                        }

                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        private void DrawRoutesComparisonTable(ActionReport report)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("🔀 动作路由窗口要求比对 (Routes Comparison):", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"共 {report.Routes.Count} 条路由", EditorStyles.miniLabel, GUILayout.Width(80));
                }

                if (report.Routes.Count == 0)
                {
                    EditorGUILayout.LabelField("（当前动作未配置任何派生路由或通用路由集）", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                // 表头
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("状态", EditorStyles.boldLabel, GUILayout.Width(85));
                    GUILayout.Label("路由来源", EditorStyles.boldLabel, GUILayout.Width(150));
                    GUILayout.Label("目标动作/事件", EditorStyles.boldLabel, GUILayout.Width(160));
                    GUILayout.Label("触发器", EditorStyles.boldLabel, GUILayout.Width(130));
                    GUILayout.Label("要求窗口 (RequiredWindow)", EditorStyles.boldLabel, GUILayout.Width(170));
                    GUILayout.Label("时间轴对应窗口状态", EditorStyles.boldLabel, GUILayout.Width(220));
                    GUILayout.FlexibleSpace();
                }

                // 表体
                for (int i = 0; i < report.Routes.Count; i++)
                {
                    var item = report.Routes[i];

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // 1. 状态徽章
                        DrawStatusBadge(item.Status);

                        // 2. 来源
                        EditorGUILayout.LabelField(item.RouteSource, EditorStyles.miniLabel, GUILayout.Width(150));

                        // 3. 目标
                        EditorGUILayout.SelectableLabel(item.TargetName, EditorStyles.miniLabel, GUILayout.Height(18), GUILayout.Width(160));

                        // 4. 触发器
                        EditorGUILayout.LabelField(item.TriggerTypeName, EditorStyles.miniLabel, GUILayout.Width(130));

                        // 5. 要求窗口
                        string reqDisplay;
                        if (!string.IsNullOrEmpty(item.RequiredTag))
                        {
                            reqDisplay = $"[{item.RequiredWindowTypeName}] {item.RequiredTag}";
                        }
                        else
                        {
                            reqDisplay = "(无窗口要求)";
                        }
                        EditorGUILayout.LabelField(reqDisplay, EditorStyles.miniLabel, GUILayout.Width(170));

                        // 6. 时间轴匹配详情
                        if (item.Status == RouteMatchStatus.Matched && item.MatchedTimelineWindow != null)
                        {
                            GUI.color = Color.green;
                            EditorGUILayout.LabelField($"✔ 匹配成功 [{item.MatchedTimelineWindow.StartTime:F2}s ~ {item.MatchedTimelineWindow.EndTime:F2}s]", EditorStyles.miniLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }
                        else if (item.Status == RouteMatchStatus.MissingWindow)
                        {
                            GUI.color = new Color(1f, 0.3f, 0.3f);
                            EditorGUILayout.LabelField($"✘ 时间轴缺少窗口 \"{item.RequiredTag}\"", EditorStyles.boldLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }
                        else if (item.Status == RouteMatchStatus.TypeMismatch)
                        {
                            GUI.color = new Color(1f, 0.6f, 0.1f);
                            string timelineType = item.MatchedTimelineWindow?.WindowTypeName ?? "未知";
                            EditorGUILayout.LabelField($"⚠ 类型不符: 时间轴为 [{timelineType}]", EditorStyles.boldLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }
                        else if (item.Status == RouteMatchStatus.LegacyTimelineTag)
                        {
                            GUI.color = new Color(0.9f, 0.7f, 0.2f);
                            EditorGUILayout.LabelField($"○ 匹配旧版标签 \"{item.RequiredTag}\"", EditorStyles.miniLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }
                        else if (item.Status == RouteMatchStatus.MissingTimeline)
                        {
                            GUI.color = Color.magenta;
                            EditorGUILayout.LabelField("✘ 动作未绑定时间轴资产", EditorStyles.boldLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUI.color = Color.gray;
                            EditorGUILayout.LabelField("— 无需时间轴窗口", EditorStyles.miniLabel, GUILayout.Width(220));
                            GUI.color = Color.white;
                        }

                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        private void DrawDiagnosticSuggestions(ActionReport report)
        {
            if (report.IsClean) return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("💡 诊断与排查建议:", EditorStyles.boldLabel);

                if (!report.HasTimeline)
                {
                    EditorGUILayout.HelpBox("【致命错误】当前动作未配置 actionTimelineSO 或 TimelineAsset。请在动作资产 Inspector 中挂载有效的时间轴数据。", MessageType.Error);
                }

                if (report.MissingWindowCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"【时间轴缺失窗口】共 {report.MissingWindowCount} 条路由要求的窗口在时间轴中不存在！\n" +
                        "排查建议：打开该动作对应的时间轴，在【连招窗口轨道】中添加对应的 RouteWindowClip，并在 Inspector 中选择对应的预设窗口。",
                        MessageType.Error
                    );
                }

                if (report.TypeMismatchCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"【窗口类型不符】共 {report.TypeMismatchCount} 条路由存在 Tag 相同但类型不一致的情况！\n" +
                        "排查建议：例如路由要求 ExecuteRouteWindow，而时间轴中配置了 BufferRouteWindow。请统一路由触发器与时间轴片段中的窗口类型。",
                        MessageType.Warning
                    );
                }

                if (report.LegacyTagCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"【旧版标签提示】共 {report.LegacyTagCount} 处匹配到了时间轴中的旧版 comboTag 字符串。\n" +
                        "排查建议：建议在时间轴编辑器中重新选择预设 RouteWindow 对象并保存，以升级为强类型窗口配置。",
                        MessageType.Info
                    );
                }

                if (report.UnusedTimelineWindowCount > 0)
                {
                    var unusedTags = string.Join(", ", report.TimelineWindows.Where(w => w.MatchedRouteCount == 0).Select(w => $"\"{w.Tag}\""));
                    EditorGUILayout.HelpBox(
                        $"【未引用窗口提示】时间轴中配置了以下窗口，但当前动作的所有路由（派生 + 路由集）均未要求进入这些窗口：\n{unusedTags}\n" +
                        "提示：如果这些窗口是预留连招分支，请检查动作的派生路由或通用路由集是否遗漏了配置；若为废弃窗口，可考虑从时间轴中移除。",
                        MessageType.None
                    );
                }
            }
        }

        #endregion

        #region 底部栏与报告导出

        private void DrawBottomBar()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (_selectedTab == 0 && _hasScanned)
                {
                    if (GUILayout.Button("📋 复制 Markdown 汇总报告", EditorStyles.toolbarButton, GUILayout.Width(170)))
                    {
                        CopyMarkdownReportToClipboard(_batchReports);
                    }

                    if (GUILayout.Button("💾 导出报告到文件", EditorStyles.toolbarButton, GUILayout.Width(120)))
                    {
                        ExportMarkdownReportToFile(_batchReports);
                    }
                }
                else if (_selectedTab == 1 && _singleActionReport != null)
                {
                    if (GUILayout.Button("📋 复制单动作 Markdown 报告", EditorStyles.toolbarButton, GUILayout.Width(180)))
                    {
                        CopyMarkdownReportToClipboard(new List<ActionReport> { _singleActionReport });
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Action Timeline Window Validator v1.0", EditorStyles.miniLabel, GUILayout.Width(220));
            }
        }

        #endregion

        #region 核心比对与分析逻辑

        /// <summary>
        /// 对单个动作资产执行深度解析与时间轴窗口匹配比对。
        /// </summary>
        public static ActionReport AnalyzeAction(ActionConfigAsset actionAsset)
        {
            if (actionAsset == null) return null;

            string path = AssetDatabase.GetAssetPath(actionAsset);
            string charName = ExtractCharacterName(path);

            ActionReport report = new()
            {
                ActionAsset = actionAsset,
                ActionName = actionAsset.name,
                AssetPath = path,
                CharacterName = charName
            };

            // 1. 解析时间轴资产
            report.ResolvedTimeline = ResolveTimeline(actionAsset, out UnityEngine.Object sourceAsset, out string sourceDesc);
            report.TimelineSourceAsset = sourceAsset;
            report.TimelineSourceDesc = sourceDesc;

            // 2. 提取时间轴中的所有窗口
            if (report.ResolvedTimeline != null)
            {
                report.TimelineWindows = ExtractTimelineWindows(report.ResolvedTimeline);
            }

            // 3. 收集所有相关路由（自身派生路由 + 路由集）
            List<RouteMatchItem> items = new();

            // 3.1 派生路由
            if (actionAsset.Routes != null)
            {
                for (int i = 0; i < actionAsset.Routes.Count; i++)
                {
                    var route = actionAsset.Routes[i];
                    if (route == null) continue;

                    var item = EvaluateRoute(actionAsset, route, $"自身派生路由 [{i}]", i, report.TimelineWindows, report.HasTimeline);
                    items.Add(item);
                }
            }

            // 3.2 路由集
            if (actionAsset.RouteSets != null)
            {
                foreach (var setAsset in actionAsset.RouteSets)
                {
                    if (setAsset == null || setAsset.Routes == null) continue;

                    for (int i = 0; i < setAsset.Routes.Count; i++)
                    {
                        var route = setAsset.Routes[i];
                        if (route == null) continue;

                        var item = EvaluateRoute(setAsset, route, $"路由集: {setAsset.name} [{i}]", i, report.TimelineWindows, report.HasTimeline);
                        items.Add(item);
                    }
                }
            }

            report.Routes = items;
            return report;
        }

        /// <summary>
        /// 解析动作关联的时间轴资产（优先读取 SO，其次读取 JSON TextAsset）。
        /// </summary>
        public static ActionTimeline ResolveTimeline(ActionConfigAsset actionAsset, out UnityEngine.Object sourceAsset, out string sourceDesc)
        {
            sourceAsset = null;
            sourceDesc = "未配置时间轴";

            if (actionAsset == null) return null;

            // 优先直接使用 ScriptableObject
            if (actionAsset.actionTimelineSO != null)
            {
                sourceAsset = actionAsset.actionTimelineSO;
                sourceDesc = $"ScriptableObject ({actionAsset.actionTimelineSO.name})";
                return actionAsset.actionTimelineSO;
            }

            // 其次尝试从 TextAsset (JSON) 反序列化
            if (actionAsset.TimelineAsset != null)
            {
                sourceAsset = actionAsset.TimelineAsset;
                try
                {
                    var timeline = ATEditor.SerializationUtility.OpenFromJson(actionAsset.TimelineAsset);
                    if (timeline != null)
                    {
                        sourceDesc = $"JSON TextAsset ({actionAsset.TimelineAsset.name})";
                        return timeline;
                    }
                }
                catch (Exception ex)
                {
                    sourceDesc = $"JSON 解析失败: {ex.Message}";
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 从时间轴中提取全部 RouteWindowClip 片段。
        /// </summary>
        private static List<TimelineWindowInfo> ExtractTimelineWindows(ActionTimeline timeline)
        {
            List<TimelineWindowInfo> results = new();
            if (timeline == null) return results;

            // 遍历所有轨道
            foreach (var track in timeline.AllTracks)
            {
                if (track == null || track.clips == null) continue;

                foreach (var clip in track.clips)
                {
                    if (clip is RouteWindowClip rwc)
                    {
                        string tag = string.Empty;
                        string typeName = "None";
                        RouteWindow win = rwc.routewindow;

                        if (win != null)
                        {
                            tag = win.Tag ?? string.Empty;
                            typeName = win.GetType().Name;
                        }
                        else
                        {
                            tag = "(未配置 RouteWindow)";
                            typeName = "Null";
                        }

                        results.Add(new TimelineWindowInfo
                        {
                            Clip = rwc,
                            Window = win,
                            Tag = tag,
                            WindowTypeName = typeName,
                            StartTime = rwc.StartTime,
                            Duration = rwc.Duration,
                            IsLegacyComboTag = false,
                            MatchedRouteCount = 0
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 比对单条路由要求的窗口与时间轴窗口的匹配关系。
        /// </summary>
        private static RouteMatchItem EvaluateRoute(
            ScriptableObject sourceAsset,
            ActionRoute route,
            string sourceDesc,
            int routeIndex,
            List<TimelineWindowInfo> timelineWindows,
            bool hasTimeline)
        {
            string targetName = route.ExecuteType == ExecuteTarget.Action
                ? (route.ExecuteAction != null ? route.ExecuteAction.name : "None (Action)")
                : $"[Event] {route.RouteExecuteEvent}";

            IRouteTrigger trigger = route.TriggerStrategy;
            string triggerTypeName = trigger != null ? trigger.GetType().Name : "无触发器";

            RouteWindow reqWindow = null;
            string reqTag = string.Empty;
            string reqTypeName = "None";

            if (trigger != null)
            {
                Type tType = trigger.GetType();
                FieldInfo reqWinField = tType.GetField("RequiredWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                reqWindow = reqWinField?.GetValue(trigger) as RouteWindow;

                FieldInfo legacyField = tType.GetField("RequiredWindowTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                string legacyTag = legacyField?.GetValue(trigger) as string;

                if (reqWindow != null)
                {
                    reqTag = reqWindow.Tag;
                    reqTypeName = reqWindow.GetType().Name;
                }
                else if (!string.IsNullOrEmpty(legacyTag))
                {
                    reqTag = legacyTag;
                    var match = ActionTagOptions.FindMatchingWindowByTag(legacyTag);
                    reqTypeName = match != null ? match.GetType().Name : "LegacyTag";
                }
            }

            RouteMatchItem item = new()
            {
                RouteSource = sourceDesc,
                SourceAsset = sourceAsset,
                RouteIndex = routeIndex,
                TargetName = targetName,
                TriggerTypeName = triggerTypeName,
                RequiredWindow = reqWindow,
                RequiredTag = reqTag,
                RequiredWindowTypeName = reqTypeName
            };

            // 1. 无需窗口的情况
            if (string.IsNullOrWhiteSpace(reqTag))
            {
                item.Status = RouteMatchStatus.NoWindowRequired;
                item.DetailMessage = "该路由未配置窗口约束，无需时间轴窗口。";
                return item;
            }

            // 2. 缺少时间轴
            if (!hasTimeline || timelineWindows == null)
            {
                item.Status = RouteMatchStatus.MissingTimeline;
                item.DetailMessage = "当前动作未关联有效的时间轴资产，无法提供所需窗口！";
                return item;
            }

            // 3. 在时间轴中查找匹配窗口
            Type reqType = reqWindow?.GetType();

            // 3.1 尝试查找完全匹配项（Tag 一致，且类型一致）
            TimelineWindowInfo matched = timelineWindows.FirstOrDefault(tw =>
                tw.Tag.Equals(reqTag, StringComparison.OrdinalIgnoreCase) &&
                (reqType == null || tw.Window == null || tw.Window.GetType() == reqType));

            if (matched != null)
            {
                item.MatchedTimelineWindow = matched;
                matched.MatchedRouteCount++;

                if (matched.IsLegacyComboTag)
                {
                    item.Status = RouteMatchStatus.LegacyTimelineTag;
                    item.DetailMessage = $"时间轴匹配成功，但时间轴中仍使用旧版 comboTag \"{reqTag}\"，建议升级为 RouteWindow 对象。";
                }
                else
                {
                    item.Status = RouteMatchStatus.Matched;
                    item.DetailMessage = $"完美匹配！时间轴窗口位于 [{matched.StartTime:F2}s ~ {matched.EndTime:F2}s]。";
                }
                return item;
            }

            // 3.2 检查是否存在 Tag 相同但类型不一致的窗口
            TimelineWindowInfo sameTagDifferentType = timelineWindows.FirstOrDefault(tw =>
                tw.Tag.Equals(reqTag, StringComparison.OrdinalIgnoreCase));

            if (sameTagDifferentType != null)
            {
                item.MatchedTimelineWindow = sameTagDifferentType;
                sameTagDifferentType.MatchedRouteCount++;
                item.Status = RouteMatchStatus.TypeMismatch;
                item.DetailMessage = $"时间轴中存在同名 Tag \"{reqTag}\"，但类型不匹配（路由要求: [{reqTypeName}]，时间轴为: [{sameTagDifferentType.WindowTypeName}]）！";
                return item;
            }

            // 3.3 完全未找到窗口
            item.Status = RouteMatchStatus.MissingWindow;
            item.DetailMessage = $"时间轴中完全未找到 Tag 为 \"{reqTag}\" 的 RouteWindowClip 窗口！此路由在此动作中将永远无法被激活。";
            return item;
        }

        #endregion

        #region 批量检索执行与筛选

        private void ExecuteBatchScan()
        {
            if (!Directory.Exists(_searchPath))
            {
                EditorUtility.DisplayDialog("路径不存在", $"指定的检索目录不存在:\n{_searchPath}", "确定");
                return;
            }

            _batchReports.Clear();
            _reportFoldouts.Clear();
            _totalScannedActions = 0;
            _totalScannedRoutes = 0;

            try
            {
                EditorUtility.DisplayProgressBar("检索动作时间轴窗口匹配", "正在检索动作资产...", 0f);

                string[] guids = AssetDatabase.FindAssets("t:ActionConfigAsset", new[] { _searchPath });
                HashSet<string> characterNames = new() { "全部角色" };

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    float progress = (float)i / guids.Length;
                    EditorUtility.DisplayProgressBar("检索动作时间轴窗口匹配", $"正在检查 ({i + 1}/{guids.Length}): {Path.GetFileName(path)}", progress);

                    ActionConfigAsset actionAsset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(path);
                    if (actionAsset == null) continue;

                    _totalScannedActions++;

                    ActionReport report = AnalyzeAction(actionAsset);
                    _batchReports.Add(report);
                    _totalScannedRoutes += report.TotalRouteCount;

                    if (!string.IsNullOrEmpty(report.CharacterName))
                    {
                        characterNames.Add(report.CharacterName);
                    }
                }

                _characterFilterOptions = characterNames.ToArray();
                _characterFilterIndex = 0;
                _hasScanned = true;

                // 默认展开所有存在异常的动作
                foreach (var rep in _batchReports)
                {
                    _reportFoldouts[rep.ActionAsset] = rep.HasErrors || rep.HasWarnings;
                }

                Debug.Log($"[ActionTimelineValidator] 批量扫描完成！共检查 {_totalScannedActions} 个动作、{_totalScannedRoutes} 条路由。");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private IEnumerable<ActionReport> FilterReports(List<ActionReport> source)
        {
            if (source == null) yield break;

            string selectedChar = _characterFilterIndex > 0 && _characterFilterIndex < _characterFilterOptions.Length
                ? _characterFilterOptions[_characterFilterIndex]
                : null;

            foreach (var report in source)
            {
                // 1. 角色筛选
                if (!string.IsNullOrEmpty(selectedChar) && report.CharacterName != selectedChar)
                {
                    continue;
                }

                // 2. 状态筛选
                switch (_statusFilterIndex)
                {
                    case 1: // 仅显示有异常
                        if (report.IsClean) continue;
                        break;
                    case 2: // 仅缺失时间轴
                        if (report.HasTimeline) continue;
                        break;
                    case 3: // 仅缺失窗口
                        if (report.MissingWindowCount == 0) continue;
                        break;
                    case 4: // 仅类型不符
                        if (report.TypeMismatchCount == 0) continue;
                        break;
                    case 5: // 仅存在未引用窗口
                        if (report.UnusedTimelineWindowCount == 0) continue;
                        break;
                }

                // 3. 关键字搜索
                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    bool match = report.ActionName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 report.CharacterName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 report.Routes.Any(r => r.TargetName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                        r.RequiredTag.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!match) continue;
                }

                yield return report;
            }
        }

        private void SetAllFoldouts(bool open)
        {
            foreach (var rep in _batchReports)
            {
                _reportFoldouts[rep.ActionAsset] = open;
            }
        }

        #endregion

        #region 报告生成与导出

        private void CopyMarkdownReportToClipboard(List<ActionReport> reports)
        {
            string markdown = BuildMarkdownReport(reports);
            EditorGUIUtility.systemCopyBuffer = markdown;
            EditorUtility.DisplayDialog("复制成功", "Markdown 格式诊断报告已成功复制到系统剪贴板！", "确定");
        }

        private void ExportMarkdownReportToFile(List<ActionReport> reports)
        {
            string savePath = EditorUtility.SaveFilePanel("导出诊断报告", Application.dataPath, $"ActionTimeline_Validation_{DateTime.Now:yyyyMMdd_HHmmss}", "md");
            if (string.IsNullOrEmpty(savePath)) return;

            string markdown = BuildMarkdownReport(reports);
            File.WriteAllText(savePath, markdown, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("导出成功", $"报告已成功保存至:\n{savePath}", "确定");
        }

        private string BuildMarkdownReport(List<ActionReport> reports)
        {
            StringBuilder sb = new();
            sb.AppendLine("# 动作路由窗口与时间轴匹配诊断报告");
            sb.AppendLine($"> 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"> 检索路径: `{_searchPath}`");
            sb.AppendLine();

            int totalActions = reports.Count;
            int errorActions = reports.Count(r => r.HasErrors);
            int warnActions = reports.Count(r => !r.HasErrors && r.HasWarnings);
            int cleanActions = reports.Count(r => r.IsClean);

            sb.AppendLine("## 汇总概览");
            sb.AppendLine($"| 指标 | 数量 |");
            sb.AppendLine($"| :--- | :--- |");
            sb.AppendLine($"| **检查动作总数** | {totalActions} |");
            sb.AppendLine($"| **完美匹配动作** | {cleanActions} |");
            sb.AppendLine($"| **异常动作 (错误)** | {errorActions} |");
            sb.AppendLine($"| **警告动作** | {warnActions} |");
            sb.AppendLine();

            sb.AppendLine("## 详细诊断列表");
            foreach (var rep in reports)
            {
                string statusEmoji = rep.HasErrors ? "❌" : rep.HasWarnings ? "⚠️" : "✅";
                sb.AppendLine($"### {statusEmoji} 动作: `{rep.ActionName}` ({rep.CharacterName})");
                sb.AppendLine($"- **路径**: `{rep.AssetPath}`");
                sb.AppendLine($"- **时间轴**: {(rep.HasTimeline ? rep.TimelineSourceDesc : "**未配置时间轴！**")}");

                if (rep.Routes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("| 状态 | 路由来源 | 目标 | 触发器 | 要求窗口 | 时间轴对应状态 |");
                    sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :--- |");

                    foreach (var route in rep.Routes)
                    {
                        string icon = route.Status switch
                        {
                            RouteMatchStatus.Matched => "✅",
                            RouteMatchStatus.MissingWindow => "❌ 缺窗口",
                            RouteMatchStatus.TypeMismatch => "⚠️ 类型不符",
                            RouteMatchStatus.LegacyTimelineTag => "🟠 旧标签",
                            RouteMatchStatus.MissingTimeline => "🚫 缺时间轴",
                            _ => "⚪ 无要求"
                        };

                        string reqWin = string.IsNullOrEmpty(route.RequiredTag) ? "—" : $"[{route.RequiredWindowTypeName}] {route.RequiredTag}";
                        sb.AppendLine($"| {icon} | {route.RouteSource} | {route.TargetName} | {route.TriggerTypeName} | {reqWin} | {route.DetailMessage} |");
                    }
                }

                if (rep.UnusedTimelineWindowCount > 0)
                {
                    var unused = string.Join(", ", rep.TimelineWindows.Where(w => w.MatchedRouteCount == 0).Select(w => $"`{w.Tag}`"));
                    sb.AppendLine();
                    sb.AppendLine($"> ⚠️ **未引用的时间轴窗口**: {unused}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region 辅助工具方法

        private static void DrawStatusBadge(RouteMatchStatus status)
        {
            switch (status)
            {
                case RouteMatchStatus.Matched:
                    DrawBadge("✔ 匹配", Color.green, 75);
                    break;
                case RouteMatchStatus.MissingWindow:
                    DrawBadge("✘ 缺窗口", Color.red, 75);
                    break;
                case RouteMatchStatus.TypeMismatch:
                    DrawBadge("⚠ 类型不符", new Color(1f, 0.6f, 0.1f), 75);
                    break;
                case RouteMatchStatus.LegacyTimelineTag:
                    DrawBadge("○ 旧版标签", new Color(0.9f, 0.7f, 0.2f), 75);
                    break;
                case RouteMatchStatus.MissingTimeline:
                    DrawBadge("✘ 缺时间轴", Color.magenta, 75);
                    break;
                default:
                    DrawBadge("— 无约束", Color.gray, 75);
                    break;
            }
        }

        private static void DrawBadge(string text, Color color, float width = 0)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUIStyle badgeStyle = new(EditorStyles.miniButton)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            if (width > 0)
            {
                GUILayout.Label(text, badgeStyle, GUILayout.Width(width), GUILayout.Height(18));
            }
            else
            {
                GUILayout.Label(text, badgeStyle, GUILayout.Height(18));
            }

            GUI.backgroundColor = prev;
        }

        private static string ExtractCharacterName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;

            string normalized = assetPath.Replace('\\', '/');
            string marker = "/Role/";
            int index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                string sub = normalized.Substring(index + marker.Length);
                int slashIndex = sub.IndexOf('/');
                if (slashIndex > 0)
                {
                    return sub.Substring(0, slashIndex);
                }
                return sub;
            }

            string dir = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            return dir != null ? Path.GetFileName(dir) : string.Empty;
        }

        private static string MakeRelativePath(string absolutePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            string target = absolutePath.Replace('\\', '/');

            if (target.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                string rel = target.Substring(projectRoot.Length);
                if (rel.StartsWith("/")) rel = rel.Substring(1);
                return rel;
            }
            return absolutePath;
        }

        #endregion
    }
}
