using System;
using UnityEngine;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 工作区级全局通用配置模型 (持久化于 JSON)
    /// </summary>
    [Serializable]
    public class WorkbenchWorkspaceConfig
    {
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private string actionConfigRoot = WorkbenchPathResolver.DefaultActionConfigRoot;
        [SerializeField] private string timelineJsonRoot = WorkbenchPathResolver.DEFAULT_TIMELINE_JSON_ROOT;
        [SerializeField] private string timelineAssetRoot = WorkbenchPathResolver.DEFAULT_TIMELINE_SO_ROOT;
        [SerializeField] private string namingTemplate = "{RoleName}_Action_{ActionName}";
        [SerializeField] private ConflictPolicy defaultConflictPolicy = ConflictPolicy.Skip;

        // 各分类动作配表数据源路径（支持分表）
        [SerializeField] private string roleSkillTablePath = "Assets/Configs/zzz_tbskill.json";
        [SerializeField] private string monsterSkillTablePath = "Assets/Configs/zzz_tbskill.json";
        [SerializeField] private string commonSkillTablePath = "Assets/Configs/zzz_tbskill.json";

        /// <summary>
        /// Schema 版本号
        /// </summary>
        public int SchemaVersion
        {
            get => schemaVersion;
            set => schemaVersion = value;
        }

        /// <summary>
        /// ActionConfig 资产根目录（Unity 项目相对路径）
        /// </summary>
        public string ActionConfigRoot
        {
            get => actionConfigRoot;
            set => actionConfigRoot = value;
        }

        /// <summary>
        /// Timeline JSON 根目录
        /// </summary>
        public string TimelineJsonRoot
        {
            get => timelineJsonRoot;
            set => timelineJsonRoot = value;
        }

        /// <summary>
        /// Timeline SO 资产根目录
        /// </summary>
        public string TimelineAssetRoot
        {
            get => timelineAssetRoot;
            set => timelineAssetRoot = value;
        }

        /// <summary>
        /// 动作命名规则模板，支持占位符：{Category}, {RoleName}, {ActionName}
        /// </summary>
        public string NamingTemplate
        {
            get => namingTemplate;
            set => namingTemplate = value;
        }

        /// <summary>
        /// 默认冲突处理策略
        /// </summary>
        public ConflictPolicy DefaultConflictPolicy
        {
            get => defaultConflictPolicy;
            set => defaultConflictPolicy = value;
        }

        /// <summary>
        /// Role 分类默认技能配表数据源路径
        /// </summary>
        public string RoleSkillTablePath
        {
            get => roleSkillTablePath;
            set => roleSkillTablePath = value;
        }

        /// <summary>
        /// Monster 分类默认技能配表数据源路径
        /// </summary>
        public string MonsterSkillTablePath
        {
            get => monsterSkillTablePath;
            set => monsterSkillTablePath = value;
        }

        /// <summary>
        /// Common 分类默认技能配表数据源路径
        /// </summary>
        public string CommonSkillTablePath
        {
            get => commonSkillTablePath;
            set => commonSkillTablePath = value;
        }

        /// <summary>
        /// 获取指定分类对应的默认技能配表路径
        /// </summary>
        public string GetDefaultSkillTablePath(string category)
        {
            if (string.Equals(category, SharedWorkspaceDefinition.CategoryMonster, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(monsterSkillTablePath) ? monsterSkillTablePath : "Assets/Configs/zzz_tbskill.json";
            }
            if (string.Equals(category, SharedWorkspaceDefinition.CategoryCommon, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(commonSkillTablePath) ? commonSkillTablePath : "Assets/Configs/zzz_tbskill.json";
            }
            return !string.IsNullOrEmpty(roleSkillTablePath) ? roleSkillTablePath : "Assets/Configs/zzz_tbskill.json";
        }

        /// <summary>
        /// 创建默认工作区配置
        /// </summary>
        public static WorkbenchWorkspaceConfig CreateDefault()
        {
            return new WorkbenchWorkspaceConfig();
        }

        /// <summary>
        /// 规范化路径配置（统一斜杠并清理首尾冗余空白）
        /// </summary>
        public void Normalize()
        {
            if (!string.IsNullOrEmpty(actionConfigRoot))
            {
                actionConfigRoot = actionConfigRoot.Trim().Replace('\\', '/').TrimEnd('/');
            }
            if (!string.IsNullOrEmpty(timelineJsonRoot))
            {
                timelineJsonRoot = timelineJsonRoot.Trim().Replace('\\', '/').TrimEnd('/');
            }
            if (!string.IsNullOrEmpty(timelineAssetRoot))
            {
                timelineAssetRoot = timelineAssetRoot.Trim().Replace('\\', '/').TrimEnd('/');
            }
            if (!string.IsNullOrEmpty(roleSkillTablePath))
            {
                roleSkillTablePath = roleSkillTablePath.Trim().Replace('\\', '/');
            }
            if (!string.IsNullOrEmpty(monsterSkillTablePath))
            {
                monsterSkillTablePath = monsterSkillTablePath.Trim().Replace('\\', '/');
            }
            if (!string.IsNullOrEmpty(commonSkillTablePath))
            {
                commonSkillTablePath = commonSkillTablePath.Trim().Replace('\\', '/');
            }
            if (string.IsNullOrEmpty(namingTemplate))
            {
                namingTemplate = "{Category}_{RoleName}_{ActionName}";
            }
        }

        /// <summary>
        /// 深度克隆副本
        /// </summary>
        public WorkbenchWorkspaceConfig Clone()
        {
            return new WorkbenchWorkspaceConfig
            {
                schemaVersion = this.schemaVersion,
                actionConfigRoot = this.actionConfigRoot,
                timelineJsonRoot = this.timelineJsonRoot,
                timelineAssetRoot = this.timelineAssetRoot,
                namingTemplate = this.namingTemplate,
                defaultConflictPolicy = this.defaultConflictPolicy,
                roleSkillTablePath = this.roleSkillTablePath,
                monsterSkillTablePath = this.monsterSkillTablePath,
                commonSkillTablePath = this.commonSkillTablePath
            };
        }
    }
}
