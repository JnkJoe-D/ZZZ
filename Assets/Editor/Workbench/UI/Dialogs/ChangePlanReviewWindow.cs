using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 变更计划审核确认弹窗 (ChangePlanReviewWindow)
    /// 遵循安全确认、Diff 对比呈现、固定布局与纯中文 Tooltip 规范。
    /// </summary>
    public class ChangePlanReviewWindow : EditorWindow
    {
        private ChangePlan _plan;
        private WorkbenchCharacterContext _context;
        private Action _onCompleted;
        private Vector2 _scrollPos;

        public static void Open(ChangePlan plan, WorkbenchCharacterContext context, Action onCompleted)
        {
            var window = CreateInstance<ChangePlanReviewWindow>();
            window.titleContent = new GUIContent(plan?.Title ?? "变更审核");
            window._plan = plan;
            window._context = context;
            window._onCompleted = onCompleted;
            window.minSize = new Vector2(820f, 480f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (_plan == null)
            {
                EditorGUILayout.HelpBox("变更计划为空或已失效。", MessageType.Info);
                if (GUILayout.Button("关闭", GUILayout.Width(80f))) Close();
                return;
            }

            // 1. 顶部统计指示区 (固定高度 68px)
            DrawSummaryHeader();

            EditorGUILayout.Space(4f);

            // 2. 工具栏 (固定高度 24px)
            DrawSelectionToolbar();

            EditorGUILayout.Space(2f);

            // 3. 表头 (固定列宽)
            DrawTableHeader();

            // 4. 内容列表 (滚动视图，固定行高)
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                for (int i = 0; i < _plan.Items.Count; i++)
                {
                    DrawPlanItemRow(_plan.Items[i], i + 1);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);

            // 5. 底部确认操作栏 (固定高度 36px)
            DrawBottomActionBar();
        }

        private void DrawSummaryHeader()
        {
            Rect headerRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField(new GUIContent(_plan.Title, "本次拟执行的批处理变更计划总称"), EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(_plan.Description))
                {
                    EditorGUILayout.LabelField(_plan.Description, EditorStyles.miniLabel);
                }

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent($"总计: {_plan.TotalCount} 项", "计划中的全部变更条目总数"), EditorStyles.miniBoldLabel, GUILayout.Width(100f));
                    GUILayout.Label(new GUIContent($"已勾选: {_plan.SelectedCount} 项", "用户确认将执行的条目数"), EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                    GUILayout.Label(new GUIContent($"就绪: {_plan.ReadyCount} 项", "无冲突、可安全执行的条目数"), EditorStyles.miniBoldLabel, GUILayout.Width(100f));

                    if (_plan.ConflictCount > 0)
                    {
                        var prevColor = GUI.color;
                        GUI.color = new Color(1f, 0.4f, 0.4f, 1f);
                        GUILayout.Label(new GUIContent($"冲突: {_plan.ConflictCount} 项", "存在重名或源文件缺失的冲突条目，默认已取消勾选"), EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                        GUI.color = prevColor;
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectionToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24f));
            {
                if (GUILayout.Button(new GUIContent("全选安全项", "仅勾选无冲突的健康条目"), EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    _plan.SelectAll(true);
                }

                if (GUILayout.Button(new GUIContent("全部取消", "取消所有勾选"), EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    _plan.SelectAll(false);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent("提示: 带有冲突标记的项默认已被过滤，以防误覆盖资产", "冲突条目无法执行"), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(WorkbenchUIStyles.TableHeader);
            {
                GUILayout.Label(new GUIContent("选", "是否执行勾选"), EditorStyles.miniBoldLabel, GUILayout.Width(30f));
                GUILayout.Label(new GUIContent("#", "序号"), EditorStyles.miniBoldLabel, GUILayout.Width(25f));
                GUILayout.Label(new GUIContent("操作类型", "创建资产或绑定时间轴"), EditorStyles.miniBoldLabel, GUILayout.Width(75f));
                GUILayout.Label(new GUIContent("源条目", "配表技能或源动作名称"), EditorStyles.miniBoldLabel, GUILayout.Width(170f));
                GUILayout.Label(new GUIContent("目标动作资产", "拟生成的 Action 资产名称"), EditorStyles.miniBoldLabel, GUILayout.Width(190f));
                GUILayout.Label(new GUIContent("目标相对路径", "保存文件的目标物理路径"), EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent("就绪状态", "是否存在冲突或已就绪"), EditorStyles.miniBoldLabel, GUILayout.Width(100f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlanItemRow(ChangePlanItem item, int index)
        {
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
            if (item.HasBlockingConflict)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.6f, 0.2f, 0.2f, 0.15f));
            }
            else if (index % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
            }

            {
                // 1. 勾选框
                bool prevEnabled = GUI.enabled;
                if (item.HasBlockingConflict) GUI.enabled = false;
                item.IsSelected = EditorGUILayout.Toggle(item.IsSelected, GUILayout.Width(25f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                GUI.enabled = prevEnabled;

                // 2. 序号
                GUILayout.Label(index.ToString(), EditorStyles.miniLabel, GUILayout.Width(25f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 3. 操作类型
                string opName = item.OperationType switch
                {
                    ChangeOperationType.CreateAction => "创建动作",
                    ChangeOperationType.BindTimeline => "绑定时间轴",
                    ChangeOperationType.RenameAsset => "重命名",
                    ChangeOperationType.MoveAsset => "移动资产",
                    _ => "操作"
                };
                GUILayout.Label(opName, EditorStyles.label, GUILayout.Width(75f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 4. 源条目
                GUILayout.Label(item.SourceName, EditorStyles.label, GUILayout.Width(170f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 5. 目标动作名称
                GUILayout.Label(item.TargetName, EditorStyles.boldLabel, GUILayout.Width(190f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                // 6. 目标相对路径
                EditorGUILayout.SelectableLabel(item.TargetPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true), GUILayout.Height(18f));

                // 7. 就绪状态徽标
                bool isSuccessStatus = item.ConflictStatus == ChangeConflictStatus.Ready;
                string statusText = item.ConflictStatus switch
                {
                    ChangeConflictStatus.Ready => "安全就绪",
                    ChangeConflictStatus.ConflictTargetExists => "目标已存在",
                    ChangeConflictStatus.SourceNotFound => "缺少时间轴",
                    ChangeConflictStatus.InvalidIdentifier => "路径非法",
                    ChangeConflictStatus.AlreadyLinked => "已完全绑定",
                    _ => "冲突拦截"
                };
                WorkbenchUIStyles.DrawStatusBadge(isSuccessStatus, statusText, 95f);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBottomActionBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(32f));
            {
                int executableCount = _plan.Items.Count(i => i.IsSelected && !i.HasBlockingConflict);

                GUILayout.Space(8f);
                if (executableCount == 0)
                {
                    GUILayout.Label("当前未勾选任何可执行的有效条目", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label($"即将执行 {executableCount} 项变更", EditorStyles.miniBoldLabel);
                }

                GUILayout.FlexibleSpace();

                // 取消按钮
                if (GUILayout.Button(new GUIContent("取消", "退出本次变更审核，不保存任何修改"), EditorStyles.toolbarButton, GUILayout.Width(80f), GUILayout.Height(24f)))
                {
                    Close();
                }

                GUILayout.Space(6f);

                // 确认执行按钮
                bool canExecute = executableCount > 0;
                GUI.enabled = canExecute;

                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = canExecute ? new Color(0.25f, 0.75f, 0.35f, 1f) : prevColor;

                if (GUILayout.Button(new GUIContent($"确认执行 ({executableCount})", "立即对勾选项执行批处理写入"), EditorStyles.toolbarButton, GUILayout.Width(130f), GUILayout.Height(24f)))
                {
                    GUI.backgroundColor = prevColor;
                    ExecutePlanWithProgress();
                }
                GUI.backgroundColor = prevColor;
                GUI.enabled = true;

                GUILayout.Space(8f);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ExecutePlanWithProgress()
        {
            try
            {
                var report = WorkbenchBatchExecutor.ExecutePlan(_plan, _context, (prog, title) =>
                {
                    EditorUtility.DisplayProgressBar("执行批处理变更", title, prog);
                });

                EditorUtility.ClearProgressBar();

                string msg = $"批处理执行完毕！\n成功: {report.SuccessCount} 项\n失败: {report.FailedCount} 项";
                EditorUtility.DisplayDialog("执行结果", msg, "确定");

                _onCompleted?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("异常", $"批处理执行中发生异常: {ex.Message}", "确定");
            }
        }
    }
}
