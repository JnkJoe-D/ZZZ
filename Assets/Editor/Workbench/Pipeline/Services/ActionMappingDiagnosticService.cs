using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.GamePlay;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 配表数据源到实际 Action 资产的多维映射与诊断服务
    /// 提供高置信度防串扰模糊对齐算法、最佳独占分配及 ID/名称双向同步能力。
    /// </summary>
    public static class ActionMappingDiagnosticService
    {
        /// <summary>
        /// 对当前角色的配表技能与 Action 资产执行全量多维诊断与最优映射构建
        /// </summary>
        public static List<ActionMappingDiagnosticItem> BuildMappings(
            WorkbenchCharacterContext context,
            bool showAllSkillsInTable = false)
        {
            var results = new List<ActionMappingDiagnosticItem>();
            if (context == null) return results;

            // 1. 获取配表技能清单
            var skills = SkillTableDataSourceAdapter.GetDiagnosedSkillsForCharacter(context, showAllSkillsInTable);
            if (skills == null || skills.Count == 0) return results;

            // 2. 获取当前角色名下的 Action 资产清单
            var actions = context.Actions ?? Array.Empty<ActionAssetIndexItem>();
            string roleName = GetShortRoleName(context.Workspace);

            // 3. 构建候选匹配矩阵 (Candidate Pairs) 并评分
            var candidatePairs = new List<ScoredPair>();
            for (int sIdx = 0; sIdx < skills.Count; sIdx++)
            {
                var skill = skills[sIdx];
                for (int aIdx = 0; aIdx < actions.Count; aIdx++)
                {
                    var action = actions[aIdx];
                    float score = CalculateMatchScore(skill, action, roleName);
                    if (score > 100f) // 仅保留具备合理置信度的候选
                    {
                        candidatePairs.Add(new ScoredPair(sIdx, aIdx, score));
                    }
                }
            }

            // 4. 按得分降序排序，执行全局最优贪心独占分配 (1:1 Best Match)
            candidatePairs.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));

            var assignedSkills = new Dictionary<int, ScoredPair>();
            var assignedActions = new HashSet<int>();

            foreach (var pair in candidatePairs)
            {
                if (assignedSkills.ContainsKey(pair.SkillIndex)) continue;
                if (assignedActions.Contains(pair.ActionIndex)) continue;

                // 成功建立 1:1 独占锁定
                assignedSkills[pair.SkillIndex] = pair;
                assignedActions.Add(pair.ActionIndex);
            }

            // 5. 组合诊断结果并计算精确匹配状态
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                var item = new ActionMappingDiagnosticItem
                {
                    SkillItem = skill
                };

                if (assignedSkills.TryGetValue(i, out var matchPair))
                {
                    var matchedAction = actions[matchPair.ActionIndex];
                    item.MatchedActionItem = matchedAction;
                    item.MatchScore = matchPair.Score;

                    DetermineStatus(item, roleName);
                }
                else
                {
                    item.Status = ActionMappingStatus.NoMatch;
                    item.StatusDescription = "未在工程中匹配到对应名称或 ID 的 Action 资产";
                }

                results.Add(item);
            }

            return results;
        }

        /// <summary>
        /// 针对单个 Skill 执行最佳匹配诊断
        /// </summary>
        public static ActionMappingDiagnosticItem DiagnoseSingleSkill(SkillTableItem skill, WorkbenchCharacterContext context)
        {
            if (skill == null) return null;
            string roleName = GetShortRoleName(context?.Workspace);
            var actions = context?.Actions ?? Array.Empty<ActionAssetIndexItem>();

            ActionAssetIndexItem bestAction = null;
            float bestScore = 100f;

            foreach (var action in actions)
            {
                float score = CalculateMatchScore(skill, action, roleName);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }

            var item = new ActionMappingDiagnosticItem
            {
                SkillItem = skill,
                MatchedActionItem = bestAction,
                MatchScore = bestAction != null ? bestScore : 0f
            };

            if (bestAction != null)
            {
                DetermineStatus(item, roleName);
            }
            else
            {
                item.Status = ActionMappingStatus.NoMatch;
                item.StatusDescription = "未在工程中匹配到对应名称或 ID 的 Action 资产";
            }

            return item;
        }

        /// <summary>
        /// 计算 Skill 与 Action 之间的匹配置信度评分
        /// </summary>
        public static float CalculateMatchScore(SkillTableItem skill, ActionAssetIndexItem action, string roleName)
        {
            if (skill == null || action == null) return -9999f;

            float score = 0f;

            int actionAssetId = action.AssetObject != null ? action.AssetObject.ID : 0;
            string skillName = skill.Name ?? string.Empty;
            string actionName = action.ActionName ?? string.Empty;

            // 提取核心动作部分
            string skillPart = ExtractCoreActionPart(skillName, roleName);
            string actionPart = ExtractCoreActionPart(actionName, roleName);

            // 1. 编号/后缀防串扰检查 (例如 01_01 vs 01_02 决不能混淆)
            string skillNumberPattern = ExtractNumberSuffix(skillPart);
            string actionNumberPattern = ExtractNumberSuffix(actionPart);
            if (!string.IsNullOrEmpty(skillNumberPattern) && !string.IsNullOrEmpty(actionNumberPattern))
            {
                if (!string.Equals(skillNumberPattern, actionNumberPattern, StringComparison.OrdinalIgnoreCase))
                {
                    // 编号明确不一致（如 01_01 vs 01_02），严重降权！
                    return -5000f;
                }
                else
                {
                    // 编号明确一致，奖励强分数
                    score += 500f;
                }
            }

            // 2. 特殊修饰符对齐检查 (_Near, _Ex, _Enhanced)
            bool skillHasNear = skillPart.IndexOf("_Near", StringComparison.OrdinalIgnoreCase) >= 0;
            bool actionHasNear = actionPart.IndexOf("_Near", StringComparison.OrdinalIgnoreCase) >= 0;
            if (skillHasNear != actionHasNear) score -= 300f;
            else if (skillHasNear && actionHasNear) score += 200f;

            bool skillHasEx = skillPart.IndexOf("_Ex", StringComparison.OrdinalIgnoreCase) >= 0;
            bool actionHasEx = actionPart.IndexOf("_Ex", StringComparison.OrdinalIgnoreCase) >= 0;
            if (skillHasEx != actionHasEx) score -= 300f;
            else if (skillHasEx && actionHasEx) score += 200f;

            // 3. ID 匹配权重大于一切
            if (skill.Id > 0 && actionAssetId > 0)
            {
                if (skill.Id == actionAssetId)
                {
                    score += 1500f; // ID 全等给予极高基础分
                }
                else
                {
                    score -= 400f; // 双方都有 ID 但不相等，显著惩罚
                }
            }

            // 4. 名称强一致性规则 (标准 _Action_ 中缀匹配)
            if (SkillTableDataSourceAdapter.IsSkillMatchAction(skillName, actionName, roleName))
            {
                score += 800f;
            }

            // 5. 核心词素包含与分词重合度
            if (!string.IsNullOrEmpty(skillPart) && !string.IsNullOrEmpty(actionPart))
            {
                if (string.Equals(skillPart, actionPart, StringComparison.OrdinalIgnoreCase))
                {
                    score += 700f;
                }
                else if (actionPart.IndexOf(skillPart, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         skillPart.IndexOf(actionPart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 450f;
                }

                // 分词 Jaccard 相似度
                var skillTokens = skillPart.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                var actionTokens = actionPart.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

                int overlap = skillTokens.Intersect(actionTokens, StringComparer.OrdinalIgnoreCase).Count();
                int totalDistinct = skillTokens.Union(actionTokens, StringComparer.OrdinalIgnoreCase).Count();

                if (totalDistinct > 0)
                {
                    float tokenSimilarity = (float)overlap / totalDistinct;
                    score += tokenSimilarity * 350f;
                }
            }

            return score;
        }

        private static void DetermineStatus(ActionMappingDiagnosticItem item, string roleName)
        {
            var skill = item.SkillItem;
            var action = item.MatchedActionItem;
            int actionId = action.AssetObject != null ? action.AssetObject.ID : 0;

            bool isIdExact = skill.Id > 0 && actionId > 0 && skill.Id == actionId;
            bool isNameExact = SkillTableDataSourceAdapter.IsSkillMatchAction(skill.Name, action.ActionName, roleName);

            if (isNameExact && isIdExact)
            {
                item.Status = ActionMappingStatus.ExactMatch;
                item.StatusDescription = "完全匹配：名称与数字 ID 均完全对齐";
            }
            else if (!isNameExact && isIdExact)
            {
                item.Status = ActionMappingStatus.FuzzyNameExactId;
                item.StatusDescription = "名称模糊匹配，Id 完全匹配 (建议保留或按需同步名称)";
            }
            else if (isNameExact && !isIdExact)
            {
                item.Status = ActionMappingStatus.ExactNameDiffId;
                item.StatusDescription = actionId == 0
                    ? $"名称完全匹配，Action 资产 ID 未写入 (建议同步 ID 为 {skill.Id})"
                    : $"名称完全匹配，ID 不一致 (配表: {skill.Id} vs 资产: {actionId})";
            }
            else
            {
                item.Status = ActionMappingStatus.FuzzyNameDiffId;
                item.StatusDescription = $"名称模糊匹配，ID 亦不一致 (置信度得分: {item.MatchScore:F0})";
            }
        }

        /// <summary>
        /// 同步单个 Action 资产的内部 ID 为配表 ID
        /// </summary>
        public static bool SyncActionId(ActionConfigAsset actionAsset, int targetId)
        {
            if (actionAsset == null || targetId <= 0) return false;
            if (actionAsset.ID == targetId) return true;

            Undo.RecordObject(actionAsset, "同步 Action ID");
            actionAsset.ID = targetId;
            EditorUtility.SetDirty(actionAsset);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// 同步单个 Action 资产的资产文件名称为配表推导名称
        /// </summary>
        public static bool SyncActionName(ActionAssetIndexItem actionItem, string targetAssetName)
        {
            if (actionItem?.AssetObject == null || string.IsNullOrEmpty(targetAssetName)) return false;
            if (string.Equals(actionItem.ActionName, targetAssetName, StringComparison.OrdinalIgnoreCase)) return true;

            string assetPath = actionItem.AssetPath;
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return false;

            // 1. 同步内部 Name 字段
            Undo.RecordObject(actionItem.AssetObject, "同步 Action 内部名称");
            actionItem.AssetObject.Name = targetAssetName;
            EditorUtility.SetDirty(actionItem.AssetObject);

            // 2. 重命名磁盘 .asset 文件
            string error = AssetDatabase.RenameAsset(assetPath, targetAssetName);
            if (string.IsNullOrEmpty(error))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }
            else
            {
                Debug.LogError($"[ActionMapping] 重命名 Action 资产失败: {error}");
                return false;
            }
        }

        /// <summary>
        /// 批量同步所有需要同步 ID 的项
        /// </summary>
        public static int BatchSyncAllIds(IEnumerable<ActionMappingDiagnosticItem> items)
        {
            if (items == null) return 0;

            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in items)
                {
                    if (item.CanSyncId && item.ActionAsset != null)
                    {
                        item.ActionAsset.ID = item.SkillId;
                        EditorUtility.SetDirty(item.ActionAsset);
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return count;
        }

        /// <summary>
        /// 提取核心动作部分（去除角色前缀、_Action_、Attack_ 等干扰层）
        /// </summary>
        private static string ExtractCoreActionPart(string name, string roleName)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string s = name;

            if (!string.IsNullOrEmpty(roleName) && s.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(roleName.Length + 1);
            }

            if (s.StartsWith("Action_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(7);
            }

            return s;
        }

        /// <summary>
        /// 提取尾部数字编号特征（如 01_01, 01_02, 02_345 等）
        /// </summary>
        private static string ExtractNumberSuffix(string part)
        {
            if (string.IsNullOrEmpty(part)) return string.Empty;

            var tokens = part.Split('_');
            var numberTokens = new List<string>();
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out _))
                {
                    numberTokens.Add(t);
                }
            }

            return numberTokens.Count > 0 ? string.Join("_", numberTokens) : string.Empty;
        }

        private static string GetShortRoleName(SharedWorkspaceDefinition workspace)
        {
            if (workspace == null) return string.Empty;
            if (!string.IsNullOrEmpty(workspace.FolderName))
            {
                return workspace.FolderName.Replace('\\', '/').Split('/').LastOrDefault() ?? workspace.DisplayName;
            }
            return workspace.DisplayName;
        }

        private struct ScoredPair
        {
            public int SkillIndex;
            public int ActionIndex;
            public float Score;

            public ScoredPair(int skillIdx, int actionIdx, float score)
            {
                SkillIndex = skillIdx;
                ActionIndex = actionIdx;
                Score = score;
            }
        }
    }
}
