using System;
using System.Collections.Generic;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 生效不可变配置快照
    /// 经过四级优先级（任务临时 > 角色覆盖 > 工作区配置 > 系统默认值）解析后的最终结果，
    /// 供所有资产扫描器、生成器、校验器与批处理执行器单向只读消费。
    /// </summary>
    public class EffectiveWorkbenchConfig
    {
        public string WorkspaceId { get; }
        public string Category { get; }
        public string RoleName { get; }

        public string ActionConfigDirectory { get; }
        public ConfigSource ActionConfigDirectorySource { get; }

        public string TimelineJsonDirectory { get; }
        public ConfigSource TimelineJsonDirectorySource { get; }

        public string TimelineAssetDirectory { get; }
        public ConfigSource TimelineAssetDirectorySource { get; }

        public string NamingTemplate { get; }
        public ConfigSource NamingTemplateSource { get; }

        public ConflictPolicy ConflictPolicy { get; }
        public ConfigSource ConflictPolicySource { get; }

        public string LubanCharacterId { get; }
        public ConfigSource LubanCharacterIdSource { get; }

        public string SkillTablePath { get; }
        public ConfigSource SkillTablePathSource { get; }

        public IReadOnlyList<string> CustomTags { get; }

        public EffectiveWorkbenchConfig(
            string workspaceId,
            string category,
            string roleName,
            string actionConfigDirectory,
            ConfigSource actionConfigDirectorySource,
            string timelineJsonDirectory,
            ConfigSource timelineJsonDirectorySource,
            string timelineAssetDirectory,
            ConfigSource timelineAssetDirectorySource,
            string namingTemplate,
            ConfigSource namingTemplateSource,
            ConflictPolicy conflictPolicy,
            ConfigSource conflictPolicySource,
            string lubanCharacterId,
            ConfigSource lubanCharacterIdSource,
            IEnumerable<string> customTags,
            string skillTablePath = "Assets/Configs/zzz_tbskill.json",
            ConfigSource skillTablePathSource = ConfigSource.SystemDefault)
        {
            WorkspaceId = workspaceId ?? string.Empty;
            Category = category ?? string.Empty;
            RoleName = roleName ?? string.Empty;

            ActionConfigDirectory = actionConfigDirectory ?? string.Empty;
            ActionConfigDirectorySource = actionConfigDirectorySource;

            TimelineJsonDirectory = timelineJsonDirectory ?? string.Empty;
            TimelineJsonDirectorySource = timelineJsonDirectorySource;

            TimelineAssetDirectory = timelineAssetDirectory ?? string.Empty;
            TimelineAssetDirectorySource = timelineAssetDirectorySource;

            NamingTemplate = namingTemplate ?? string.Empty;
            NamingTemplateSource = namingTemplateSource;

            ConflictPolicy = conflictPolicy;
            ConflictPolicySource = conflictPolicySource;

            LubanCharacterId = lubanCharacterId ?? string.Empty;
            LubanCharacterIdSource = lubanCharacterIdSource;

            SkillTablePath = !string.IsNullOrEmpty(skillTablePath) ? skillTablePath : "Assets/Configs/zzz_tbskill.json";
            SkillTablePathSource = skillTablePathSource;

            CustomTags = customTags != null ? new List<string>(customTags).AsReadOnly() : Array.Empty<string>();
        }

        public override string ToString()
        {
            return $"[EffectiveWorkbenchConfig: {WorkspaceId}] Role={RoleName}, ActionDir={ActionConfigDirectory} ({ActionConfigDirectorySource}), TimelineJson={TimelineJsonDirectory} ({TimelineJsonDirectorySource}), ConflictPolicy={ConflictPolicy} ({ConflictPolicySource})";
        }
    }
}
