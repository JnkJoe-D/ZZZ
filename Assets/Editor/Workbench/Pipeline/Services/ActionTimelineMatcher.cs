using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.GamePlay;

namespace Game.Editor.Workbench
{
    public enum ActionTimelineMatchStatus
    {
        FullyMatched = 0,
        MissingJson = 1,
        MissingSo = 2,
        MissingBoth = 3
    }

    public class ActionTimelineMatchResult
    {
        public ActionAssetIndexItem ActionItem { get; }
        public ActionTimelineMatchStatus Status { get; }
        public TimelineAssetIndexItem MatchedJsonItem { get; }
        public TimelineAssetIndexItem MatchedSoItem { get; }
        public string ExpectedJsonPath { get; }
        public string ExpectedSoPath { get; }

        public bool CanAutoRepair =>
            (Status != ActionTimelineMatchStatus.FullyMatched) &&
            (MatchedJsonItem != null || MatchedSoItem != null);

        public ActionTimelineMatchResult(
            ActionAssetIndexItem actionItem,
            ActionTimelineMatchStatus status,
            TimelineAssetIndexItem matchedJsonItem,
            TimelineAssetIndexItem matchedSoItem,
            string expectedJsonPath,
            string expectedSoPath)
        {
            ActionItem = actionItem;
            Status = status;
            MatchedJsonItem = matchedJsonItem;
            MatchedSoItem = matchedSoItem;
            ExpectedJsonPath = expectedJsonPath ?? string.Empty;
            ExpectedSoPath = expectedSoPath ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{ActionItem?.ActionName}] Status={Status}, JSON={(MatchedJsonItem != null ? "Found" : "Missing")}, SO={(MatchedSoItem != null ? "Found" : "Missing")}";
        }
    }

    /// <summary>
    /// 全流程工作台 - 动作与时间轴对齐匹配与关联诊断器
    /// 统一基于 EffectiveWorkbenchConfig 的路径与命名规则，
    /// 诊断 ActionConfigAsset 与对应 Timeline JSON/SO 的关联对齐状态，并提供孤立时间轴排查与一键对齐修复。
    /// </summary>
    public static class ActionTimelineMatcher
    {
        /// <summary>
        /// 诊断指定 Action 资产与时间轴的对齐关联状态
        /// </summary>
        public static ActionTimelineMatchResult DiagnoseMatch(
            ActionAssetIndexItem actionItem,
            WorkbenchCharacterContext context)
        {
            if (actionItem == null) throw new ArgumentNullException(nameof(actionItem));
            if (context == null) throw new ArgumentNullException(nameof(context));

            string actionName = actionItem.ActionName;
            string roleName = context.EffectiveConfig.RoleName;

            // 1. 推导预期的 Timeline 文件名候选
            var candidateNames = GenerateCandidateTimelineNames(actionName, roleName);

            // 2. 检索当前 Context 中匹配的 JSON 与 SO
            TimelineAssetIndexItem matchedJson = null;
            TimelineAssetIndexItem matchedSo = null;

            foreach (var name in candidateNames)
            {
                if (matchedJson == null) matchedJson = context.FindTimeline(name, isJson: true);
                if (matchedSo == null) matchedSo = context.FindTimeline(name, isJson: false);
                if (matchedJson != null && matchedSo != null) break;
            }

            // 3. 计算预期的绝对路径
            string expectedJsonPath = Path.Combine(context.EffectiveConfig.TimelineJsonDirectory, $"{candidateNames[0]}.json").Replace('\\', '/');
            string expectedSoPath = Path.Combine(context.EffectiveConfig.TimelineAssetDirectory, $"{candidateNames[0]}.asset").Replace('\\', '/');

            // 4. 判断状态
            bool hasJson = actionItem.HasTimelineJson || matchedJson != null;
            bool hasSo = actionItem.HasTimelineSo || matchedSo != null;

            ActionTimelineMatchStatus status;
            if (hasJson && hasSo) status = ActionTimelineMatchStatus.FullyMatched;
            else if (!hasJson && !hasSo) status = ActionTimelineMatchStatus.MissingBoth;
            else if (!hasJson) status = ActionTimelineMatchStatus.MissingJson;
            else status = ActionTimelineMatchStatus.MissingSo;

            return new ActionTimelineMatchResult(
                actionItem,
                status,
                matchedJson,
                matchedSo,
                expectedJsonPath,
                expectedSoPath
            );
        }

        /// <summary>
        /// 排查当前角色目录下未被任何 Action 引用的孤立时间轴
        /// </summary>
        public static List<TimelineAssetIndexItem> FindOrphanTimelines(WorkbenchCharacterContext context)
        {
            var orphans = new List<TimelineAssetIndexItem>();
            if (context == null) return orphans;

            var referencedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in context.Actions)
            {
                if (!string.IsNullOrEmpty(action.TimelineJsonPath)) referencedPaths.Add(action.TimelineJsonPath);
                if (!string.IsNullOrEmpty(action.TimelineSoPath)) referencedPaths.Add(action.TimelineSoPath);
            }

            foreach (var timeline in context.Timelines)
            {
                if (!referencedPaths.Contains(timeline.AssetPath))
                {
                    orphans.Add(timeline);
                }
            }

            return orphans;
        }

        /// <summary>
        /// 为动作资产自动对齐绑定时间轴并保存
        /// </summary>
        public static bool AutoBindTimeline(
            ActionConfigAsset actionAsset,
            TimelineAssetIndexItem jsonItem,
            TimelineAssetIndexItem soItem)
        {
            if (actionAsset == null) return false;

            bool isDirty = false;

            if (jsonItem != null && actionAsset.TimelineAsset == null)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonItem.AssetPath);
                if (textAsset != null)
                {
                    actionAsset.TimelineAsset = textAsset;
                    isDirty = true;
                }
            }

            if (soItem != null && actionAsset.actionTimelineSO == null)
            {
                var soAsset = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTimeline>(soItem.AssetPath);
                if (soAsset != null)
                {
                    actionAsset.actionTimelineSO = soAsset;
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                EditorUtility.SetDirty(actionAsset);
                AssetDatabase.SaveAssets();
            }

            return isDirty;
        }

        private static List<string> GenerateCandidateTimelineNames(string actionName, string roleName)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(actionName)) return list;

            // 1. 原始 actionName，如 "Ellen_Timeline_Attack_01" 或 "Attack_01"
            list.Add(actionName);

            // 2. 带 Timeline 标记变体，如 "Ellen_Timeline_Attack_01"
            if (!actionName.Contains("Timeline"))
            {
                list.Add($"{roleName}_Timeline_{actionName}");
                list.Add($"{actionName}_Timeline");
            }

            // 3. 去除前缀变体，如 "Attack_01" 从 "Ellen_Attack_01" 剥离
            if (!string.IsNullOrEmpty(roleName) && actionName.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                string stripped = actionName.Substring(roleName.Length + 1);
                list.Add(stripped);
                list.Add($"{roleName}_Timeline_{stripped}");
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
