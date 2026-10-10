using System;
using System.IO;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 四级生效配置解析引擎 (EffectiveConfigResolver)
    /// 严格遵循覆盖优先级：
    /// 任务临时参数 (Task Overrides) > 角色差异化覆盖 (Character Overrides) > 工作区全局配置 (Workspace Config) > 系统默认值 (System Defaults)
    /// 统一生成只读且不可变的生效配置快照 EffectiveWorkbenchConfig，并附带每一项的来源溯源标记。
    /// </summary>
    public static class EffectiveConfigResolver
    {
        public const string SystemDefaultNamingTemplate = "{RoleName}_Action_{ActionName}";
        public const ConflictPolicy SystemDefaultConflictPolicy = ConflictPolicy.Skip;

        /// <summary>
        /// 基于当前共享工作区定义自动加载配置并解析
        /// </summary>
        public static EffectiveWorkbenchConfig Resolve(
            SharedWorkspaceDefinition ws,
            TaskOverrideParams taskParams = null)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            var wsConfig = WorkbenchConfigStorage.LoadWorkspaceConfig();
            var charOverride = WorkbenchConfigStorage.LoadCharacterOverride(ws.Id);

            return Resolve(ws, wsConfig, charOverride, taskParams);
        }

        /// <summary>
        /// 完整解析生效配置（支持显式注入各层级配置，便于离线测试与沙盒预览）
        /// </summary>
        public static EffectiveWorkbenchConfig Resolve(
            SharedWorkspaceDefinition ws,
            WorkbenchWorkspaceConfig wsConfig,
            CharacterOverrideConfig charOverride,
            TaskOverrideParams taskParams = null)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            wsConfig = wsConfig ?? WorkbenchWorkspaceConfig.CreateDefault();
            wsConfig.Normalize();

            if (charOverride != null)
            {
                charOverride.Normalize();
            }

            string category = ws.Category ?? "Role";
            string roleName = GetRoleNameFromFolder(ws.FolderName);

            // 1. 解析 ActionConfigDirectory
            string actionDir;
            ConfigSource actionSource;
            if (taskParams != null && !string.IsNullOrEmpty(taskParams.ActionDirectoryOverride))
            {
                actionDir = taskParams.ActionDirectoryOverride.Trim().Replace('\\', '/');
                actionSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && !string.IsNullOrEmpty(charOverride.ActionSubFolderOverride))
            {
                string baseRoot = !string.IsNullOrEmpty(wsConfig.ActionConfigRoot)
                    ? wsConfig.ActionConfigRoot
                    : WorkbenchPathResolver.DefaultActionConfigRoot;
                actionDir = CombinePath(baseRoot, charOverride.ActionSubFolderOverride);
                actionSource = ConfigSource.CharacterOverride;
            }
            else
            {
                string baseRoot = !string.IsNullOrEmpty(wsConfig.ActionConfigRoot)
                    ? wsConfig.ActionConfigRoot
                    : WorkbenchPathResolver.DefaultActionConfigRoot;

                // 优先标准目录：baseRoot / FolderName / Action
                string subFolder = !string.IsNullOrEmpty(ws.FolderName)
                    ? $"{ws.FolderName.Trim('/')}/Action"
                    : $"{roleName}/Action";

                actionDir = CombinePath(baseRoot, subFolder);
                actionSource = !string.IsNullOrEmpty(wsConfig.ActionConfigRoot)
                    ? ConfigSource.WorkspaceInherited
                    : ConfigSource.SystemDefault;
            }

            // 2. 解析 TimelineJsonDirectory
            string timelineJsonDir;
            ConfigSource timelineJsonSource;
            if (taskParams != null && !string.IsNullOrEmpty(taskParams.TimelineJsonDirectoryOverride))
            {
                timelineJsonDir = taskParams.TimelineJsonDirectoryOverride.Trim().Replace('\\', '/');
                timelineJsonSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && !string.IsNullOrEmpty(charOverride.TimelineSubFolderOverride))
            {
                string baseJsonRoot = !string.IsNullOrEmpty(wsConfig.TimelineJsonRoot)
                    ? wsConfig.TimelineJsonRoot
                    : WorkbenchPathResolver.DEFAULT_TIMELINE_JSON_ROOT;
                timelineJsonDir = CombinePath(baseJsonRoot, charOverride.TimelineSubFolderOverride);
                timelineJsonSource = ConfigSource.CharacterOverride;
            }
            else
            {
                string baseJsonRoot = !string.IsNullOrEmpty(wsConfig.TimelineJsonRoot)
                    ? wsConfig.TimelineJsonRoot
                    : WorkbenchPathResolver.DEFAULT_TIMELINE_JSON_ROOT;
                timelineJsonDir = ResolveExistingOrFallbackTimelineDir(baseJsonRoot, ws.FolderName);
                timelineJsonSource = !string.IsNullOrEmpty(wsConfig.TimelineJsonRoot)
                    ? ConfigSource.WorkspaceInherited
                    : ConfigSource.SystemDefault;
            }

            // 3. 解析 TimelineAssetDirectory
            string timelineAssetDir;
            ConfigSource timelineAssetSource;
            if (taskParams != null && !string.IsNullOrEmpty(taskParams.TimelineAssetDirectoryOverride))
            {
                timelineAssetDir = taskParams.TimelineAssetDirectoryOverride.Trim().Replace('\\', '/');
                timelineAssetSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && !string.IsNullOrEmpty(charOverride.TimelineSubFolderOverride))
            {
                string baseSoRoot = !string.IsNullOrEmpty(wsConfig.TimelineAssetRoot)
                    ? wsConfig.TimelineAssetRoot
                    : WorkbenchPathResolver.DEFAULT_TIMELINE_SO_ROOT;
                timelineAssetDir = CombinePath(baseSoRoot, charOverride.TimelineSubFolderOverride);
                timelineAssetSource = ConfigSource.CharacterOverride;
            }
            else
            {
                string baseSoRoot = !string.IsNullOrEmpty(wsConfig.TimelineAssetRoot)
                    ? wsConfig.TimelineAssetRoot
                    : WorkbenchPathResolver.DEFAULT_TIMELINE_SO_ROOT;
                timelineAssetDir = ResolveExistingOrFallbackTimelineDir(baseSoRoot, ws.FolderName);
                timelineAssetSource = !string.IsNullOrEmpty(wsConfig.TimelineAssetRoot)
                    ? ConfigSource.WorkspaceInherited
                    : ConfigSource.SystemDefault;
            }

            // 4. 解析 NamingTemplate
            string namingTemplate;
            ConfigSource namingSource;
            if (taskParams != null && !string.IsNullOrEmpty(taskParams.NamingTemplateOverride))
            {
                namingTemplate = taskParams.NamingTemplateOverride.Trim();
                namingSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && !string.IsNullOrEmpty(charOverride.NamingRuleOverride))
            {
                namingTemplate = charOverride.NamingRuleOverride.Trim();
                namingSource = ConfigSource.CharacterOverride;
            }
            else if (!string.IsNullOrEmpty(wsConfig.NamingTemplate))
            {
                namingTemplate = wsConfig.NamingTemplate.Trim();
                namingSource = ConfigSource.WorkspaceInherited;
            }
            else
            {
                namingTemplate = SystemDefaultNamingTemplate;
                namingSource = ConfigSource.SystemDefault;
            }

            // 5. 解析 ConflictPolicy
            ConflictPolicy conflictPolicy;
            ConfigSource conflictSource;
            if (taskParams != null && taskParams.ConflictPolicyOverride.HasValue)
            {
                conflictPolicy = taskParams.ConflictPolicyOverride.Value;
                conflictSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && charOverride.HasConflictPolicyOverride)
            {
                conflictPolicy = charOverride.ConflictPolicyOverride;
                conflictSource = ConfigSource.CharacterOverride;
            }
            else
            {
                conflictPolicy = wsConfig.DefaultConflictPolicy;
                conflictSource = ConfigSource.WorkspaceInherited;
            }

            // 6. 解析 LubanCharacterId
            string lubanCharId;
            ConfigSource lubanSource;
            if (charOverride != null && !string.IsNullOrEmpty(charOverride.LubanCharacterId))
            {
                lubanCharId = charOverride.LubanCharacterId.Trim();
                lubanSource = ConfigSource.CharacterOverride;
            }
            else
            {
                // 智能自动探测：根据角色目录名或 Id 自动匹配 Luban 角色
                if (LubanCharacterSourceAdapter.TryGetCharacterByName(roleName, out var lubanInfo))
                {
                    lubanCharId = lubanInfo.EnumName;
                    lubanSource = ConfigSource.WorkspaceInherited;
                }
                else
                {
                    lubanCharId = string.Empty;
                    lubanSource = ConfigSource.SystemDefault;
                }
            }

            // 7. 解析 SkillTablePath（支持分类分表与角色覆盖）
            string skillTablePath;
            ConfigSource skillTableSource;
            if (taskParams != null && !string.IsNullOrEmpty(taskParams.SkillTablePathOverride))
            {
                skillTablePath = taskParams.SkillTablePathOverride.Trim().Replace('\\', '/');
                skillTableSource = ConfigSource.TaskOverride;
            }
            else if (charOverride != null && !string.IsNullOrEmpty(charOverride.SkillTablePathOverride))
            {
                skillTablePath = charOverride.SkillTablePathOverride.Trim().Replace('\\', '/');
                skillTableSource = ConfigSource.CharacterOverride;
            }
            else
            {
                string catPath = wsConfig.GetDefaultSkillTablePath(ws.Category);
                if (!string.IsNullOrEmpty(catPath))
                {
                    skillTablePath = catPath.Trim().Replace('\\', '/');
                    skillTableSource = ConfigSource.WorkspaceInherited;
                }
                else
                {
                    skillTablePath = "Assets/Configs/zzz_tbskill.json";
                    skillTableSource = ConfigSource.SystemDefault;
                }
            }

            var tags = charOverride != null ? charOverride.CustomTags : null;

            return new EffectiveWorkbenchConfig(
                ws.Id,
                category,
                roleName,
                actionDir,
                actionSource,
                timelineJsonDir,
                timelineJsonSource,
                timelineAssetDir,
                timelineAssetSource,
                namingTemplate,
                namingSource,
                conflictPolicy,
                conflictSource,
                lubanCharId,
                lubanSource,
                tags,
                skillTablePath,
                skillTableSource
            );
        }

        private static string GetRoleNameFromFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return string.Empty;
            string trimmed = folderName.Trim().Replace('\\', '/').Trim('/');
            int lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
        }

        private static string CombinePath(string root, string sub)
        {
            if (string.IsNullOrEmpty(root)) return sub ?? string.Empty;
            if (string.IsNullOrEmpty(sub)) return root;

            string cleanRoot = root.Trim().Replace('\\', '/').TrimEnd('/');
            string cleanSub = sub.Trim().Replace('\\', '/').TrimStart('/');
            return $"{cleanRoot}/{cleanSub}";
        }

        private static string ResolveExistingOrFallbackTimelineDir(string baseRoot, string folderName)
        {
            return CombinePath(baseRoot, folderName);
        }
    }
}
