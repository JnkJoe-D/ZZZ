using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 数据源到 Action 资产映射与对齐视图 (WorkbenchActionMappingView)
    /// 提供左右映射对照、多维状态诊断、防串扰模糊对齐、批处理同步与单项右键操作。
    /// 遵循严格固定布局、纯中文与 Tooltip 交互规范。
    /// </summary>
    public static class WorkbenchActionMappingView
    {
        private static Vector2 _scrollPos;
        private static string _searchQuery = string.Empty;
        private static int _filterMode = 0; // 0: 全部, 1: 完全匹配, 2: 名称模糊/Id匹配, 3: 需同步Id, 4: 无匹配资产
        private static bool _showAllInTable = false;

        // 缓存映射数据
        private static string _cachedWorkspaceId;
        private static List<ActionMappingDiagnosticItem> _cachedMappings;

        public static void Draw(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个有效的角色工作区。", MessageType.Info);
                return;
            }

            // 检查缓存有效性
            if (_cachedMappings == null || !string.Equals(_cachedWorkspaceId, context.Workspace.Id, StringComparison.OrdinalIgnoreCase))
            {
                RefreshMappings(context);
            }

            // 1. 顶部统计快照卡片 (固定高度 64px)
            DrawStatCards(_cachedMappings);

            EditorGUILayout.Space(2f);

            // 2. 工具栏与批处理操作下拉 (固定高度 26px)
            DrawToolbar(_cachedMappings, context, onRequireRefresh);

            EditorGUILayout.Space(2f);

            // 3. 固定表头
            DrawTableHeader();

            // 4. 固定行高列表
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                var filtered = FilterItems(_cachedMappings);
                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到符合条件的映射条目。", MessageType.None);
                }
                else
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        DrawMappingRow(filtered[i], i + 1, context, onRequireRefresh);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public static void InvalidateCache()
        {
            _cachedMappings = null;
            _cachedWorkspaceId = null;
            SkillTableDataSourceAdapter.ClearCache();
        }

        private static void RefreshMappings(WorkbenchCharacterContext context)
        {
            _cachedWorkspaceId = context?.Workspace?.Id;
            SkillTableDataSourceAdapter.ClearCache();
            _cachedMappings = ActionMappingDiagnosticService.BuildMappings(context, _showAllInTable);
        }

        private static void DrawStatCards(List<ActionMappingDiagnosticItem> items)
        {
            if (items == null) return;

            int total = items.Count;
            int exactMatch = items.Count(i => i.Status == ActionMappingStatus.ExactMatch);
            int fuzzyNameExactId = items.Count(i => i.Status == ActionMappingStatus.FuzzyNameExactId);
            int needSyncId = items.Count(i => i.CanSyncId);
            int noMatch = items.Count(i => i.Status == ActionMappingStatus.NoMatch);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(76f));
            {
                WorkbenchUIStyles.DrawStatCard("配表条目总数", total.ToString(), "当前配表识别到的技能数据条目总数", 150f, new Color(0.4f, 0.4f, 0.4f, 1f));
                WorkbenchUIStyles.DrawStatCard("完全匹配", exactMatch.ToString(), "名称格式与数字 ID 均完全对齐的健康条目", 150f, new Color(0.2f, 0.7f, 0.35f, 1f));
                WorkbenchUIStyles.DrawStatCard("名称模糊/Id匹配", fuzzyNameExactId.ToString(), "数字 ID 完全匹配，但命名模板有轻微修饰差异", 160f, new Color(0.2f, 0.6f, 0.85f, 1f));
                WorkbenchUIStyles.DrawStatCard("需同步 ID", needSyncId.ToString(), "已有匹配资产但资产内部 ID 尚未同步写入", 150f, new Color(0.9f, 0.6f, 0.2f, 1f));
                WorkbenchUIStyles.DrawStatCard("无匹配资产", noMatch.ToString(), "工程中尚未创建对应 Action 资产的条目", 150f, new Color(0.85f, 0.3f, 0.25f, 1f));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToolbar(
            List<ActionMappingDiagnosticItem> items,
            WorkbenchCharacterContext context,
            Action onRequireRefresh)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24f));
            {
                EditorGUILayout.LabelField(new GUIContent("搜索:", "按技能名称、技能ID、Action名称或Action ID快速过滤"), GUILayout.Width(35f));
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(180f));

                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20f)))
                {
                    _searchQuery = string.Empty;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(6f);

                string[] filterOptions = new[]
                {
                    $"全部 ({items.Count})",
                    $"完全匹配 ({items.Count(i => i.Status == ActionMappingStatus.ExactMatch)})",
                    $"名称模糊/Id匹配 ({items.Count(i => i.Status == ActionMappingStatus.FuzzyNameExactId)})",
                    $"需同步 ID ({items.Count(i => i.CanSyncId)})",
                    $"无匹配资产 ({items.Count(i => i.Status == ActionMappingStatus.NoMatch)})"
                };
                _filterMode = EditorGUILayout.Popup(_filterMode, filterOptions, EditorStyles.toolbarPopup, GUILayout.Width(170f));

                EditorGUILayout.Space(6f);

                bool newShowAll = GUILayout.Toggle(_showAllInTable, new GUIContent("显示表内全量", "是否包含未打上当前角色前缀的表内通用技能条目"), EditorStyles.toolbarButton, GUILayout.Width(95f));
                if (newShowAll != _showAllInTable)
                {
                    _showAllInTable = newShowAll;
                    RefreshMappings(context);
                }

                GUILayout.FlexibleSpace();

                // 批处理快捷按钮 1: 批量同步 ID
                int syncIdCount = items.Count(i => i.CanSyncId);
                if (syncIdCount > 0)
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f, 1f);
                    if (GUILayout.Button(new GUIContent($"批量同步 ID ({syncIdCount})", "将配表技能 ID 批量写入 Action 资产内部的 ID 字段"), EditorStyles.toolbarButton, GUILayout.Width(125f)))
                    {
                        int done = ActionMappingDiagnosticService.BatchSyncAllIds(items);
                        EditorUtility.DisplayDialog("提示", $"成功为 {done} 个 Action 资产同步更新了数字编号 ID！", "确定");
                        RefreshMappings(context);
                        onRequireRefresh?.Invoke();
                    }
                    GUI.backgroundColor = prevBg;
                }

                // 批处理快捷按钮 2: 批量生成无匹配项
                int noMatchCount = items.Count(i => i.Status == ActionMappingStatus.NoMatch);
                if (noMatchCount > 0)
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.75f, 0.4f, 1f);
                    if (GUILayout.Button(new GUIContent($"批量生成动作 ({noMatchCount})", "为无匹配资产的配表条目推导并批量创建 Action 资产"), EditorStyles.toolbarButton, GUILayout.Width(130f)))
                    {
                        var candidates = items.Where(i => i.Status == ActionMappingStatus.NoMatch).Select(i => i.SkillItem).ToList();
                        var plan = ChangePlanner.PlanActionGeneration(context, candidates);
                        ChangePlanReviewWindow.Open(plan, context, () =>
                        {
                            RefreshMappings(context);
                            onRequireRefresh?.Invoke();
                        });
                    }
                    GUI.backgroundColor = prevBg;
                }

                // 批处理下拉菜单按钮
                if (GUILayout.Button(new GUIContent("批处理操作 ▾", "展开批量同步与处理菜单"), EditorStyles.toolbarDropDown, GUILayout.Width(95f)))
                {
                    ShowBatchOperationsMenu(items, context, onRequireRefresh);
                }

                if (GUILayout.Button(new GUIContent("重新诊断", "强制重新从磁盘读取配表并重新计算多维映射评分"), EditorStyles.toolbarButton, GUILayout.Width(75f)))
                {
                    AssetDatabase.Refresh();
                    SkillTableDataSourceAdapter.ClearCache();
                    InvalidateCache();
                    RefreshMappings(context);
                    onRequireRefresh?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void ShowBatchOperationsMenu(
            List<ActionMappingDiagnosticItem> items,
            WorkbenchCharacterContext context,
            Action onRequireRefresh)
        {
            var menu = new GenericMenu();

            int syncIdCount = items.Count(i => i.CanSyncId);
            if (syncIdCount > 0)
            {
                menu.AddItem(new GUIContent($"一键同步所有不匹配的 ID 到 Action 资产 ({syncIdCount})"), false, () =>
                {
                    int done = ActionMappingDiagnosticService.BatchSyncAllIds(items);
                    EditorUtility.DisplayDialog("提示", $"成功同步了 {done} 项 Action ID！", "确定");
                    RefreshMappings(context);
                    onRequireRefresh?.Invoke();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("一键同步所有不匹配的 ID 到 Action 资产 (已全部一致)"));
            }

            int noMatchCount = items.Count(i => i.Status == ActionMappingStatus.NoMatch);
            if (noMatchCount > 0)
            {
                menu.AddItem(new GUIContent($"为所有无匹配项生成 Action 资产 ({noMatchCount})"), false, () =>
                {
                    var candidates = items.Where(i => i.Status == ActionMappingStatus.NoMatch).Select(i => i.SkillItem).ToList();
                    var plan = ChangePlanner.PlanActionGeneration(context, candidates);
                    ChangePlanReviewWindow.Open(plan, context, () =>
                    {
                        RefreshMappings(context);
                        onRequireRefresh?.Invoke();
                    });
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("为所有无匹配项生成 Action 资产 (无待生成项)"));
            }

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("刷新配表与资产缓存"), false, () =>
            {
                SkillTableDataSourceAdapter.ClearCache();
                RefreshMappings(context);
                onRequireRefresh?.Invoke();
            });

            menu.ShowAsContext();
        }

        private static void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(WorkbenchUIStyles.TableHeader);
            {
                GUILayout.Label(new GUIContent("#", "序号"), EditorStyles.miniBoldLabel, GUILayout.Width(30f));
                GUILayout.Label(new GUIContent("配表技能编号", "配表中的技能数字 ID"), EditorStyles.miniBoldLabel, GUILayout.Width(75f));
                GUILayout.Label(new GUIContent("数据源技能名称", "来自技能配表的原始条目名称"), EditorStyles.miniBoldLabel, GUILayout.Width(230f));
                GUILayout.Label(new GUIContent("匹配状态", "双向多维诊断状态指示"), EditorStyles.miniBoldLabel, GUILayout.Width(150f));
                GUILayout.Label(new GUIContent("Action 资产 ID", "工程中 ActionConfigAsset 内部存储的 ID 字段"), EditorStyles.miniBoldLabel, GUILayout.Width(80f));
                GUILayout.Label(new GUIContent("匹配 Action 资产名称", "在工程中识别匹配到的动作资产文件"), EditorStyles.miniBoldLabel, GUILayout.Width(240f));
                GUILayout.Label(new GUIContent("资产相对路径", "Action 资产文件在工程中的相对物理路径"), EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent("操作", "单项操作菜单或右键操作"), EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private const float RowHeight = 28f;

        private static void DrawMappingRow(
            ActionMappingDiagnosticItem item,
            int index,
            WorkbenchCharacterContext context,
            Action onRequireRefresh)
        {
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
            if (item.Status == ActionMappingStatus.NoMatch)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.6f, 0.2f, 0.2f, 0.12f));
            }
            else if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            }

            // 处理整行右键菜单交互
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.ContextClick && rowRect.Contains(currentEvent.mousePosition))
            {
                ShowRowContextMenu(item, context, onRequireRefresh);
                currentEvent.Use();
            }

            {
                // 1. 序号 (垂直居中)
                GUILayout.Label(index.ToString(), WorkbenchUIStyles.CellMiniLabel, GUILayout.Width(30f), GUILayout.Height(RowHeight));

                // 2. 配表技能 ID (垂直居中)
                GUILayout.Label(item.SkillId.ToString(), WorkbenchUIStyles.CellMiniBoldLabel, GUILayout.Width(75f), GUILayout.Height(RowHeight));

                // 3. 配表技能名称 (垂直居中)
                GUILayout.Label(item.SkillName, WorkbenchUIStyles.CellLabel, GUILayout.Width(230f), GUILayout.Height(RowHeight));

                // 4. 匹配状态徽标 (垂直居中，上下等间距 4px)
                EditorGUILayout.BeginVertical(GUILayout.Width(145f), GUILayout.Height(RowHeight));
                GUILayout.FlexibleSpace();
                WorkbenchUIStyles.DrawMappingStatusBadge(item.Status, 145f);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.Space(5f);

                // 5. Action 资产 ID (垂直居中)
                string actionIdText = item.HasAssociatedAction
                    ? (item.ActionId > 0 ? item.ActionId.ToString() : "未写入 (0)")
                    : "-";
                var idStyle = item.CanSyncId ? WorkbenchUIStyles.CellMiniBoldLabel : WorkbenchUIStyles.CellMiniLabel;
                GUILayout.Label(actionIdText, idStyle, GUILayout.Width(80f), GUILayout.Height(RowHeight));

                // 6. Action 资产名称 (垂直居中)
                string actionNameText = item.HasAssociatedAction ? item.ActionName : "(无对应资产)";
                GUILayout.Label(actionNameText, WorkbenchUIStyles.CellLabel, GUILayout.Width(240f), GUILayout.Height(RowHeight));

                // 7. 资产路径 (垂直居中，上下等间距 5px)
                string pathText = item.HasAssociatedAction ? item.ActionAssetPath : "-";
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(RowHeight));
                GUILayout.FlexibleSpace();
                EditorGUILayout.SelectableLabel(pathText, EditorStyles.miniLabel, GUILayout.ExpandWidth(true), GUILayout.Height(18f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                // 8. 操作下拉按钮 (垂直居中，上下等间距 4px)
                EditorGUILayout.BeginVertical(GUILayout.Width(65f), GUILayout.Height(RowHeight));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("操作 ▾", "点击或在整行右击打开操作菜单"), EditorStyles.miniButton, GUILayout.Width(65f), GUILayout.Height(20f)))
                {
                    ShowRowContextMenu(item, context, onRequireRefresh);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void ShowRowContextMenu(
            ActionMappingDiagnosticItem item,
            WorkbenchCharacterContext context,
            Action onRequireRefresh)
        {
            var menu = new GenericMenu();

            if (item.HasAssociatedAction && item.ActionAsset != null)
            {
                menu.AddItem(new GUIContent("定位 Action 资产"), false, () =>
                {
                    EditorGUIUtility.PingObject(item.ActionAsset);
                    Selection.activeObject = item.ActionAsset;
                });

                if (item.CanSyncId)
                {
                    menu.AddItem(new GUIContent($"同步配表 ID ({item.SkillId}) 到 Action 资产"), false, () =>
                    {
                        ActionMappingDiagnosticService.SyncActionId(item.ActionAsset, item.SkillId);
                        RefreshMappings(context);
                        onRequireRefresh?.Invoke();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("同步配表 ID (ID 已一致)"));
                }

                if (item.CanSyncName)
                {
                    menu.AddItem(new GUIContent($"同步 Action 资产名称为标准命名"), false, () =>
                    {
                        string targetName = ChangePlanner.DeduceActionAssetName(context.Workspace.DisplayName, item.SkillName);
                        if (ActionMappingDiagnosticService.SyncActionName(item.MatchedActionItem, targetName))
                        {
                            RefreshMappings(context);
                            onRequireRefresh?.Invoke();
                        }
                    });
                }
            }
            else
            {
                menu.AddItem(new GUIContent("为此技能生成 Action 资产..."), false, () =>
                {
                    var plan = ChangePlanner.PlanActionGeneration(context, new[] { item.SkillItem });
                    ChangePlanReviewWindow.Open(plan, context, () =>
                    {
                        RefreshMappings(context);
                        onRequireRefresh?.Invoke();
                    });
                });
            }

            menu.ShowAsContext();
        }

        private static List<ActionMappingDiagnosticItem> FilterItems(List<ActionMappingDiagnosticItem> all)
        {
            if (all == null) return new List<ActionMappingDiagnosticItem>();

            var results = new List<ActionMappingDiagnosticItem>();
            foreach (var item in all)
            {
                if (_filterMode == 1 && item.Status != ActionMappingStatus.ExactMatch) continue;
                if (_filterMode == 2 && item.Status != ActionMappingStatus.FuzzyNameExactId) continue;
                if (_filterMode == 3 && !item.CanSyncId) continue;
                if (_filterMode == 4 && item.Status != ActionMappingStatus.NoMatch) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    bool match = (item.SkillName?.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (item.ActionName?.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (item.SkillId.ToString().IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (item.ActionId.ToString().IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!match) continue;
                }

                results.Add(item);
            }

            return results;
        }
    }
}
