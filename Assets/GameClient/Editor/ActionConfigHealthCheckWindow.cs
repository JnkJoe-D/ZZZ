using System;
using System.Collections.Generic;
using Game.Logic;
using ATEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.ActionConfig
{
    public sealed class ActionConfigHealthCheckWindow : EditorWindow
    {
        private const string ActionRootFolder = "Assets/Resources/Serializations/ScriptableObjects/Action";

        private readonly List<ActionConfigHealthIssue> issues = new();
        private Vector2 scrollPosition;
        private bool showWarnings = true;
        private bool showErrors = true;
        private bool autoSelectContext = true;

        [MenuItem("ATEditor/动作配置健康检查")]
        public static void Open()
        {
            ActionConfigHealthCheckWindow window = GetWindow<ActionConfigHealthCheckWindow>("动作配置健康检查");
            window.minSize = new Vector2(760f, 420f);
            window.Scan();
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            DrawIssues();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("重新扫描", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    Scan();
                }

                GUILayout.Space(10f);
                showErrors = GUILayout.Toggle(showErrors, "Errors", EditorStyles.toolbarButton, GUILayout.Width(70f));
                showWarnings = GUILayout.Toggle(showWarnings, "Warnings", EditorStyles.toolbarButton, GUILayout.Width(82f));
                autoSelectContext = GUILayout.Toggle(autoSelectContext, "点击时定位对象", EditorStyles.toolbarButton, GUILayout.Width(120f));
                GUILayout.FlexibleSpace();
                GUILayout.Label(ActionRootFolder, EditorStyles.miniLabel);
            }
        }

        private void DrawSummary()
        {
            int errorCount = 0;
            int warningCount = 0;
            foreach (ActionConfigHealthIssue issue in issues)
            {
                if (issue.Severity == ActionConfigIssueSeverity.Error)
                {
                    errorCount++;
                }
                else if (issue.Severity == ActionConfigIssueSeverity.Warning)
                {
                    warningCount++;
                }
            }

            EditorGUILayout.HelpBox(
                $"扫描完成：{errorCount} 个错误，{warningCount} 个警告。",
                errorCount > 0 ? MessageType.Error : warningCount > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void DrawIssues()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (ActionConfigHealthIssue issue in issues)
            {
                if (issue.Severity == ActionConfigIssueSeverity.Error && !showErrors)
                {
                    continue;
                }

                if (issue.Severity == ActionConfigIssueSeverity.Warning && !showWarnings)
                {
                    continue;
                }

                DrawIssue(issue);
            }

            if (issues.Count == 0)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("没有发现配置问题。", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(ActionConfigHealthIssue issue)
        {
            MessageType messageType = issue.Severity == ActionConfigIssueSeverity.Error
                ? MessageType.Error
                : MessageType.Warning;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"[{issue.Code}] {issue.Message}", messageType);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField("Action", issue.Action, typeof(ActionConfigAsset), false);
                    if (GUILayout.Button("选中 Action", GUILayout.Width(90f)))
                    {
                        SelectObject(issue.Action);
                    }
                }

                if (issue.ContextObject != null && issue.ContextObject != issue.Action)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField("Context", issue.ContextObject, typeof(Object), false);
                        if (GUILayout.Button("选中 Context", GUILayout.Width(90f)))
                        {
                            SelectObject(issue.ContextObject);
                        }
                    }
                }

                if (autoSelectContext && Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    SelectObject(issue.ContextObject != null ? issue.ContextObject : issue.Action);
                    Event.current.Use();
                }
            }
        }

        private static void SelectObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private void Scan()
        {
            issues.Clear();
            issues.AddRange(ActionConfigHealthScanner.ScanAll(ActionRootFolder));
        }
    }

    internal static class ActionConfigHealthScanner
    {
        public static List<ActionConfigHealthIssue> ScanAll(string rootFolder)
        {
            List<ActionConfigHealthIssue> results = new();
            HashSet<string> configuredComboTags = new(ActionTagOptions.GetComboWindowTags());
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { rootFolder });
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ActionConfigAsset action = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(path);
                if (action == null)
                {
                    continue;
                }

                ScanAction(action, configuredComboTags, results);
            }

            return results;
        }

        private static void ScanAction(
            ActionConfigAsset action,
            HashSet<string> configuredComboTags,
            List<ActionConfigHealthIssue> results)
        {
            if (action.TimelineAsset == null)
            {
                AddError(results, action, action, "MissingTimeline", "TimelineAsset 为空，运行时无法播放这个动作。");
            }

            if (action.CompleteMode == ActionCompleteMode.TransitToAction && action.CompleteAction == null)
            {
                AddError(results, action, action, "MissingCompleteAction", "CompleteMode 是 TransitToAction，但 CompleteAction 为空。");
            }

            if (action.PlaybackSpeed <= 0f)
            {
                AddWarning(results, action, action, "InvalidPlaybackSpeed", "PlaybackSpeed 小于或等于 0，动作时间不会正常推进。");
            }

            TimelineScanResult timeline = ScanTimeline(action, configuredComboTags, results);
            ScanCompletionPolicy(action, timeline, results);
            ScanRoutes(action, timeline, configuredComboTags, results);
        }

        private static TimelineScanResult ScanTimeline(
            ActionConfigAsset action,
            HashSet<string> configuredComboTags,
            List<ActionConfigHealthIssue> results)
        {
            TimelineScanResult result = new TimelineScanResult();
            if (action.TimelineAsset == null)
            {
                return result;
            }

            SkillTimeline timeline = ATEditor.SerializationUtility.OpenFromJson(action.TimelineAsset);
            if (timeline == null)
            {
                AddError(results, action, action.TimelineAsset, "TimelineParseFailed", "Timeline JSON 无法反序列化。");
                return result;
            }

            try
            {
                result.HasTimeline = true;
                result.IsLoop = timeline.isLoop;
                result.Duration = timeline.Duration;

                foreach (TrackBase track in timeline.AllTracks)
                {
                    if (track == null || !track.isEnabled || !track.CanPlay || track.clips == null)
                    {
                        continue;
                    }

                    foreach (ClipBase clip in track.clips)
                    {
                        if (clip is not ComboWindowClip comboWindow || !comboWindow.isEnabled)
                        {
                            continue;
                        }

                        result.HasComboWindow = true;
                        if (string.IsNullOrWhiteSpace(comboWindow.comboTag))
                        {
                            AddError(results, action, action.TimelineAsset, "EmptyComboTag", "Timeline 中存在空的 ComboWindow 标签。");
                            continue;
                        }

                        result.ComboTags.Add(comboWindow.comboTag);
                        if (configuredComboTags.Count > 0 && !configuredComboTags.Contains(comboWindow.comboTag))
                        {
                            AddWarning(
                                results,
                                action,
                                action.TimelineAsset,
                                "UnregisteredComboTag",
                                $"Timeline 使用了未登记的 ComboWindow 标签 '{comboWindow.comboTag}'。");
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }

            return result;
        }

        private static void ScanCompletionPolicy(
            ActionConfigAsset action,
            TimelineScanResult timeline,
            List<ActionConfigHealthIssue> results)
        {
            if (!timeline.HasTimeline)
            {
                return;
            }

            if (timeline.IsLoop && action.CompleteMode == ActionCompleteMode.TransitToAction)
            {
                AddWarning(results, action, action.TimelineAsset, "LoopTransitUnreachable", "Timeline 是循环的，CompleteAction 通常不会自然触发。");
            }

            if (!timeline.IsLoop && action.CompleteMode == ActionCompleteMode.Stay)
            {
                AddWarning(results, action, action.TimelineAsset, "StayNonLoop", "非循环 Timeline 使用 Stay，动作结束后会停在当前状态，容易残留不可输入状态。");
            }

            if (!timeline.IsLoop &&
                action.CompleteMode == ActionCompleteMode.Default &&
                IsSustainedGroundState(action.EnterState))
            {
                AddWarning(results, action, action.TimelineAsset, "GroundLoopExpected", "Idle/Jog/Dash 的持续动作使用非循环 Timeline + Default，播放结束会回到 Idle。");
            }
        }

        private static void ScanRoutes(
            ActionConfigAsset action,
            TimelineScanResult timeline,
            HashSet<string> configuredComboTags,
            List<ActionConfigHealthIssue> results)
        {
            List<ActionRoute> routes = new();
            action.CollectEffectiveRoutes(routes);
            HashSet<string> missingTimelineTags = new();
            HashSet<string> unregisteredRouteTags = new();

            for (int i = 0; i < routes.Count; i++)
            {
                ActionRoute route = routes[i];
                if (route == null)
                {
                    continue;
                }

                if (route.ExecuteAction == null)
                {
                    AddError(results, action, action, "RouteMissingNextAction", $"第 {i + 1} 条 Route 的 NextAction 为空。");
                }

                if (string.IsNullOrWhiteSpace(route.RequiredWindowTag))
                {
                    AddError(results, action, action, "RouteEmptyTag", $"第 {i + 1} 条 Route 的 RequiredWindowTag 为空。");
                    continue;
                }

                if (configuredComboTags.Count > 0 &&
                    !configuredComboTags.Contains(route.RequiredWindowTag) &&
                    unregisteredRouteTags.Add(route.RequiredWindowTag))
                {
                    AddWarning(
                        results,
                        action,
                        action,
                        "UnregisteredRouteTag",
                        $"Route 使用了未登记的窗口标签 '{route.RequiredWindowTag}'。");
                }

                if (timeline.HasTimeline &&
                    timeline.HasComboWindow &&
                    !timeline.ComboTags.Contains(route.RequiredWindowTag) &&
                    missingTimelineTags.Add(route.RequiredWindowTag))
                {
                    AddWarning(
                        results,
                        action,
                        action.TimelineAsset,
                        "RouteTagMissingInTimeline",
                        $"Route 需要窗口标签 '{route.RequiredWindowTag}'，但当前 Timeline 没有这个 ComboWindow。");
                }

                if (!Enum.IsDefined(typeof(CommandTriggerMode), route.TriggerMode))
                {
                    AddError(results, action, action, "InvalidTriggerMode", $"第 {i + 1} 条 Route 的 TriggerMode 不在合法枚举范围内。");
                }
            }
        }

        private static bool IsSustainedGroundState(ActionState state)
        {
            return state == ActionState.Idle || state == ActionState.Jog || state == ActionState.Dash;
        }

        private static void AddError(
            List<ActionConfigHealthIssue> results,
            ActionConfigAsset action,
            Object contextObject,
            string code,
            string message)
        {
            results.Add(new ActionConfigHealthIssue(ActionConfigIssueSeverity.Error, code, message, action, contextObject));
        }

        private static void AddWarning(
            List<ActionConfigHealthIssue> results,
            ActionConfigAsset action,
            Object contextObject,
            string code,
            string message)
        {
            results.Add(new ActionConfigHealthIssue(ActionConfigIssueSeverity.Warning, code, message, action, contextObject));
        }

        private sealed class TimelineScanResult
        {
            public bool HasTimeline;
            public bool IsLoop;
            public bool HasComboWindow;
            public float Duration;
            public readonly HashSet<string> ComboTags = new();
        }
    }

    internal enum ActionConfigIssueSeverity
    {
        Error,
        Warning
    }

    internal sealed class ActionConfigHealthIssue
    {
        public ActionConfigHealthIssue(
            ActionConfigIssueSeverity severity,
            string code,
            string message,
            ActionConfigAsset action,
            Object contextObject)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Action = action;
            ContextObject = contextObject;
        }

        public ActionConfigIssueSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public ActionConfigAsset Action { get; }
        public Object ContextObject { get; }
    }
}
