using System;
using System.Collections.Generic;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    [Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Target { get; }
        public string Message { get; }
        public string Suggestion { get; }

        public ValidationIssue(ValidationSeverity severity, string target, string message, string suggestion = null)
        {
            Severity = severity;
            Target = target ?? string.Empty;
            Message = message ?? string.Empty;
            Suggestion = suggestion ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{Severity}] ({Target}) {Message}" + (!string.IsNullOrEmpty(Suggestion) ? $" -> 建议: {Suggestion}" : "");
        }
    }

    public class WorkbenchValidationReport
    {
        private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();

        public IReadOnlyList<ValidationIssue> Issues => _issues;
        public bool HasErrors { get; private set; }
        public bool HasWarnings { get; private set; }

        public void AddIssue(ValidationSeverity severity, string target, string message, string suggestion = null)
        {
            var issue = new ValidationIssue(severity, target, message, suggestion);
            _issues.Add(issue);

            if (severity == ValidationSeverity.Error) HasErrors = true;
            else if (severity == ValidationSeverity.Warning) HasWarnings = true;
        }
    }

    /// <summary>
    /// 全流程工作台 - 多维度配置校验器
    /// 负责对工作区全局配置、角色覆盖配置及生效配置快照执行结构、安全与引用校验。
    /// </summary>
    public static class WorkbenchConfigValidator
    {
        /// <summary>
        /// 校验工作区全局配置
        /// </summary>
        public static WorkbenchValidationReport ValidateWorkspaceConfig(WorkbenchWorkspaceConfig config)
        {
            var report = new WorkbenchValidationReport();
            if (config == null)
            {
                report.AddIssue(ValidationSeverity.Error, "WorkspaceConfig", "工作区配置对象为空！");
                return report;
            }

            // 1. ActionConfigRoot
            if (string.IsNullOrEmpty(config.ActionConfigRoot))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.ActionConfigRoot), "ActionConfigRoot 根目录不能为空！");
            }
            else
            {
                if (WorkbenchPathResolver.IsPathTraversing(config.ActionConfigRoot))
                {
                    report.AddIssue(ValidationSeverity.Error, nameof(config.ActionConfigRoot), "ActionConfigRoot 存在非法的路径穿越 (../)！");
                }
                if (!config.ActionConfigRoot.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    report.AddIssue(ValidationSeverity.Warning, nameof(config.ActionConfigRoot), "ActionConfigRoot 应位于 Unity 项目 Assets/ 目录下。");
                }
            }

            // 2. TimelineJsonRoot
            if (string.IsNullOrEmpty(config.TimelineJsonRoot))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.TimelineJsonRoot), "TimelineJsonRoot 根目录不能为空！");
            }
            else if (WorkbenchPathResolver.IsPathTraversing(config.TimelineJsonRoot))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.TimelineJsonRoot), "TimelineJsonRoot 存在非法的路径穿越 (../)！");
            }

            // 3. TimelineAssetRoot
            if (string.IsNullOrEmpty(config.TimelineAssetRoot))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.TimelineAssetRoot), "TimelineAssetRoot 根目录不能为空！");
            }
            else if (WorkbenchPathResolver.IsPathTraversing(config.TimelineAssetRoot))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.TimelineAssetRoot), "TimelineAssetRoot 存在非法的路径穿越 (../)！");
            }

            // 4. NamingTemplate
            if (string.IsNullOrEmpty(config.NamingTemplate))
            {
                report.AddIssue(ValidationSeverity.Warning, nameof(config.NamingTemplate), "命名模板为空，建议配置至少包含 {ActionName} 占位符。");
            }
            else if (!config.NamingTemplate.Contains("{ActionName}"))
            {
                report.AddIssue(ValidationSeverity.Warning, nameof(config.NamingTemplate), "命名模板未包含 {ActionName} 占位符，可能导致生成资产名称重复！");
            }

            return report;
        }

        /// <summary>
        /// 校验指定角色的覆盖配置
        /// </summary>
        public static WorkbenchValidationReport ValidateCharacterOverride(
            CharacterOverrideConfig config,
            SharedWorkspaceData sharedData = null)
        {
            var report = new WorkbenchValidationReport();
            if (config == null)
            {
                report.AddIssue(ValidationSeverity.Error, "CharacterOverride", "角色覆盖配置对象为空！");
                return report;
            }

            if (string.IsNullOrEmpty(config.WorkspaceId))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(config.WorkspaceId), "角色覆盖未指定有效的 WorkspaceId！");
                return report;
            }

            // 1. 引用一致性：检查是否对应有效的公共共享角色
            sharedData = sharedData ?? SharedWorkspaceFileIO.Load();
            if (sharedData != null && sharedData.FindById(config.WorkspaceId) == null)
            {
                report.AddIssue(
                    ValidationSeverity.Warning,
                    nameof(config.WorkspaceId),
                    $"共享角色列表已不存在角色 [{config.WorkspaceId}]，该覆盖配置可能已孤立。",
                    "建议在工作台中清理或删除此孤立角色的覆盖文件。");
            }

            // 2. Luban 角色绑定校验
            if (!string.IsNullOrEmpty(config.LubanCharacterId))
            {
                if (!LubanCharacterSourceAdapter.IsCharacterValid(config.LubanCharacterId))
                {
                    report.AddIssue(
                        ValidationSeverity.Warning,
                        nameof(config.LubanCharacterId),
                        $"绑定的 Luban 角色标识 [{config.LubanCharacterId}] 在当前 Luban 表中无法匹配！",
                        "请检查 LubanCharacterId 是否拼写正确或配表是否已重新导出。");
                }
            }

            // 3. 覆盖路径安全校验
            if (!string.IsNullOrEmpty(config.ActionSubFolderOverride))
            {
                if (WorkbenchPathResolver.IsPathTraversing(config.ActionSubFolderOverride))
                {
                    report.AddIssue(ValidationSeverity.Error, nameof(config.ActionSubFolderOverride), "Action 子目录覆盖存在非法的路径穿越 (../)！");
                }
            }

            if (!string.IsNullOrEmpty(config.TimelineSubFolderOverride))
            {
                if (WorkbenchPathResolver.IsPathTraversing(config.TimelineSubFolderOverride))
                {
                    report.AddIssue(ValidationSeverity.Error, nameof(config.TimelineSubFolderOverride), "Timeline 子目录覆盖存在非法的路径穿越 (../)！");
                }
            }

            return report;
        }

        /// <summary>
        /// 校验解析后的生效配置快照
        /// </summary>
        public static WorkbenchValidationReport ValidateEffectiveConfig(EffectiveWorkbenchConfig effectiveConfig)
        {
            var report = new WorkbenchValidationReport();
            if (effectiveConfig == null)
            {
                report.AddIssue(ValidationSeverity.Error, "EffectiveConfig", "生效配置快照为空！");
                return report;
            }

            if (string.IsNullOrEmpty(effectiveConfig.ActionConfigDirectory))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(effectiveConfig.ActionConfigDirectory), "解析后的 Action 资产目录为空！");
            }
            else if (WorkbenchPathResolver.IsPathTraversing(effectiveConfig.ActionConfigDirectory))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(effectiveConfig.ActionConfigDirectory), "解析后的 Action 资产目录存在越界穿越！");
            }

            if (string.IsNullOrEmpty(effectiveConfig.TimelineJsonDirectory))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(effectiveConfig.TimelineJsonDirectory), "解析后的 Timeline JSON 目录为空！");
            }

            if (string.IsNullOrEmpty(effectiveConfig.TimelineAssetDirectory))
            {
                report.AddIssue(ValidationSeverity.Error, nameof(effectiveConfig.TimelineAssetDirectory), "解析后的 Timeline Asset 目录为空！");
            }

            return report;
        }
    }
}
