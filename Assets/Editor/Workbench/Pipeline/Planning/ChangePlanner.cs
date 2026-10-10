using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 资产变更计划生成器 (ChangePlanner)
    /// 遵循安全只读原则：仅推导变更清单与冲突标记，严禁直接执行磁盘写操作。
    /// </summary>
    public static class ChangePlanner
    {
        /// <summary>
        /// 为配表技能规划批量生成 Action 资产计划
        /// </summary>
        public static ChangePlan PlanActionGeneration(
            WorkbenchCharacterContext context,
            IEnumerable<SkillTableItem> skills)
        {
            var plan = new ChangePlan("批量生成 Action 资产计划", "依据配表条目推导将要创建的动作资产及初次时间轴关联");

            if (context == null || skills == null) return plan;

            string targetDir = context.EffectiveConfig?.ActionConfigDirectory;
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = $"Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/{context.Workspace.Category}/{context.Workspace.DisplayName}/Action";
            }

            // 规范化路径分隔符
            targetDir = targetDir.Replace('\\', '/').TrimEnd('/');

            string roleName = GetRoleNameIdentifier(context.Workspace);

            foreach (var skill in skills)
            {
                if (skill == null) continue;

                // 1. 推导标准 Action 资产名称: RoleName_Action_ActionName
                string targetActionName = DeduceActionAssetName(roleName, skill.SkillName);
                string targetPath = $"{targetDir}/{targetActionName}.asset";

                var item = new ChangePlanItem
                {
                    Id = skill.SkillId.ToString(),
                    OperationType = ChangeOperationType.CreateAction,
                    SourceName = skill.SkillName,
                    TargetName = targetActionName,
                    SourcePath = string.Empty,
                    TargetPath = targetPath,
                    UserData = skill
                };

                // 2. 检查命名与相对路径合法性
                if (WorkbenchPathResolver.IsPathTraversing(targetPath))
                {
                    item.ConflictStatus = ChangeConflictStatus.InvalidIdentifier;
                    item.StatusDescription = "检测到潜在的越界路径穿越风险";
                    plan.AddItem(item);
                    continue;
                }

                // 3. 检查目标文件是否已存在
                if (File.Exists(targetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath) != null)
                {
                    item.ConflictStatus = ChangeConflictStatus.ConflictTargetExists;
                    item.StatusDescription = "目标 Action 资产已存在，请勿重复创建";
                    plan.AddItem(item);
                    continue;
                }

                // 4. 智能推导是否已有可连带绑定的时间轴资产
                FindMatchedTimelinesForNewAction(targetActionName, skill.SkillName, context.Timelines, out string matchedJson, out string matchedSo);
                item.MatchedJsonPath = matchedJson;
                item.MatchedSoPath = matchedSo;

                item.ConflictStatus = ChangeConflictStatus.Ready;
                if (!string.IsNullOrEmpty(matchedJson) && !string.IsNullOrEmpty(matchedSo))
                {
                    item.StatusDescription = "就绪 (将同步绑定双轨时间轴)";
                }
                else if (!string.IsNullOrEmpty(matchedJson) || !string.IsNullOrEmpty(matchedSo))
                {
                    item.StatusDescription = "就绪 (将同步绑定单轨时间轴)";
                }
                else
                {
                    item.StatusDescription = "就绪 (未匹配到预置时间轴)";
                }

                plan.AddItem(item);
            }

            return plan;
        }

        /// <summary>
        /// 为已有 Action 资产规划批量对齐绑定时间轴计划
        /// </summary>
        public static ChangePlan PlanTimelineRebind(
            WorkbenchCharacterContext context,
            IEnumerable<ActionAssetIndexItem> actions)
        {
            var plan = new ChangePlan("批量对齐绑定时间轴计划", "诊断当前角色名下未完全绑定的 Action 资产并匹配对应时间轴");

            if (context == null || actions == null) return plan;

            foreach (var action in actions)
            {
                if (action == null) continue;

                var item = new ChangePlanItem
                {
                    Id = action.ActionName,
                    OperationType = ChangeOperationType.BindTimeline,
                    SourceName = action.ActionName,
                    TargetName = action.ActionName,
                    SourcePath = action.AssetPath,
                    TargetPath = action.AssetPath,
                    UserData = action
                };

                if (action.IsFullyLinked)
                {
                    item.ConflictStatus = ChangeConflictStatus.AlreadyLinked;
                    item.StatusDescription = "已处于完全绑定状态";
                    item.IsSelected = false;
                    plan.AddItem(item);
                    continue;
                }

                var matchResult = ActionTimelineMatcher.DiagnoseMatch(action, context);
                if (matchResult.CanAutoRepair)
                {
                    item.MatchedJsonPath = matchResult.MatchedJsonItem?.AssetPath ?? string.Empty;
                    item.MatchedSoPath = matchResult.MatchedSoItem?.AssetPath ?? string.Empty;
                    item.ConflictStatus = ChangeConflictStatus.Ready;
                    item.StatusDescription = "就绪 (匹配到可用时间轴)";
                }
                else
                {
                    item.ConflictStatus = ChangeConflictStatus.SourceNotFound;
                    item.StatusDescription = "未在时间轴目录中匹配到对应名称的资产";
                }

                plan.AddItem(item);
            }

            return plan;
        }

        /// <summary>
        /// 推导规范的 Action 资产名称: {RoleName}_Action_{ActionPart}
        /// </summary>
        public static string DeduceActionAssetName(string roleName, string skillName)
        {
            if (string.IsNullOrEmpty(skillName)) return $"{roleName}_Action_Default";
            if (string.IsNullOrEmpty(roleName)) return skillName;

            // 1. 若已经带有 _Action_，防止重复拼接
            if (skillName.IndexOf("_Action_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 如果没有以当前 roleName 开头，规范化前缀为当前 roleName
                if (!skillName.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
                {
                    int actionIdx = skillName.IndexOf("_Action_", StringComparison.OrdinalIgnoreCase);
                    string actionSuffix = skillName.Substring(actionIdx); // 包含 _Action_xxx
                    return $"{roleName}{actionSuffix}";
                }
                return skillName;
            }

            // 2. 若配表技能名以 RoleName 开头 (如 TyrfingInfested_Attack_01 或 Ellen_Attack_01)
            if (skillName.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                string actionPart = skillName.Substring(roleName.Length + 1);
                if (actionPart.StartsWith("Action_", StringComparison.OrdinalIgnoreCase))
                {
                    actionPart = actionPart.Substring("Action_".Length);
                }
                return $"{roleName}_Action_{actionPart}";
            }

            // 3. 兜底追加 _Action_
            return $"{roleName}_Action_{skillName}";
        }

        /// <summary>
        /// 从工作区获取角色核心英文标识
        /// </summary>
        private static string GetRoleNameIdentifier(SharedWorkspaceDefinition workspace)
        {
            if (workspace == null) return "Role";

            if (!string.IsNullOrEmpty(workspace.FolderName))
            {
                string lastPart = workspace.FolderName.Replace('\\', '/').Split('/').LastOrDefault();
                if (!string.IsNullOrEmpty(lastPart)) return lastPart;
            }

            if (!string.IsNullOrEmpty(workspace.Id))
            {
                int underscoreIdx = workspace.Id.IndexOf('_');
                if (underscoreIdx >= 0 && underscoreIdx < workspace.Id.Length - 1)
                {
                    return workspace.Id.Substring(underscoreIdx + 1);
                }
                return workspace.Id;
            }

            return workspace.DisplayName;
        }

        /// <summary>
        /// 为新建 Action 寻找潜在匹配的 Timeline
        /// </summary>
        private static void FindMatchedTimelinesForNewAction(
            string actionName,
            string skillName,
            IReadOnlyList<TimelineAssetIndexItem> timelines,
            out string matchedJson,
            out string matchedSo)
        {
            matchedJson = string.Empty;
            matchedSo = string.Empty;
            if (timelines == null || timelines.Count == 0) return;

            // 候选匹配关键词集合
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                actionName,
                skillName
            };

            // 提取核心动作短名
            int actionIdx = actionName.IndexOf("_Action_", StringComparison.OrdinalIgnoreCase);
            if (actionIdx >= 0)
            {
                string suffix = actionName.Substring(actionIdx + "_Action_".Length);
                keywords.Add(suffix);
            }

            foreach (var t in timelines)
            {
                if (t == null) continue;
                bool isMatch = false;

                foreach (var kw in keywords)
                {
                    if (string.Equals(t.TimelineName, kw, StringComparison.OrdinalIgnoreCase) ||
                        t.TimelineName.EndsWith("_" + kw, StringComparison.OrdinalIgnoreCase) ||
                        kw.EndsWith("_" + t.TimelineName, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                        break;
                    }
                }

                if (isMatch)
                {
                    if (t.IsJson && string.IsNullOrEmpty(matchedJson)) matchedJson = t.AssetPath;
                    else if (!t.IsJson && string.IsNullOrEmpty(matchedSo)) matchedSo = t.AssetPath;
                }
            }
        }
    }
}
