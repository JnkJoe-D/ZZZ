using System;
using UnityEngine;
using Game.GamePlay;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 数据源与 Action 资产的映射状态
    /// </summary>
    public enum ActionMappingStatus
    {
        /// <summary>完全匹配：名称格式规范匹配 且 Id 完全全等</summary>
        ExactMatch,

        /// <summary>名称格式模糊匹配，Id 完全匹配 (如配表 Ellen_Dash_Start_Front vs 资产 Ellen_Action_Attack_Dash_Start_Front)</summary>
        FuzzyNameExactId,

        /// <summary>名称格式完全匹配，Id 不匹配 (如资产内 ID 尚未写入或配表 ID 变动)</summary>
        ExactNameDiffId,

        /// <summary>名称格式模糊匹配，Id 不匹配</summary>
        FuzzyNameDiffId,

        /// <summary>无匹配资产：工程中不存在对应或相似的 Action 资产</summary>
        NoMatch
    }

    /// <summary>
    /// 配表数据源到实际 Action 资产的诊断映射条目
    /// </summary>
    public class ActionMappingDiagnosticItem
    {
        /// <summary>配表数据源条目</summary>
        public SkillTableItem SkillItem { get; set; }

        /// <summary>匹配到的实际 Action 资产条目（若无匹配则为空）</summary>
        public ActionAssetIndexItem MatchedActionItem { get; set; }

        /// <summary>匹配状态</summary>
        public ActionMappingStatus Status { get; set; } = ActionMappingStatus.NoMatch;

        /// <summary>匹配置信度评分 (Score)</summary>
        public float MatchScore { get; set; } = 0f;

        /// <summary>状态详细说明或差异提示</summary>
        public string StatusDescription { get; set; } = string.Empty;

        // 快捷数据读取
        public int SkillId => SkillItem?.Id ?? 0;
        public string SkillName => SkillItem?.Name ?? string.Empty;

        public int ActionId => MatchedActionItem?.AssetObject != null ? MatchedActionItem.AssetObject.ID : 0;
        public string ActionName => MatchedActionItem?.ActionName ?? string.Empty;
        public string ActionAssetPath => MatchedActionItem?.AssetPath ?? string.Empty;
        public ActionConfigAsset ActionAsset => MatchedActionItem?.AssetObject;

        /// <summary>是否有关联的 Action 资产（包括模糊匹配）</summary>
        public bool HasAssociatedAction => MatchedActionItem != null && MatchedActionItem.AssetObject != null;

        /// <summary>是否需要同步 ID（有关联资产但 ID 不一致）</summary>
        public bool CanSyncId => HasAssociatedAction && SkillId > 0 && ActionId != SkillId;

        /// <summary>是否需要同步名称（有关联资产但名称不完全一致）</summary>
        public bool CanSyncName => HasAssociatedAction && !string.IsNullOrEmpty(SkillName) &&
                                   !string.Equals(SkillName, ActionName, StringComparison.OrdinalIgnoreCase);
    }
}
