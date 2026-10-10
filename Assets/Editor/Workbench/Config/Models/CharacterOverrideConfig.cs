using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 角色专属差异化覆盖配置模型 (持久化于 JSON)
    /// 仅记录相对工作区配置的差异值，未显式覆盖的项均继承上层工作区配置。
    /// </summary>
    [Serializable]
    public class CharacterOverrideConfig
    {
        [SerializeField] private string workspaceId = string.Empty;
        [SerializeField] private string lubanCharacterId = string.Empty;
        [SerializeField] private string actionSubFolderOverride = string.Empty;
        [SerializeField] private string timelineSubFolderOverride = string.Empty;
        [SerializeField] private string namingRuleOverride = string.Empty;
        [SerializeField] private bool hasConflictPolicyOverride = false;
        [SerializeField] private ConflictPolicy conflictPolicyOverride = ConflictPolicy.Skip;
        [SerializeField] private string skillTablePathOverride = string.Empty;
        [SerializeField] private List<string> customTags = new List<string>();

        /// <summary>
        /// 关联的公共共享角色工作区唯一标识（如 "Role_Ellen"）
        /// </summary>
        public string WorkspaceId
        {
            get => workspaceId;
            set => workspaceId = value;
        }

        /// <summary>
        /// 绑定的 Luban 角色 ID（如 "Ellen" 或 "1003"）
        /// </summary>
        public string LubanCharacterId
        {
            get => lubanCharacterId;
            set => lubanCharacterId = value;
        }

        /// <summary>
        /// Action 子目录覆盖（相对 ActionConfigRoot，如 "Role/Ellen/Action"；若为空则继承默认规则）
        /// </summary>
        public string ActionSubFolderOverride
        {
            get => actionSubFolderOverride;
            set => actionSubFolderOverride = value;
        }

        /// <summary>
        /// Timeline 子目录覆盖（若为空则继承默认规则）
        /// </summary>
        public string TimelineSubFolderOverride
        {
            get => timelineSubFolderOverride;
            set => timelineSubFolderOverride = value;
        }

        /// <summary>
        /// 动作命名规则覆盖（若为空则继承工作区模板）
        /// </summary>
        public string NamingRuleOverride
        {
            get => namingRuleOverride;
            set => namingRuleOverride = value;
        }

        /// <summary>
        /// 是否覆盖冲突策略
        /// </summary>
        public bool HasConflictPolicyOverride
        {
            get => hasConflictPolicyOverride;
            set => hasConflictPolicyOverride = value;
        }

        /// <summary>
        /// 覆盖的冲突策略（仅当 HasConflictPolicyOverride 为 true 时生效）
        /// </summary>
        public ConflictPolicy ConflictPolicyOverride
        {
            get => conflictPolicyOverride;
            set => conflictPolicyOverride = value;
        }

        /// <summary>
        /// 技能/动作配表数据源路径覆盖（若为空则继承工作区分类默认路径）
        /// </summary>
        public string SkillTablePathOverride
        {
            get => skillTablePathOverride;
            set => skillTablePathOverride = value;
        }

        /// <summary>
        /// 角色业务标签
        /// </summary>
        public List<string> CustomTags
        {
            get => customTags ?? (customTags = new List<string>());
            set => customTags = value;
        }

        /// <summary>
        /// 检查当前角色是否有任何显式覆盖
        /// </summary>
        public bool HasAnyOverride =>
            !string.IsNullOrEmpty(actionSubFolderOverride) ||
            !string.IsNullOrEmpty(timelineSubFolderOverride) ||
            !string.IsNullOrEmpty(namingRuleOverride) ||
            !string.IsNullOrEmpty(skillTablePathOverride) ||
            hasConflictPolicyOverride ||
            !string.IsNullOrEmpty(lubanCharacterId);

        /// <summary>
        /// 规范化配置字段（统一斜杠等）
        /// </summary>
        public void Normalize()
        {
            if (!string.IsNullOrEmpty(workspaceId))
            {
                workspaceId = workspaceId.Trim();
            }
            if (!string.IsNullOrEmpty(lubanCharacterId))
            {
                lubanCharacterId = lubanCharacterId.Trim();
            }
            if (!string.IsNullOrEmpty(actionSubFolderOverride))
            {
                actionSubFolderOverride = actionSubFolderOverride.Trim().Replace('\\', '/').Trim('/');
            }
            if (!string.IsNullOrEmpty(timelineSubFolderOverride))
            {
                timelineSubFolderOverride = timelineSubFolderOverride.Trim().Replace('\\', '/').Trim('/');
            }
            if (!string.IsNullOrEmpty(namingRuleOverride))
            {
                namingRuleOverride = namingRuleOverride.Trim();
            }
            if (!string.IsNullOrEmpty(skillTablePathOverride))
            {
                skillTablePathOverride = skillTablePathOverride.Trim().Replace('\\', '/');
            }
        }

        /// <summary>
        /// 深度克隆副本
        /// </summary>
        public CharacterOverrideConfig Clone()
        {
            return new CharacterOverrideConfig
            {
                workspaceId = this.workspaceId,
                lubanCharacterId = this.lubanCharacterId,
                actionSubFolderOverride = this.actionSubFolderOverride,
                timelineSubFolderOverride = this.timelineSubFolderOverride,
                namingRuleOverride = this.namingRuleOverride,
                hasConflictPolicyOverride = this.hasConflictPolicyOverride,
                conflictPolicyOverride = this.conflictPolicyOverride,
                skillTablePathOverride = this.skillTablePathOverride,
                customTags = new List<string>(this.CustomTags)
            };
        }
    }
}
