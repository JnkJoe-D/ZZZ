using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.GamePlay;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 工作台安全批处理执行器 (WorkbenchBatchExecutor)
    /// 遵循原子批处理、异常防护、不中断安全项与全量审计报告原则。
    /// </summary>
    public static class WorkbenchBatchExecutor
    {
        /// <summary>
        /// 执行变更计划中的勾选项
        /// </summary>
        public static BatchExecutionReport ExecutePlan(
            ChangePlan plan,
            WorkbenchCharacterContext context,
            Action<float, string> onProgress = null)
        {
            var report = new BatchExecutionReport
            {
                PlanTitle = plan?.Title ?? "未命名变更计划"
            };

            if (plan == null || plan.TotalCount == 0) return report;

            var itemsToExecute = plan.Items.Where(i => i.IsSelected && !i.HasBlockingConflict).ToList();
            if (itemsToExecute.Count == 0) return report;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < itemsToExecute.Count; i++)
                {
                    var item = itemsToExecute[i];
                    float progress = (float)i / itemsToExecute.Count;
                    onProgress?.Invoke(progress, $"正在处理: {item.TargetName} ({i + 1}/{itemsToExecute.Count})");

                    try
                    {
                        switch (item.OperationType)
                        {
                            case ChangeOperationType.CreateAction:
                                ExecuteCreateAction(item, context, report);
                                break;

                            case ChangeOperationType.BindTimeline:
                                ExecuteBindTimeline(item, context, report);
                                break;

                            default:
                                report.AddResult(item, false, $"暂不支持的操作类型: {item.OperationType}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        report.AddResult(item, false, $"执行异常: {ex.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                onProgress?.Invoke(1f, "完成");
            }

            return report;
        }

        private static void ExecuteCreateAction(
            ChangePlanItem item,
            WorkbenchCharacterContext context,
            BatchExecutionReport report)
        {
            if (string.IsNullOrEmpty(item.TargetPath))
            {
                report.AddResult(item, false, "目标保存路径为空");
                return;
            }

            // 1. 确保目标目录存在
            string fullDirectory = Path.GetDirectoryName(item.TargetPath);
            if (!string.IsNullOrEmpty(fullDirectory) && !Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            // 2. 根据分类选择具体的 ActionConfigAsset 派生类实例
            ActionConfigAsset actionInstance;
            bool isMonster = context?.Workspace != null &&
                             string.Equals(context.Workspace.Category, SharedWorkspaceDefinition.CategoryMonster, StringComparison.OrdinalIgnoreCase);

            if (isMonster)
            {
                actionInstance = ScriptableObject.CreateInstance<MonsterActionConfigAsset>();
            }
            else
            {
                actionInstance = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            }

            // 3. 注入基础字段
            actionInstance.Name = item.TargetName;
            if (item.UserData is SkillTableItem skill)
            {
                actionInstance.ID = skill.SkillId;
            }

            // 4. 初次连带时间轴绑定
            if (!string.IsNullOrEmpty(item.MatchedJsonPath))
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(item.MatchedJsonPath);
                if (textAsset != null)
                {
                    actionInstance.TimelineAsset = textAsset;
                }
            }

            if (!string.IsNullOrEmpty(item.MatchedSoPath))
            {
                var soAsset = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTimeline>(item.MatchedSoPath);
                if (soAsset != null)
                {
                    actionInstance.actionTimelineSO = soAsset;
                }
            }

            // 5. 写入 Unity 资产数据库
            AssetDatabase.CreateAsset(actionInstance, item.TargetPath);
            EditorUtility.SetDirty(actionInstance);

            report.AddResult(item, true, $"成功创建 Action 资产: {item.TargetPath}");
        }

        private static void ExecuteBindTimeline(
            ChangePlanItem item,
            WorkbenchCharacterContext context,
            BatchExecutionReport report)
        {
            if (string.IsNullOrEmpty(item.TargetPath))
            {
                report.AddResult(item, false, "目标资产路径为空");
                return;
            }

            var actionAsset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(item.TargetPath);
            if (actionAsset == null)
            {
                report.AddResult(item, false, $"未能加载目标 Action 资产: {item.TargetPath}");
                return;
            }

            bool dirty = false;

            if (!string.IsNullOrEmpty(item.MatchedJsonPath) && actionAsset.TimelineAsset == null)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(item.MatchedJsonPath);
                if (textAsset != null)
                {
                    actionAsset.TimelineAsset = textAsset;
                    dirty = true;
                }
            }

            if (!string.IsNullOrEmpty(item.MatchedSoPath) && actionAsset.actionTimelineSO == null)
            {
                var soAsset = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTimeline>(item.MatchedSoPath);
                if (soAsset != null)
                {
                    actionAsset.actionTimelineSO = soAsset;
                    dirty = true;
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(actionAsset);
                report.AddResult(item, true, "成功绑定时间轴资产");
            }
            else
            {
                report.AddResult(item, true, "无需重复绑定或对应时间轴资源不可读");
            }
        }
    }
}
