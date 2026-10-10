using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Actions 动作资产列表视图
    /// 严格采用固定列宽与固定行高布局，提供搜索过滤、双轨绑定指示与一键对齐修复能力。
    /// </summary>
    public static class WorkbenchActionsView
    {
        private static Vector2 _scrollPos;
        private static string _searchQuery = string.Empty;
        private static int _filterMode = 0; // 0: 全部, 1: 完全绑定, 2: 缺失绑定

        public static void Draw(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个有效的角色工作区。", MessageType.Info);
                return;
            }

            // 1. 顶部固定工具栏
            DrawToolbar(context, onRequireRefresh);

            EditorGUILayout.Space(2f);

            // 2. 固定表头
            DrawTableHeader();

            // 3. 固定行高列表
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                var filteredActions = FilterActions(context.Actions);
                if (filteredActions.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到符合条件的动作资产。", MessageType.None);
                }
                else
                {
                    for (int i = 0; i < filteredActions.Count; i++)
                    {
                        DrawActionRow(filteredActions[i], i + 1, context, onRequireRefresh);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawToolbar(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24f));
            {
                EditorGUILayout.LabelField("🔍 搜索:", GUILayout.Width(45f));
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(220f));

                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    _searchQuery = string.Empty;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(10f);

                string[] filterOptions = new[] { $"全部 ({context.ActionCount})", $"完全绑定 ({context.FullyLinkedActionCount})", $"缺失时间轴 ({context.ActionCount - context.FullyLinkedActionCount})" };
                _filterMode = EditorGUILayout.Popup(_filterMode, filterOptions, EditorStyles.toolbarPopup, GUILayout.Width(170f));

                GUILayout.FlexibleSpace();

                int unlinkedCount = context.ActionCount - context.FullyLinkedActionCount;
                bool canBatchRebind = unlinkedCount > 0;
                GUI.enabled = canBatchRebind;
                var prevColor = GUI.backgroundColor;
                if (canBatchRebind) GUI.backgroundColor = new Color(0.3f, 0.75f, 0.4f, 1f);

                if (GUILayout.Button(new GUIContent($"批量对齐绑定 ({unlinkedCount})", "为当前缺失时间轴绑定的 Action 资产诊断并批量绑定同名 JSON/SO"), EditorStyles.toolbarButton, GUILayout.Width(135f)))
                {
                    GUI.backgroundColor = prevColor;
                    var candidates = context.Actions.Where(a => !a.IsFullyLinked).ToList();
                    var plan = ChangePlanner.PlanTimelineRebind(context, candidates);
                    ChangePlanReviewWindow.Open(plan, context, onRequireRefresh);
                }
                GUI.backgroundColor = prevColor;
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent("重新扫描", "重新扫描当前角色名下的动作资产与时间轴关联"), EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    onRequireRefresh?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(WorkbenchUIStyles.TableHeader);
            {
                GUILayout.Label(new GUIContent("#", "序号"), EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth));
                GUILayout.Label(new GUIContent("动作资产名称", "工程中已存在的 Action 资产文件名"), EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColNameWidth));
                GUILayout.Label(new GUIContent("JSON 时间轴", "是否已成功绑定 JSON 格式时间轴文件"), EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColStatusWidth));
                GUILayout.Label(new GUIContent("SO 时间轴", "是否已成功绑定 ScriptableObject 格式时间轴资产"), EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColStatusWidth));
                GUILayout.Label(new GUIContent("资产相对路径", "动作资产文件在项目中的相对路径"), EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent("操作", "定位或执行对齐修复操作"), EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColActionWidth + 60f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawActionRow(
            ActionAssetIndexItem action,
            int index,
            WorkbenchCharacterContext context,
            Action onRequireRefresh)
        {
            // 斑马条纹背景
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
            if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            }

            {
                // 1. 序号
                GUILayout.Label(index.ToString(), EditorStyles.miniLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 2. 动作名称
                GUILayout.Label(action.ActionName, EditorStyles.label, GUILayout.Width(WorkbenchUIStyles.ColNameWidth), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 3. JSON 状态
                WorkbenchUIStyles.DrawStatusBadge(action.HasTimelineJson, action.HasTimelineJson ? "JSON 正常" : "缺失 JSON", WorkbenchUIStyles.ColStatusWidth - 10f);

                // 4. SO 状态
                WorkbenchUIStyles.DrawStatusBadge(action.HasTimelineSo, action.HasTimelineSo ? "SO 正常" : "缺失 SO", WorkbenchUIStyles.ColStatusWidth - 10f);

                // 5. 路径显示（Selectable 不换行）
                EditorGUILayout.SelectableLabel(action.AssetPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true), GUILayout.Height(18f));

                // 6. 操作按钮区
                EditorGUILayout.BeginHorizontal(GUILayout.Width(WorkbenchUIStyles.ColActionWidth + 60f));
                {
                    if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(45f)))
                    {
                        if (action.AssetObject != null)
                        {
                            EditorGUIUtility.PingObject(action.AssetObject);
                            Selection.activeObject = action.AssetObject;
                        }
                    }

                    bool canRepair = !action.IsFullyLinked;
                    GUI.enabled = canRepair;
                    if (GUILayout.Button("对齐绑定", EditorStyles.miniButtonRight, GUILayout.Width(65f)))
                    {
                        var matchResult = ActionTimelineMatcher.DiagnoseMatch(action, context);
                        if (matchResult.CanAutoRepair && action.AssetObject != null)
                        {
                            ActionTimelineMatcher.AutoBindTimeline(action.AssetObject, matchResult.MatchedJsonItem, matchResult.MatchedSoItem);
                            onRequireRefresh?.Invoke();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("提示", "未在角色时间轴目录中匹配到可供绑定的同名 JSON 或 SO 时间轴。", "确定");
                        }
                    }
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static List<ActionAssetIndexItem> FilterActions(IReadOnlyList<ActionAssetIndexItem> allActions)
        {
            var results = new List<ActionAssetIndexItem>();
            foreach (var a in allActions)
            {
                if (_filterMode == 1 && !a.IsFullyLinked) continue;
                if (_filterMode == 2 && a.IsFullyLinked) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    if (a.ActionName.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        a.AssetPath.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                results.Add(a);
            }
            return results;
        }
    }
}
