using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Game.Editor.Workspace;
using Game.Editor.Workbench;

namespace GameClient.Tests.Editor
{
    /// <summary>
    /// Phase 2: EffectiveConfigResolver 与工作台专属配置体系单元测试
    /// 验证四级优先级覆盖（任务 > 角色 > 工作区 > 默认）、来源溯源、Luban 适配器与多维校验器
    /// </summary>
    public class EffectiveConfigResolverTests
    {
        private SharedWorkspaceDefinition _testWs;
        private WorkbenchWorkspaceConfig _defaultWsConfig;

        [SetUp]
        public void SetUp()
        {
            _testWs = new SharedWorkspaceDefinition(
                "Role_Ellen",
                "Role",
                "艾莲",
                "Role/Ellen",
                0,
                "测试角色定义"
            );

            _defaultWsConfig = new WorkbenchWorkspaceConfig
            {
                ActionConfigRoot = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig",
                TimelineJsonRoot = "Assets/GameClient/TimelineRoot/Json",
                TimelineAssetRoot = "Assets/GameClient/TimelineRoot/Assets",
                NamingTemplate = "{RoleName}_Action_{ActionName}",
                DefaultConflictPolicy = ConflictPolicy.Skip
            };
        }

        [Test]
        public void Test01_DefaultResolution_InheritsFromWorkspace()
        {
            // 当没有角色覆盖和任务临时覆盖时
            var effective = EffectiveConfigResolver.Resolve(_testWs, _defaultWsConfig, null, null);

            Assert.AreEqual("Role_Ellen", effective.WorkspaceId);
            Assert.AreEqual("Role", effective.Category);
            Assert.AreEqual("Ellen", effective.RoleName);

            // Action 目录继承
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action", effective.ActionConfigDirectory);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.ActionConfigDirectorySource);

            // Timeline 目录继承
            Assert.AreEqual("Assets/GameClient/TimelineRoot/Json/Role/Ellen", effective.TimelineJsonDirectory);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.TimelineJsonDirectorySource);

            // 命名规则继承
            Assert.AreEqual("{RoleName}_Action_{ActionName}", effective.NamingTemplate);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.NamingTemplateSource);

            // 冲突策略继承
            Assert.AreEqual(ConflictPolicy.Skip, effective.ConflictPolicy);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.ConflictPolicySource);

            // Luban 智能推导
            Assert.AreEqual("Ellen", effective.LubanCharacterId);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.LubanCharacterIdSource);
        }

        [Test]
        public void Test02_CharacterOverride_TakesPrecedenceOverWorkspace()
        {
            var charOverride = new CharacterOverrideConfig
            {
                WorkspaceId = "Role_Ellen",
                ActionSubFolderOverride = "Custom/Ellen/SpecialAction",
                NamingRuleOverride = "Special_{ActionName}",
                HasConflictPolicyOverride = true,
                ConflictPolicyOverride = ConflictPolicy.Overwrite,
                LubanCharacterId = "Ellen"
            };

            var effective = EffectiveConfigResolver.Resolve(_testWs, _defaultWsConfig, charOverride, null);

            // Action 目录覆盖
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Custom/Ellen/SpecialAction", effective.ActionConfigDirectory);
            Assert.AreEqual(ConfigSource.CharacterOverride, effective.ActionConfigDirectorySource);

            // 命名规则覆盖
            Assert.AreEqual("Special_{ActionName}", effective.NamingTemplate);
            Assert.AreEqual(ConfigSource.CharacterOverride, effective.NamingTemplateSource);

            // 冲突策略覆盖
            Assert.AreEqual(ConflictPolicy.Overwrite, effective.ConflictPolicy);
            Assert.AreEqual(ConfigSource.CharacterOverride, effective.ConflictPolicySource);

            // 未覆盖的项（如 TimelineJson）保持继承
            Assert.AreEqual("Assets/GameClient/TimelineRoot/Json/Role/Ellen", effective.TimelineJsonDirectory);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effective.TimelineJsonDirectorySource);
        }

        [Test]
        public void Test03_TaskOverride_TakesHighestPrecedence()
        {
            var charOverride = new CharacterOverrideConfig
            {
                WorkspaceId = "Role_Ellen",
                ActionSubFolderOverride = "Custom/Ellen/SpecialAction",
                HasConflictPolicyOverride = true,
                ConflictPolicyOverride = ConflictPolicy.Overwrite
            };

            var taskParams = new TaskOverrideParams
            {
                ActionDirectoryOverride = "Assets/Temp/BatchGenerate",
                ConflictPolicyOverride = ConflictPolicy.AutoRename
            };

            var effective = EffectiveConfigResolver.Resolve(_testWs, _defaultWsConfig, charOverride, taskParams);

            // 任务临时参数压倒角色覆盖
            Assert.AreEqual("Assets/Temp/BatchGenerate", effective.ActionConfigDirectory);
            Assert.AreEqual(ConfigSource.TaskOverride, effective.ActionConfigDirectorySource);

            Assert.AreEqual(ConflictPolicy.AutoRename, effective.ConflictPolicy);
            Assert.AreEqual(ConfigSource.TaskOverride, effective.ConflictPolicySource);
        }

        [Test]
        public void Test04_LubanCharacterSourceAdapter_LoadsAndValidates()
        {
            var all = LubanCharacterSourceAdapter.GetAllCharacters();
            Assert.IsNotNull(all);
            Assert.IsTrue(all.Count > 0, "应成功加载到 Luban 角色");

            // 验证已知角色
            Assert.IsTrue(LubanCharacterSourceAdapter.TryGetCharacterByName("Ellen", out var ellen));
            Assert.AreEqual("Ellen", ellen.EnumName);
            Assert.AreEqual(1003, ellen.CharacterId);

            Assert.IsTrue(LubanCharacterSourceAdapter.TryGetCharacter(1001, out var anbi));
            Assert.AreEqual("Anbi", anbi.EnumName);

            // 验证合法性检查
            Assert.IsTrue(LubanCharacterSourceAdapter.IsCharacterValid("1003"));
            Assert.IsTrue(LubanCharacterSourceAdapter.IsCharacterValid("Ellen"));
            Assert.IsFalse(LubanCharacterSourceAdapter.IsCharacterValid("NonExistentCharacter_9999"));
        }

        [Test]
        public void Test05_ConfigValidator_DetectsErrorsAndTraversals()
        {
            // 路径穿越配置
            var invalidWsConfig = new WorkbenchWorkspaceConfig
            {
                ActionConfigRoot = "Assets/Resources/../../System32",
                TimelineJsonRoot = "Assets/Valid/Json",
                TimelineAssetRoot = "Assets/Valid/Assets"
            };

            var report1 = WorkbenchConfigValidator.ValidateWorkspaceConfig(invalidWsConfig);
            Assert.IsTrue(report1.HasErrors, "存在路径穿越应报告 Error");

            // 角色覆盖穿越与非法 Luban 绑定
            var invalidOverride = new CharacterOverrideConfig
            {
                WorkspaceId = "Role_Ellen",
                ActionSubFolderOverride = "../EscapedFolder",
                LubanCharacterId = "GhostCharacterNotExist"
            };

            var report2 = WorkbenchConfigValidator.ValidateCharacterOverride(invalidOverride);
            Assert.IsTrue(report2.HasErrors, "存在子目录穿越应报告 Error");
            Assert.IsTrue(report2.HasWarnings, "非法 Luban 绑定应报告 Warning");
        }

        [Test]
        public void Test06_Storage_SafeRoundTrip_PreservesFields()
        {
            var config = new WorkbenchWorkspaceConfig
            {
                ActionConfigRoot = "Assets/CustomActionRoot",
                NamingTemplate = "Custom_{ActionName}",
                DefaultConflictPolicy = ConflictPolicy.Overwrite
            };

            // 测试 Clone 与 Normalize
            var clone = config.Clone();
            Assert.AreEqual(config.ActionConfigRoot, clone.ActionConfigRoot);
            Assert.AreEqual(config.NamingTemplate, clone.NamingTemplate);
            Assert.AreEqual(config.DefaultConflictPolicy, clone.DefaultConflictPolicy);
        }
    }
}
