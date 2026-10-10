using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Skills 配表动作数据源视图
    /// 位于 Overview (概览看板) 与 Actions (动作资产) 之间，
    /// 展示从技能/动作数据源表中读取到的条目，支持分表与角色覆盖路径，
    /// 并指示该配表条目是否已在工程中生成了同名 Action 资产。
    /// 严格遵循固定高宽与固定列宽布局，杜绝界面跳变。
    /// </summary>
    public static class WorkbenchSkillsView
    {
        private static Vector2 _scrollPos;
        private static string _searchQuery = string.Empty;
        private static int _filterMode = 0; // 0: 全部, 1: 已生成 Action, 2: 待生成 Action
        private static bool _showAllInTable = false;

        public static void Draw(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个有效的角色工作区。", MessageType.Info);
                return;
            }

            var cfg = context.EffectiveConfig;

            // 1. 顶部数据源信息条与工具栏（固定高度 26px）
            DrawSourceInfoBar(cfg);

            EditorGUILayout.Space(2f);

            // 2. 过滤与操作工具栏（固定高度 24px）
            var skills = SkillTableDataSourceAdapter.GetDiagnosedSkillsForCharacter(context, _showAllInTable);
            DrawToolbar(skills, context, onRequireRefresh);

            EditorGUILayout.Space(2f);

            // 3. 固定表头
            DrawTableHeader();

            // 4. 固定行高列表
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                var filtered = FilterSkills(skills);
                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到符合条件的配表技能条目。若需查看整张表，可勾选右上角 [显示表内全部条目]。", MessageType.None);
                }
                else
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        DrawSkillRow(filtered[i], i + 1, context, onRequireRefresh);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSourceInfoBar(EffectiveWorkbenchConfig cfg)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(24f));
            {
                var content = new GUIContent("当前配表数据源:", "当前角色正在读取的技能配表 JSON 路径");
                GUILayout.Label(content, EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                GUILayout.Label(cfg.SkillTablePath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                WorkbenchUIStyles.DrawSourceBadge(cfg.SkillTablePathSource, 80f);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToolbar(List<SkillTableItem> skills, WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24f));
            {
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(35f));
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200f));

                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    _searchQuery = string.Empty;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(8f);

                int generatedCount = skills.FindAll(s => s.IsActionGenerated).Count;
                string[] filterOptions = new[] { $"全部 ({skills.Count})", $"已生成 ({generatedCount})", $"待生成 ({skills.Count - generatedCount})" };
                _filterMode = EditorGUILayout.Popup(_filterMode, filterOptions, EditorStyles.toolbarPopup, GUILayout.Width(160f));

                EditorGUILayout.Space(6f);

                _showAllInTable = GUILayout.Toggle(_showAllInTable, "显示表内全部条目", EditorStyles.toolbarButton, GUILayout.Width(135f));

                GUILayout.FlexibleSpace();

                int ungeneratedCount = skills.Count - generatedCount;
                bool canBatchGenerate = ungeneratedCount > 0;
                GUI.enabled = canBatchGenerate;
                var prevColor = GUI.backgroundColor;
                if (canBatchGenerate) GUI.backgroundColor = new Color(0.3f, 0.75f, 0.4f, 1f);

                if (GUILayout.Button(new GUIContent($"批量生成动作 ({ungeneratedCount})", "为当前尚未生成 Action 资产的配表条目推导并批量创建资产"), EditorStyles.toolbarButton, GUILayout.Width(130f)))
                {
                    GUI.backgroundColor = prevColor;
                    var candidates = skills.Where(s => !s.IsActionGenerated).ToList();
                    var plan = ChangePlanner.PlanActionGeneration(context, candidates);
                    ChangePlanReviewWindow.Open(plan, context, () =>
                    {
                        SkillTableDataSourceAdapter.ClearCache();
                        onRequireRefresh?.Invoke();
                    });
                }
                GUI.backgroundColor = prevColor;
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent("重新读取配表", "清除缓存并重新从配表文件读取技能数据"), EditorStyles.toolbarButton, GUILayout.Width(95f)))
                {
                    SkillTableDataSourceAdapter.ClearCache();
                    onRequireRefresh?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(WorkbenchUIStyles.TableHeader);
            {
                GUILayout.Label("#", EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth));
                GUILayout.Label(new GUIContent("技能编号", "技能数字唯一编号"), EditorStyles.miniBoldLabel, GUILayout.Width(75f));
                GUILayout.Label(new GUIContent("配表技能名称", "来自技能配表的技能条目名称"), EditorStyles.miniBoldLabel, GUILayout.Width(240f));
                GUILayout.Label(new GUIContent("动作生成状态", "该技能是否已在工程中生成了对应的动作资产"), EditorStyles.miniBoldLabel, GUILayout.Width(115f));
                GUILayout.Label(new GUIContent("削韧与打断", "打断等级与韧性加成数值"), EditorStyles.miniBoldLabel, GUILayout.Width(105f));
                GUILayout.Label(new GUIContent("技能描述", "技能效果与行为描述"), EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent("操作", "定位或生成操作"), EditorStyles.miniBoldLabel, GUILayout.Width(75f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSkillRow(SkillTableItem item, int index, WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
            if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            }

            {
                // 1. 序号
                GUILayout.Label(index.ToString(), EditorStyles.miniLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 2. 技能 ID
                GUILayout.Label(item.Id.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(75f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 3. 技能名称
                GUILayout.Label(item.Name, EditorStyles.label, GUILayout.Width(240f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 4. Action 状态徽标 (固定尺寸)
                if (item.IsActionGenerated)
                {
                    if (item.IsFuzzyMatched)
                    {
                        var badgeRect = GUILayoutUtility.GetRect(110f, 18f, GUILayout.Width(110f), GUILayout.Height(18f));
                        EditorGUI.DrawRect(badgeRect, new Color(0.15f, 0.55f, 0.75f, 0.95f));
                        GUI.Label(badgeRect, new GUIContent("已关联动作", "名称存在轻微修饰词差异，但ID或核心动作已匹配到工程Action"), WorkbenchUIStyles.BadgeStyle);
                    }
                    else
                    {
                        WorkbenchUIStyles.DrawStatusBadge(true, "已生成动作", 110f);
                    }
                }
                else
                {
                    WorkbenchUIStyles.DrawStatusBadge(false, "待生成动作", 110f);
                }

                GUILayout.Space(5f);

                // 5. 削韧/抗打断
                GUILayout.Label($"Lv.{item.InterruptLevel} / +{item.ResilienceBonus}", EditorStyles.miniLabel, GUILayout.Width(100f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 6. 描述
                GUILayout.Label(string.IsNullOrEmpty(item.Desc) ? "-" : item.Desc, EditorStyles.miniLabel, GUILayout.ExpandWidth(true), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 7. 操作按钮
                if (item.IsActionGenerated && item.MatchedAction?.AssetObject != null)
                {
                    if (GUILayout.Button(new GUIContent("定位", "定位至该动作资产"), EditorStyles.miniButton, GUILayout.Width(65f)))
                    {
                        EditorGUIUtility.PingObject(item.MatchedAction.AssetObject);
                        Selection.activeObject = item.MatchedAction.AssetObject;
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("生成动作", "推导并为此技能创建动作资产"), EditorStyles.miniButton, GUILayout.Width(65f)))
                    {
                        var plan = ChangePlanner.PlanActionGeneration(context, new[] { item });
                        ChangePlanReviewWindow.Open(plan, context, () =>
                        {
                            SkillTableDataSourceAdapter.ClearCache();
                            onRequireRefresh?.Invoke();
                        });
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static List<SkillTableItem> FilterSkills(List<SkillTableItem> all)
        {
            var results = new List<SkillTableItem>();
            foreach (var s in all)
            {
                if (_filterMode == 1 && !s.IsActionGenerated) continue;
                if (_filterMode == 2 && s.IsActionGenerated) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    if (s.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        s.Id.ToString().IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        s.Desc.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                results.Add(s);
            }
            return results;
        }
    }
}
