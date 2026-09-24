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
    /// 路由窗口引用合规性校验工具。
    /// 一键检索 CharacterConfig 目录下所有动作 (Action) 与指令集 (RouteSet) 中的路由，
    /// 检测其 RequiredWindow 是否存在未在 ActionTagConfig 中配置、类型不兼容或残留未升级旧字符串等非法引用。
    /// </summary>
    public sealed class RouteWindowValidatorWindow : EditorWindow
    {
        public enum IssueSeverity
        {
            Error,
            Warning
        }

        public enum RouteWindowIssueType
        {
            [InspectorName("未在配置库注册")]
            UnregisteredWindow,
            [InspectorName("窗口类型与触发器不兼容")]
            TypeIncompatible,
            [InspectorName("残留未升级旧版字符串")]
            LegacyStringTag,
            [InspectorName("窗口标签为空")]
            EmptyTag
        }

        public sealed class RouteWindowIssue
        {
            public ScriptableObject Asset;
            public string AssetPath;
            public string CharacterName;
            public bool IsRouteSet;
            public int RouteIndex;
            public string RouteTargetName;
            public string TriggerTypeName;
            public RouteWindow CurrentWindow;
            public string CurrentWindowTypeName;
            public string CurrentTag;
            public RouteWindowIssueType IssueType;
            public IssueSeverity Severity;
            public string Description;
            public string Suggestion;
            public RouteWindow ConfiguredMatchByTag;
        }

        private const string DefaultSearchPath = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig";

        [SerializeField]
        private string _searchPath = DefaultSearchPath;

        private readonly List<RouteWindowIssue> _issues = new();
        private int _scannedAssetCount;
        private int _scannedActionCount;
        private int _scannedRouteSetCount;
        private int _scannedRouteCount;
        private bool _hasScanned;

        // UI 筛选与滚动
        private Vector2 _scrollPos;
        private string _searchFilter = string.Empty;
        private int _characterFilterIndex = 0;
        private string[] _characterFilterOptions = { "全部角色/怪物" };
        private int _issueTypeFilterIndex = 0;
        private readonly string[] _issueTypeFilterOptions = { "全部异常类型", "未在配置库注册", "类型与触发器不兼容", "残留未升级旧字符串", "窗口标签为空" };

        private readonly Dictionary<ScriptableObject, bool> _assetFoldouts = new();

        [MenuItem("Tools/Action/检索非法路由窗口引用", false, 100)]
        public static void Open()
        {
            var window = GetWindow<RouteWindowValidatorWindow>("路由窗口校验工具");
            window.minSize = new Vector2(750, 480);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawStatistics();
            DrawFilterBar();
            DrawResultsList();
            DrawBottomBar();
        }

        // ────────────────── 顶部标头 ──────────────────

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(" 🔍 路由窗口引用合规性校验工具 (Route Window Validator)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("重载 ActionTagConfig", EditorStyles.toolbarButton, GUILayout.Width(140)))
                {
                    ActionTagOptions.InvalidateCache();
                    ShowNotification(new GUIContent("已刷新 ActionTagConfig 缓存"));
                }
            }
        }

        // ────────────────── 工具栏设置 ──────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("检索根目录:", GUILayout.Width(80));
                    _searchPath = EditorGUILayout.TextField(_searchPath);

                    if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("选择要检索的 ScriptableObjects 目录", _searchPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            if (selected.StartsWith(Application.dataPath))
                            {
                                _searchPath = "Assets" + selected.Substring(Application.dataPath.Length).Replace('\\', '/');
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("路径错误", "请选择位于工程 Assets 目录下的文件夹！", "确定");
                            }
                        }
                    }

                    if (GUILayout.Button("恢复默认", GUILayout.Width(70)))
                    {
                        _searchPath = DefaultSearchPath;
                    }
                }

                // 显示当前加载的配置库信息
                var configuredWindows = ActionTagOptions.GetRouteWindows();
                int winCount = configuredWindows?.Count ?? 0;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = winCount > 0 ? Color.green : Color.red;
                    EditorGUILayout.LabelField($"● ActionTagConfig 已载入预设窗口数: {winCount} 个", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }

                EditorGUILayout.Space(2);
                if (GUILayout.Button("🚀 一键检索非法 Requirewindow 引用", GUILayout.Height(30)))
                {
                    ExecuteScan();
                }
            }
        }

        // ────────────────── 统计信息 ──────────────────

        private void DrawStatistics()
        {
            if (!_hasScanned) return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"已扫描资产: {_scannedAssetCount} (动作: {_scannedActionCount}, 指令集: {_scannedRouteSetCount})", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"扫描路由: {_scannedRouteCount} 条", EditorStyles.boldLabel);

                if (_issues.Count == 0)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✅ 完美！未发现任何非法窗口引用", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    EditorGUILayout.LabelField($"❌ 发现异常引用: {_issues.Count} 处", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
            }
        }

        // ────────────────── 过滤栏 ──────────────────

        private void DrawFilterBar()
        {
            if (!_hasScanned || _issues.Count == 0) return;

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("筛选:", GUILayout.Width(35));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(180));

                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    _searchFilter = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.Space(10);
                GUILayout.Label("角色/怪物:", GUILayout.Width(65));
                _characterFilterIndex = EditorGUILayout.Popup(_characterFilterIndex, _characterFilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(130));

                GUILayout.Space(10);
                GUILayout.Label("类型:", GUILayout.Width(35));
                _issueTypeFilterIndex = EditorGUILayout.Popup(_issueTypeFilterIndex, _issueTypeFilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(150));

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

        // ────────────────── 结果列表 ──────────────────

        private void DrawResultsList()
        {
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("请点击上方的【一键检索非法 Requirewindow 引用】开始扫描。", MessageType.Info);
                return;
            }

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox($"在目录 '{_searchPath}' 下检索完毕，共检查 {_scannedAssetCount} 个资产中的 {_scannedRouteCount} 条路由，所有 Requirewindow 引用完全符合 ActionTagConfig 规范！", MessageType.Info);
                return;
            }

            var filtered = GetFilteredIssues();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // 按资产分组展示
            var groupedByAsset = filtered.GroupBy(i => i.Asset).ToList();

            foreach (var group in groupedByAsset)
            {
                var asset = group.Key;
                if (asset == null) continue;

                if (!_assetFoldouts.TryGetValue(asset, out bool expanded))
                {
                    expanded = true;
                    _assetFoldouts[asset] = true;
                }

                var firstIssue = group.First();
                string typeLabel = firstIssue.IsRouteSet ? "[指令集]" : "[动作]";
                string headerTitle = $"{typeLabel} {asset.name} ({group.Count()} 处异常)";

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _assetFoldouts[asset] = EditorGUILayout.Foldout(expanded, headerTitle, true, EditorStyles.foldoutHeader);

                        if (GUILayout.Button("定位资产", EditorStyles.miniButton, GUILayout.Width(65)))
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }

                    if (_assetFoldouts[asset])
                    {
                        EditorGUI.indentLevel++;
                        foreach (var issue in group)
                        {
                            DrawIssueItem(issue);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssueItem(RouteWindowIssue issue)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string severityIcon = issue.Severity == IssueSeverity.Error ? "❌" : "⚠️";
                    string typeTag = GetIssueTypeTag(issue.IssueType);

                    GUI.color = issue.Severity == IssueSeverity.Error ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.85f, 0.3f);
                    EditorGUILayout.LabelField($"{severityIcon} {typeTag}", EditorStyles.boldLabel, GUILayout.Width(180));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField($"路由 [{issue.RouteIndex}] → 目标: {issue.RouteTargetName}", EditorStyles.miniLabel);

                    GUILayout.FlexibleSpace();

                    // 一键自动对齐/修复按钮
                    if (issue.ConfiguredMatchByTag != null)
                    {
                        if (GUILayout.Button("一键对齐配置库", EditorStyles.miniButtonRight, GUILayout.Width(110)))
                        {
                            FixIssueWithMatch(issue);
                        }
                    }

                    if (GUILayout.Button("清除此窗口", EditorStyles.miniButtonLeft, GUILayout.Width(80)))
                    {
                        ClearIssueWindow(issue);
                    }
                }

                EditorGUILayout.LabelField($"触发器类型: {issue.TriggerTypeName}  |  当前引用: {issue.CurrentWindowTypeName} (Tag: \"{issue.CurrentTag}\")", EditorStyles.miniLabel);

                GUI.color = new Color(0.95f, 0.75f, 0.75f);
                EditorGUILayout.LabelField($"问题描述: {issue.Description}", EditorStyles.wordWrappedMiniLabel);
                GUI.color = Color.white;

                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    GUI.color = new Color(0.75f, 0.95f, 0.75f);
                    EditorGUILayout.LabelField($"修复建议: {issue.Suggestion}", EditorStyles.wordWrappedMiniLabel);
                    GUI.color = Color.white;
                }
            }
        }

        // ────────────────── 底部工具栏 ──────────────────

        private void DrawBottomBar()
        {
            if (!_hasScanned || _issues.Count == 0) return;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("📋 复制 Markdown 检测报告到剪贴板", EditorStyles.toolbarButton, GUILayout.Width(230)))
                {
                    CopyMarkdownReport();
                }

                GUILayout.FlexibleSpace();

                int fixableCount = _issues.Count(i => i.ConfiguredMatchByTag != null);
                if (fixableCount > 0)
                {
                    if (GUILayout.Button($"⚡ 一键修复所有可自动对齐项 ({fixableCount} 项)", EditorStyles.toolbarButton, GUILayout.Width(240)))
                    {
                        BatchFixAllMatches();
                    }
                }
            }
        }

        // ────────────────── 扫描核心逻辑 ──────────────────

        private void ExecuteScan()
        {
            _issues.Clear();
            _scannedAssetCount = 0;
            _scannedActionCount = 0;
            _scannedRouteSetCount = 0;
            _scannedRouteCount = 0;
            _assetFoldouts.Clear();

            if (!Directory.Exists(_searchPath))
            {
                EditorUtility.DisplayDialog("目录不存在", $"无法找到目录: {_searchPath}", "确定");
                return;
            }

            // 1. 确保配置库最新
            ActionTagOptions.InvalidateCache();
            var configuredWindows = ActionTagOptions.GetRouteWindows();
            if (configuredWindows == null || configuredWindows.Count == 0)
            {
                bool proceed = EditorUtility.DisplayDialog("警告", "当前 ActionTagConfig 中尚未配置任何预设 RouteWindow！扫描将把所有窗口标记为未配置，是否继续？", "继续扫描", "取消");
                if (!proceed) return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("扫描路由窗口引用", "正在检索资产清单...", 0f);

                string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { _searchPath });
                HashSet<string> characterNames = new() { "全部角色/怪物" };

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    float progress = (float)i / guids.Length;
                    EditorUtility.DisplayProgressBar("扫描路由窗口引用", $"正在检查 ({i}/{guids.Length}): {Path.GetFileName(path)}", progress);

                    ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (so == null) continue;

                    List<ActionRoute> routes = null;
                    bool isRouteSet = false;

                    if (so is ActionConfigAsset actionAsset)
                    {
                        routes = actionAsset.Routes;
                        _scannedActionCount++;
                        _scannedAssetCount++;
                    }
                    else if (so is ActionRouteSetAsset setAsset)
                    {
                        routes = setAsset.Routes;
                        isRouteSet = true;
                        _scannedRouteSetCount++;
                        _scannedAssetCount++;
                    }
                    else
                    {
                        continue;
                    }

                    string charName = ExtractCharacterName(path);
                    if (!string.IsNullOrEmpty(charName))
                    {
                        characterNames.Add(charName);
                    }

                    if (routes == null || routes.Count == 0) continue;

                    for (int r = 0; r < routes.Count; r++)
                    {
                        _scannedRouteCount++;
                        ActionRoute route = routes[r];
                        if (route == null) continue;

                        InspectRoute(so, path, charName, isRouteSet, r, route);
                    }
                }

                _characterFilterOptions = characterNames.ToArray();
                _characterFilterIndex = 0;
                _hasScanned = true;

                SetAllFoldouts(true);

                Debug.Log($"[RouteWindowValidator] 扫描完成！检查 {_scannedAssetCount} 个资产、{_scannedRouteCount} 条路由，共发现 {_issues.Count} 处非法窗口引用。");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void InspectRoute(ScriptableObject asset, string path, string charName, bool isRouteSet, int routeIndex, ActionRoute route)
        {
            IRouteTrigger trigger = route.TriggerStrategy;
            if (trigger == null) return;

            string targetName = route.ExecuteType == ExecuteTarget.Action
                ? (route.ExecuteAction != null ? route.ExecuteAction.name : "None")
                : $"[Event] {route.RouteExecuteEvent}";

            Type triggerType = trigger.GetType();
            FieldInfo reqWinField = triggerType.GetField("RequiredWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            RouteWindow reqWindow = reqWinField?.GetValue(trigger) as RouteWindow;

            FieldInfo legacyField = triggerType.GetField("RequiredWindowTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            string legacyTag = legacyField?.GetValue(trigger) as string;

            var comboAttr = reqWinField?.GetCustomAttribute<ComboWindowTagAttribute>();

            // ── 情况 1: 配置了 RouteWindow 对象 ──
            if (reqWindow != null)
            {
                string tag = reqWindow.Tag;
                string winTypeName = reqWindow.GetType().Name;

                // 1.1 Tag 为空
                if (string.IsNullOrWhiteSpace(tag))
                {
                    _issues.Add(new RouteWindowIssue
                    {
                        Asset = asset,
                        AssetPath = path,
                        CharacterName = charName,
                        IsRouteSet = isRouteSet,
                        RouteIndex = routeIndex,
                        RouteTargetName = targetName,
                        TriggerTypeName = triggerType.Name,
                        CurrentWindow = reqWindow,
                        CurrentWindowTypeName = winTypeName,
                        CurrentTag = "(Empty)",
                        IssueType = RouteWindowIssueType.EmptyTag,
                        Severity = IssueSeverity.Error,
                        Description = "配置了 RouteWindow 对象，但 Tag 为空！运行时窗口匹配将永远无法成功。",
                        Suggestion = "请在此窗口中配置有效的 Tag，或选择 ActionTagConfig 预设窗口。",
                        ConfiguredMatchByTag = null
                    });
                    return;
                }

                // 1.2 检查是否在 ActionTagConfig 中匹配（类型和 Tag 双重匹配）
                RouteWindow matching = ActionTagOptions.FindMatchingWindow(reqWindow);
                if (matching == null)
                {
                    // 检查是否同一个 Tag 存在于配置库但类型不同
                    RouteWindow sameTagMatch = ActionTagOptions.FindMatchingWindowByTag(tag);

                    string desc;
                    string suggestion;
                    if (sameTagMatch != null)
                    {
                        desc = $"窗口未在 ActionTagConfig 中匹配！Tag \"{tag}\" 在配置库中存在，但类型不匹配（当前: [{winTypeName}]，配置库: [{sameTagMatch.GetType().Name}]）。";
                        suggestion = $"可点击【一键对齐配置库】将类型自动修复为配置库中的 [{sameTagMatch.GetType().Name}]。";
                    }
                    else
                    {
                        desc = $"窗口未在 ActionTagConfig 中配置！类型 [{winTypeName}] 且 Tag \"{tag}\" 在配置库中不存在。";
                        suggestion = $"请在 ActionTagConfig 中新增窗口 \"{tag}\"，或将本路由修改为已有的预设窗口。";
                    }

                    _issues.Add(new RouteWindowIssue
                    {
                        Asset = asset,
                        AssetPath = path,
                        CharacterName = charName,
                        IsRouteSet = isRouteSet,
                        RouteIndex = routeIndex,
                        RouteTargetName = targetName,
                        TriggerTypeName = triggerType.Name,
                        CurrentWindow = reqWindow,
                        CurrentWindowTypeName = winTypeName,
                        CurrentTag = tag,
                        IssueType = RouteWindowIssueType.UnregisteredWindow,
                        Severity = IssueSeverity.Error,
                        Description = desc,
                        Suggestion = suggestion,
                        ConfiguredMatchByTag = sameTagMatch
                    });
                    return;
                }

                // 1.3 在配置库中存在，但检查触发器类型兼容性 (ComboWindowTagAttribute.AllowedTypes)
                if (comboAttr != null && comboAttr.AllowedTypes != null && comboAttr.AllowedTypes.Length > 0)
                {
                    bool isTypeAllowed = false;
                    for (int t = 0; t < comboAttr.AllowedTypes.Length; t++)
                    {
                        if (comboAttr.AllowedTypes[t].IsAssignableFrom(reqWindow.GetType()))
                        {
                            isTypeAllowed = true;
                            break;
                        }
                    }

                    if (!isTypeAllowed)
                    {
                        string allowedNames = string.Join(" / ", comboAttr.AllowedTypes.Select(t => t.Name));
                        _issues.Add(new RouteWindowIssue
                        {
                            Asset = asset,
                            AssetPath = path,
                            CharacterName = charName,
                            IsRouteSet = isRouteSet,
                            RouteIndex = routeIndex,
                            RouteTargetName = targetName,
                            TriggerTypeName = triggerType.Name,
                            CurrentWindow = reqWindow,
                            CurrentWindowTypeName = winTypeName,
                            CurrentTag = tag,
                            IssueType = RouteWindowIssueType.TypeIncompatible,
                            Severity = IssueSeverity.Warning,
                            Description = $"窗口类型与触发器要求不兼容！触发器 [{triggerType.Name}] 仅允许 [{allowedNames}]，当前配置为 [{winTypeName}]。",
                            Suggestion = $"请将此窗口更换为符合触发器类型要求的预设窗口（如 [{allowedNames}]）。",
                            ConfiguredMatchByTag = null
                        });
                    }
                }
            }
            // ── 情况 2: RequiredWindow 为空，但残留旧版字符串 RequiredWindowTag ──
            else if (!string.IsNullOrEmpty(legacyTag))
            {
                RouteWindow matchByTag = ActionTagOptions.FindMatchingWindowByTag(legacyTag);
                string desc;
                string suggestion;

                if (matchByTag != null)
                {
                    desc = $"触发器中仍残留旧版字符串标签 \"{legacyTag}\"，未升级为序列化的 RouteWindow 对象！";
                    suggestion = $"配置库中已存在对应预设窗口 [{matchByTag.GetType().Name}] \"{legacyTag}\"，可点击【一键对齐配置库】完成升级绑定。";
                }
                else
                {
                    desc = $"触发器中残留未配置的旧版字符串标签 \"{legacyTag}\"，且该 Tag 在配置库中不存在。";
                    suggestion = "该标签已废弃或未注册，建议在 ActionTagConfig 中补齐或清理该标签。";
                }

                _issues.Add(new RouteWindowIssue
                {
                    Asset = asset,
                    AssetPath = path,
                    CharacterName = charName,
                    IsRouteSet = isRouteSet,
                    RouteIndex = routeIndex,
                    RouteTargetName = targetName,
                    TriggerTypeName = triggerType.Name,
                    CurrentWindow = null,
                    CurrentWindowTypeName = "[未实例化对象]",
                    CurrentTag = legacyTag,
                    IssueType = RouteWindowIssueType.LegacyStringTag,
                    Severity = IssueSeverity.Warning,
                    Description = desc,
                    Suggestion = suggestion,
                    ConfiguredMatchByTag = matchByTag
                });
            }
        }

        // ────────────────── 修复操作 ──────────────────

        private void FixIssueWithMatch(RouteWindowIssue issue)
        {
            if (issue.ConfiguredMatchByTag == null || issue.Asset == null) return;

            Undo.RecordObject(issue.Asset, "Align Route Window to Config");

            List<ActionRoute> routes = GetRoutesFromAsset(issue.Asset);
            if (routes != null && issue.RouteIndex >= 0 && issue.RouteIndex < routes.Count)
            {
                var trigger = routes[issue.RouteIndex].TriggerStrategy;
                if (trigger != null)
                {
                    Type triggerType = trigger.GetType();
                    FieldInfo reqWinField = triggerType.GetField("RequiredWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo legacyField = triggerType.GetField("RequiredWindowTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    // 创建新实例并赋予相同 Tag
                    RouteWindow newInstance = (RouteWindow)Activator.CreateInstance(issue.ConfiguredMatchByTag.GetType());
                    newInstance.Tag = issue.ConfiguredMatchByTag.Tag;

                    reqWinField?.SetValue(trigger, newInstance);
                    legacyField?.SetValue(trigger, null);

                    EditorUtility.SetDirty(issue.Asset);
                    AssetDatabase.SaveAssets();

                    ShowNotification(new GUIContent($"已修复 {issue.Asset.name} 路由 [{issue.RouteIndex}]！"));
                    // 重新检验此单项
                    _issues.Remove(issue);
                }
            }
        }

        private void ClearIssueWindow(RouteWindowIssue issue)
        {
            if (issue.Asset == null) return;

            Undo.RecordObject(issue.Asset, "Clear Invalid Route Window");

            List<ActionRoute> routes = GetRoutesFromAsset(issue.Asset);
            if (routes != null && issue.RouteIndex >= 0 && issue.RouteIndex < routes.Count)
            {
                var trigger = routes[issue.RouteIndex].TriggerStrategy;
                if (trigger != null)
                {
                    Type triggerType = trigger.GetType();
                    FieldInfo reqWinField = triggerType.GetField("RequiredWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo legacyField = triggerType.GetField("RequiredWindowTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    reqWinField?.SetValue(trigger, null);
                    legacyField?.SetValue(trigger, null);

                    EditorUtility.SetDirty(issue.Asset);
                    AssetDatabase.SaveAssets();

                    ShowNotification(new GUIContent($"已清除 {issue.Asset.name} 路由 [{issue.RouteIndex}] 的窗口引用"));
                    _issues.Remove(issue);
                }
            }
        }

        private void BatchFixAllMatches()
        {
            var fixables = _issues.Where(i => i.ConfiguredMatchByTag != null).ToList();
            if (fixables.Count == 0) return;

            if (!EditorUtility.DisplayDialog("确认批量对齐", $"共有 {fixables.Count} 处异常在 ActionTagConfig 中找到同名 Tag 预设，是否执行批量修复？", "立即修复", "取消"))
            {
                return;
            }

            int fixedCount = 0;
            HashSet<ScriptableObject> dirtyAssets = new();

            try
            {
                for (int i = 0; i < fixables.Count; i++)
                {
                    var issue = fixables[i];
                    Undo.RecordObject(issue.Asset, "Batch Align Route Window");

                    List<ActionRoute> routes = GetRoutesFromAsset(issue.Asset);
                    if (routes != null && issue.RouteIndex >= 0 && issue.RouteIndex < routes.Count)
                    {
                        var trigger = routes[issue.RouteIndex].TriggerStrategy;
                        if (trigger != null)
                        {
                            Type triggerType = trigger.GetType();
                            FieldInfo reqWinField = triggerType.GetField("RequiredWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            FieldInfo legacyField = triggerType.GetField("RequiredWindowTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                            RouteWindow newInstance = (RouteWindow)Activator.CreateInstance(issue.ConfiguredMatchByTag.GetType());
                            newInstance.Tag = issue.ConfiguredMatchByTag.Tag;

                            reqWinField?.SetValue(trigger, newInstance);
                            legacyField?.SetValue(trigger, null);

                            dirtyAssets.Add(issue.Asset);
                            fixedCount++;
                        }
                    }
                }

                foreach (var asset in dirtyAssets)
                {
                    EditorUtility.SetDirty(asset);
                }
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog("修复完成", $"成功批量对齐修复了 {fixedCount} 处路由窗口引用！", "确定");
                ExecuteScan();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RouteWindowValidator] 批量修复出错: {ex}");
            }
        }

        // ────────────────── 辅助方法 ──────────────────

        private static List<ActionRoute> GetRoutesFromAsset(ScriptableObject asset)
        {
            if (asset is ActionConfigAsset actionAsset) return actionAsset.Routes;
            if (asset is ActionRouteSetAsset setAsset) return setAsset.Routes;
            return null;
        }

        private List<RouteWindowIssue> GetFilteredIssues()
        {
            IEnumerable<RouteWindowIssue> result = _issues;

            // 搜索文本过滤
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                string lower = _searchFilter.ToLowerInvariant();
                result = result.Where(i =>
                    (i.Asset != null && i.Asset.name.ToLowerInvariant().Contains(lower)) ||
                    (i.CurrentTag != null && i.CurrentTag.ToLowerInvariant().Contains(lower)) ||
                    (i.CharacterName != null && i.CharacterName.ToLowerInvariant().Contains(lower)) ||
                    (i.Description != null && i.Description.ToLowerInvariant().Contains(lower))
                );
            }

            // 角色过滤
            if (_characterFilterIndex > 0 && _characterFilterIndex < _characterFilterOptions.Length)
            {
                string selectedChar = _characterFilterOptions[_characterFilterIndex];
                result = result.Where(i => string.Equals(i.CharacterName, selectedChar, StringComparison.OrdinalIgnoreCase));
            }

            // 异常类型过滤
            if (_issueTypeFilterIndex > 0)
            {
                RouteWindowIssueType selectedType = (RouteWindowIssueType)(_issueTypeFilterIndex - 1);
                result = result.Where(i => i.IssueType == selectedType);
            }

            return result.ToList();
        }

        private void SetAllFoldouts(bool expanded)
        {
            foreach (var key in _issues.Select(i => i.Asset).Distinct())
            {
                if (key != null) _assetFoldouts[key] = expanded;
            }
        }

        private static string GetIssueTypeTag(RouteWindowIssueType type)
        {
            return type switch
            {
                RouteWindowIssueType.UnregisteredWindow => "[未在配置库注册]",
                RouteWindowIssueType.TypeIncompatible => "[类型与触发器不符]",
                RouteWindowIssueType.LegacyStringTag => "[残留未升级旧字符串]",
                RouteWindowIssueType.EmptyTag => "[窗口标签为空]",
                _ => "[异常]"
            };
        }

        private static string ExtractCharacterName(string path)
        {
            // 例如: Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/xxx.asset
            // 或: Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Monster/TyrfingInfested/xxx.asset
            string normalized = path.Replace('\\', '/');
            const string marker = "CharacterConfig/";
            int idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string sub = normalized.Substring(idx + marker.Length);
                string[] parts = sub.Split('/');
                if (parts.Length >= 2)
                {
                    return parts[1]; // 返回 "Ellen" 或 "TyrfingInfested" 等
                }
            }
            return Path.GetFileNameWithoutExtension(path);
        }

        private void CopyMarkdownReport()
        {
            StringBuilder sb = new();
            sb.AppendLine("# 路由窗口引用合规性检测报告");
            sb.AppendLine();
            sb.AppendLine($"- **检测根路径**: `{_searchPath}`");
            sb.AppendLine($"- **生成时间**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **已扫描资产数**: {_scannedAssetCount} (动作: {_scannedActionCount}, 指令集: {_scannedRouteSetCount})");
            sb.AppendLine($"- **已扫描路由总数**: {_scannedRouteCount}");
            sb.AppendLine($"- **发现异常总数**: {_issues.Count}");
            sb.AppendLine();

            var grouped = _issues.GroupBy(i => i.Asset).ToList();
            foreach (var group in grouped)
            {
                var asset = group.Key;
                if (asset == null) continue;

                sb.AppendLine($"### 📄 `{asset.name}` ({group.Count()} 处异常)");
                sb.AppendLine($"*路径: `{AssetDatabase.GetAssetPath(asset)}`*");
                sb.AppendLine();
                sb.AppendLine("| 路由索引 | 触发器 | 当前引用窗口 | 异常类型 | 问题描述 | 修复建议 |");
                sb.AppendLine("| :---: | :---: | :---: | :---: | :--- | :--- |");

                foreach (var item in group)
                {
                    sb.AppendLine($"| Route[{item.RouteIndex}] | `{item.TriggerTypeName}` | `{item.CurrentWindowTypeName}` (\"`{item.CurrentTag}`\") | {GetIssueTypeTag(item.IssueType)} | {item.Description} | {item.Suggestion} |");
                }
                sb.AppendLine();
            }

            GUIUtility.systemCopyBuffer = sb.ToString();
            ShowNotification(new GUIContent("Markdown 检测报告已复制到剪贴板！"));
        }
    }
}
