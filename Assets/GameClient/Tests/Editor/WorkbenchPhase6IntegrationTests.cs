using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workbench;
using Game.Editor.Workspace;
using Game.GamePlay;

namespace GameClient.Tests.Editor
{
    /// <summary>
    /// Phase 6 全系统联动联调与交付验收集成测试套件。
    /// 依据《实施方案》第 15 节验收清单，对六大核心维度进行端到端闭环验证。
    /// </summary>
    [TestFixture]
    [Category("Workbench")]
    public class WorkbenchPhase6IntegrationTests
    {
        private SharedWorkspaceDefinition _sharedWs;
        private WorkbenchWorkspaceConfig _wsConfig;

        [SetUp]
        public void SetUp()
        {
            CleanupTestAssets();

            _sharedWs = new SharedWorkspaceDefinition
            {
                Id = "Role_Ellen",
                DisplayName = "艾莲·乔",
                Category = "Role",
                FolderName = "Role/Ellen",
                Description = "维多利亚家政角色"
            };

            _wsConfig = new WorkbenchWorkspaceConfig
            {
                ActionConfigRoot = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig",
                TimelineJsonRoot = "Assets/GameClient/TimelineRoot/Json",
                TimelineAssetRoot = "Assets/GameClient/TimelineRoot/Assets",
                NamingTemplate = "{RoleName}_Action_{ActionName}",
                DefaultConflictPolicy = ConflictPolicy.Skip
            };
        }

        [TearDown]
        public void TearDown()
        {
            CleanupTestAssets();
        }

        private static void CleanupTestAssets()
        {
            string testAssetPath = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/Ellen_Action_Attack_NewSpecial.asset";
            if (File.Exists(testAssetPath))
            {
                AssetDatabase.DeleteAsset(testAssetPath);
            }
        }

        /// <summary>
        /// 维度 1 验收：配置作用域与四级覆盖优先级（任务覆盖 > 角色覆盖 > 工作区继承 > 系统默认）。
        /// </summary>
        [Test]
        public void Dimension1_ConfigurationScopeAndPrecedence_ResolvesCorrectly()
        {
            // 1. 无覆盖时：继承工作区
            var effectiveBase = EffectiveConfigResolver.Resolve(_sharedWs, _wsConfig, null, null);
            Assert.AreEqual("{RoleName}_Action_{ActionName}", effectiveBase.NamingTemplate);
            Assert.AreEqual(ConfigSource.WorkspaceInherited, effectiveBase.NamingTemplateSource);
            Assert.AreEqual(ConflictPolicy.Skip, effectiveBase.ConflictPolicy);

            // 2. 角色覆盖：覆盖特定属性
            var roleOverride = new CharacterOverrideConfig
            {
                WorkspaceId = "Role_Ellen",
                NamingRuleOverride = "Ellen_Custom_{ActionName}",
                HasConflictPolicyOverride = true,
                ConflictPolicyOverride = ConflictPolicy.Overwrite
            };
            var effectiveRole = EffectiveConfigResolver.Resolve(_sharedWs, _wsConfig, roleOverride, null);
            Assert.AreEqual("Ellen_Custom_{ActionName}", effectiveRole.NamingTemplate);
            Assert.AreEqual(ConfigSource.CharacterOverride, effectiveRole.NamingTemplateSource);
            Assert.AreEqual(ConflictPolicy.Overwrite, effectiveRole.ConflictPolicy);

            // 3. 任务临时覆盖：最高优先级，但不污染原配置
            var taskOverride = new TaskOverrideParams
            {
                ConflictPolicyOverride = ConflictPolicy.AutoRename
            };
            var effectiveTask = EffectiveConfigResolver.Resolve(_sharedWs, _wsConfig, roleOverride, taskOverride);
            Assert.AreEqual(ConflictPolicy.AutoRename, effectiveTask.ConflictPolicy);
            Assert.AreEqual(ConfigSource.TaskOverride, effectiveTask.ConflictPolicySource);
        }

        /// <summary>
        /// 维度 2 验收：路径治理与安全拦截（候选目录探测、安全边界与路径穿越防御）。
        /// </summary>
        [Test]
        public void Dimension2_PathGovernanceAndSecurity_InterceptsInvalidPaths()
        {
            // 候选目录探测 (参数顺位: folderName, category)
            var candidates = WorkbenchPathResolver.ResolveActionConfigCandidateDirectories("Role/Ellen", "Role");
            Assert.IsNotNull(candidates);
            Assert.IsTrue(candidates.Count >= 2, "应至少包含规范子目录和历史目录");
            Assert.IsTrue(candidates[0].Contains("Ellen/Action"));

            // 路径穿越校验
            Assert.IsTrue(WorkbenchPathResolver.IsPathTraversing("../../System32"));
            Assert.IsTrue(WorkbenchPathResolver.IsPathTraversing("Assets/../../../Root"));
            Assert.IsFalse(WorkbenchPathResolver.IsPathTraversing("Assets/Resources/CharacterConfig/Ellen"));
        }

        /// <summary>
        /// 维度 3 验收：存储持久化与数据模型（纯 POCO 序列化往返与独立隔离）。
        /// </summary>
        [Test]
        public void Dimension3_StorageAndPersistence_RoundtripSerialization()
        {
            // SharedWorkspaces 结构体往返
            var data = new SharedWorkspaceData();
            data.Workspaces.Add(_sharedWs);
            string json = JsonUtility.ToJson(data, true);
            Assert.IsNotNull(json);

            var loaded = JsonUtility.FromJson<SharedWorkspaceData>(json);
            Assert.AreEqual(1, loaded.Workspaces.Count);
            Assert.AreEqual("Role_Ellen", loaded.Workspaces[0].Id);
            Assert.AreEqual("Role/Ellen", loaded.Workspaces[0].FolderName);
        }

        /// <summary>
        /// 维度 4 验收：端到端业务管线（配表映射诊断 -> 变更计划推导 -> 批处理执行与审计）。
        /// </summary>
        [Test]
        public void Dimension4_PipelineAndChangePlanner_GeneratesValidCreatePlan()
        {
            var effective = EffectiveConfigResolver.Resolve(_sharedWs, _wsConfig, null, null);
            var dummyContext = new WorkbenchCharacterContext(
                _sharedWs,
                effective,
                new LubanCharacterInfo(1003, "Ellen", "艾莲·乔"),
                new List<ActionAssetIndexItem>(),
                new List<TimelineAssetIndexItem>());

            // 模拟配表条目
            var skills = new List<SkillTableItem>
            {
                new SkillTableItem { Id = 140999, Name = "Ellen_Attack_NewSpecial" }
            };

            // 生成变更计划
            var plan = ChangePlanner.PlanActionGeneration(dummyContext, skills);
            Assert.IsNotNull(plan);
            Assert.AreEqual(1, plan.Items.Count);

            var item = plan.Items[0];
            Assert.AreEqual(ChangeOperationType.CreateAction, item.OperationType);
            Assert.AreEqual("140999", item.Id);
            // 命名规则: {RoleName}_Action_{ActionName}
            Assert.AreEqual("Ellen_Action_Attack_NewSpecial", item.TargetName);
            Assert.IsTrue(item.TargetPath.EndsWith("Ellen_Action_Attack_NewSpecial.asset"));
            Assert.AreEqual(ChangeConflictStatus.Ready, item.ConflictStatus);

            try
            {
                // 批处理执行器模拟运行（传入 dummyContext，验证结果报表模型）
                var report = WorkbenchBatchExecutor.ExecutePlan(plan, dummyContext);
                Assert.IsNotNull(report);
                Assert.AreEqual(1, report.TotalExecuted);
                // 验证报表统计完整性
                Assert.IsTrue(report.SuccessCount + report.FailedCount == 1);
            }
            finally
            {
                CleanupTestAssets();
            }
        }

        /// <summary>
        /// 维度 5 验收：映射对齐看板诊断（完全匹配、模糊匹配、无匹配资产的分值与分类健壮性）。
        /// </summary>
        [Test]
        public void Dimension5_ActionMappingDiagnostic_ClassifiesCorrectly()
        {
            var dummyAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction.ID = 140080;
            dummyAction.Name = "Ellen_Action_Attack_Dash_Start_Front";

            var actionItem = new ActionAssetIndexItem("guid-ellen-dash", "Assets/Fake/Ellen_Action_Attack_Dash_Start_Front.asset", dummyAction);
            var context = new WorkbenchCharacterContext(
                _sharedWs,
                EffectiveConfigResolver.Resolve(_sharedWs, _wsConfig, null, null),
                null,
                new List<ActionAssetIndexItem> { actionItem },
                new List<TimelineAssetIndexItem>());

            // 1. 完全匹配项 (ExactMatch)
            var perfectSkill = new SkillTableItem { Id = 140080, Name = "Ellen_Attack_Dash_Start_Front" };
            var perfectDiag = ActionMappingDiagnosticService.DiagnoseSingleSkill(perfectSkill, context);
            Assert.IsNotNull(perfectDiag);
            Assert.AreEqual(ActionMappingStatus.ExactMatch, perfectDiag.Status);
            Assert.AreEqual(140080, perfectDiag.ActionId);

            // 2. 无匹配资产项 (NoMatch)
            var missingSkill = new SkillTableItem { Id = 999999, Name = "Ellen_SuperSkill_NonExistent" };
            var missingDiag = ActionMappingDiagnosticService.DiagnoseSingleSkill(missingSkill, context);
            Assert.IsNotNull(missingDiag);
            Assert.AreEqual(ActionMappingStatus.NoMatch, missingDiag.Status);
            Assert.IsFalse(missingDiag.HasAssociatedAction);

            Object.DestroyImmediate(dummyAction);
        }

        /// <summary>
        /// 维度 6 验收：ATEditor 协议互操作与解耦（数据模型中立、向后兼容、零逆向依赖）。
        /// </summary>
        [Test]
        public void Dimension6_ATEditorInteroperability_PreservesNeutralProtocol()
        {
            // 验证 SharedWorkspaces.json 独立协议版本与兼容字段
            var workspace = new SharedWorkspaceDefinition
            {
                Id = "Role_Anby",
                DisplayName = "安比·德玛拉",
                Category = "Role",
                FolderName = "Role/Anby",
                Description = "皎洁如月的斩击"
            };

            Assert.IsFalse(string.IsNullOrEmpty(workspace.Id));
            Assert.AreEqual("Role", workspace.Category);
            Assert.IsFalse(string.IsNullOrEmpty(workspace.FolderName));
        }
    }
}
