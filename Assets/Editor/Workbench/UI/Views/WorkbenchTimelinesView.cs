using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Timelines 时间轴与孤立诊断视图
    /// 严格采用固定列宽与固定行高布局，支持展示当前角色名下的所有时间轴资产并排查孤立未引用的文件。
    /// </summary>
    public static class WorkbenchTimelinesView
    {
        private static Vector2 _scrollPos;
        private static string _searchQuery = string.Empty;
        private static bool _onlyShowOrphans = false;

        public static void Draw(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个有效的角色工作区。", MessageType.Info);
                return;
            }

            var orphans = ActionTimelineMatcher.FindOrphanTimelines(context);
            var orphanPaths = new HashSet<string>(orphans.Select(o => o.AssetPath), StringComparer.OrdinalIgnoreCase);

            // 1. 顶部工具栏
            DrawToolbar(context, orphans.Count, onRequireRefresh);

            EditorGUILayout.Space(2f);

            // 2. 固定表头
            DrawTableHeader();

            // 3. 固定行高列表
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                var filtered = FilterTimelines(context.Timelines, orphanPaths);
                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到符合条件的时间轴资产。", MessageType.None);
                }
                else
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        var item = filtered[i];
                        bool isOrphan = orphanPaths.Contains(item.AssetPath);
                        DrawTimelineRow(item, i + 1, isOrphan);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawToolbar(WorkbenchCharacterContext context, int orphanCount, Action onRequireRefresh)
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

                _onlyShowOrphans = GUILayout.Toggle(_onlyShowOrphans, $"仅显示孤立时间轴 ({orphanCount})", EditorStyles.toolbarButton, GUILayout.Width(170f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("🔄 重新扫描", EditorStyles.toolbarButton, GUILayout.Width(80f)))
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
                GUILayout.Label(new GUIContent("时间轴名称", "时间轴资产文件名称"), EditorStyles.miniBoldLabel, GUILayout.Width(260f));
                GUILayout.Label(new GUIContent("类型", "资产存储格式：JSON 或 ScriptableObject"), EditorStyles.miniBoldLabel, GUILayout.Width(65f));
                GUILayout.Label(new GUIContent("引用状态", "是否已被角色名下的 Action 资产引用"), EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                GUILayout.Label(new GUIContent("相对路径", "文件在项目 Assets 目录下的相对路径"), EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent("操作", "定位文件在 Project 窗口中的位置"), EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTimelineRow(TimelineAssetIndexItem item, int index, bool isOrphan)
        {
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
            if (isOrphan)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.7f, 0.25f, 0.15f, 0.15f));
            }
            else if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            }

            {
                // 1. 序号
                GUILayout.Label(index.ToString(), EditorStyles.miniLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 2. 名称
                GUILayout.Label(item.TimelineName, EditorStyles.label, GUILayout.Width(260f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 3. 类型徽标 (JSON / SO)
                Color typeColor = item.IsJson ? new Color(0.2f, 0.6f, 0.8f, 0.9f) : new Color(0.7f, 0.4f, 0.8f, 0.9f);
                var typeRect = GUILayoutUtility.GetRect(55f, 18f, GUILayout.Width(55f), GUILayout.Height(18f));
                EditorGUI.DrawRect(typeRect, typeColor);
                GUI.Label(typeRect, item.IsJson ? "JSON" : "SO", WorkbenchUIStyles.BadgeStyle);

                GUILayout.Space(10f);

                // 4. 引用状态
                if (isOrphan)
                {
                    WorkbenchUIStyles.DrawStatusBadge(false, "孤立未引用", 100f);
                }
                else
                {
                    WorkbenchUIStyles.DrawStatusBadge(true, "已被 Action 引用", 100f);
                }

                // 5. 路径
                EditorGUILayout.SelectableLabel(item.AssetPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true), GUILayout.Height(18f));

                // 6. 定位按钮
                if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static List<TimelineAssetIndexItem> FilterTimelines(
            IReadOnlyList<TimelineAssetIndexItem> all,
            HashSet<string> orphanPaths)
        {
            var results = new List<TimelineAssetIndexItem>();
            foreach (var t in all)
            {
                if (_onlyShowOrphans && !orphanPaths.Contains(t.AssetPath)) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    if (t.TimelineName.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        t.AssetPath.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                results.Add(t);
            }
            return results;
        }
    }
}
