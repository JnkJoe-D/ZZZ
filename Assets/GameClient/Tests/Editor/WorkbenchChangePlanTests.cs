using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Editor.Workbench;
using Game.Editor.Workspace;
using Game.GamePlay;

namespace GameClient.Tests.Editor
{
    [TestFixture]
    [Category("Workbench")]
    public class WorkbenchChangePlanTests
    {
        [Test]
        public void Test01_DeduceActionAssetName_CorrectlyFormatsName()
        {
            // 1. 标准配表命名: RoleName_ActionName -> RoleName_Action_ActionName
            string deduced1 = ChangePlanner.DeduceActionAssetName("TyrfingInfested", "TyrfingInfested_Attack_01");
            Assert.AreEqual("TyrfingInfested_Action_Attack_01", deduced1);

            // 2. 玩家自机配表命名: Ellen_Attack_01 -> Ellen_Action_Attack_01
            string deduced2 = ChangePlanner.DeduceActionAssetName("Ellen", "Ellen_Attack_01");
            Assert.AreEqual("Ellen_Action_Attack_01", deduced2);

            // 3. 不带前缀的技能名: Idle -> RoleName_Action_Idle
            string deduced3 = ChangePlanner.DeduceActionAssetName("Ellen", "Idle");
            Assert.AreEqual("Ellen_Action_Idle", deduced3);

            // 4. 已包含 _Action_ 的技能名保持原样
            string deduced4 = ChangePlanner.DeduceActionAssetName("Ellen", "Ellen_Action_Special");
            Assert.AreEqual("Ellen_Action_Special", deduced4);
        }

        [Test]
        public void Test02_PlanActionGeneration_BuildsCorrectPlanItems()
        {
            var ws = new SharedWorkspaceDefinition(
                "Monster_TyrfingInfested",
                SharedWorkspaceDefinition.CategoryMonster,
                "侵蚀提尔锋",
                "Monster/TyrfingInfested",
                0,
                "测试怪物定义"
            );

            var defaultWsConfig = new WorkbenchWorkspaceConfig
            {
                ActionConfigRoot = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig",
                TimelineJsonRoot = "Assets/GameClient/TimelineRoot/Json",
                TimelineAssetRoot = "Assets/GameClient/TimelineRoot/Assets"
            };

            var effCfg = EffectiveConfigResolver.Resolve(ws, defaultWsConfig, null, null);
            var context = new WorkbenchCharacterContext(ws, effCfg, null, null, null);

            var skills = new List<SkillTableItem>
            {
                new SkillTableItem { Id = 200040, Name = "TyrfingInfested_Attack_01", Desc = "普通攻击1" },
                new SkillTableItem { Id = 200050, Name = "TyrfingInfested_Attack_02", Desc = "普通攻击2" }
            };

            var plan = ChangePlanner.PlanActionGeneration(context, skills);

            Assert.IsNotNull(plan);
            Assert.AreEqual(2, plan.TotalCount);
            Assert.AreEqual("TyrfingInfested_Action_Attack_01", plan.Items[0].TargetName);
            Assert.AreEqual("TyrfingInfested_Action_Attack_02", plan.Items[1].TargetName);
            Assert.AreEqual(ChangeOperationType.CreateAction, plan.Items[0].OperationType);
        }

        [Test]
        public void Test03_PlanTimelineRebind_CategorizesAlreadyLinkedAndUnlinked()
        {
            var ws = new SharedWorkspaceDefinition(
                "Role_Ellen",
                SharedWorkspaceDefinition.CategoryRole,
                "艾莲",
                "Role/Ellen",
                0,
                "测试角色定义"
            );

            var defaultWsConfig = new WorkbenchWorkspaceConfig();
            var effCfg = EffectiveConfigResolver.Resolve(ws, defaultWsConfig, null, null);

            var dummyAction1 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction1.Name = "Ellen_Action_Attack_01";

            var dummyAction2 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction2.Name = "Ellen_Action_Attack_02";

            var linkedItem = new ActionAssetIndexItem("guid-1", "Assets/Fake/Ellen_Action_Attack_01.asset", dummyAction1);
            var unlinkedItem = new ActionAssetIndexItem("guid-2", "Assets/Fake/Ellen_Action_Attack_02.asset", dummyAction2);

            var context = new WorkbenchCharacterContext(ws, effCfg, null, new[] { linkedItem, unlinkedItem }, null);

            var plan = ChangePlanner.PlanTimelineRebind(context, new[] { linkedItem, unlinkedItem });

            Assert.AreEqual(2, plan.TotalCount);
            // unlinkedItem 未找到时间轴时应标明 SourceNotFound 冲突
            Assert.AreEqual(ChangeConflictStatus.SourceNotFound, plan.Items[1].ConflictStatus);
            Assert.IsTrue(plan.Items[1].HasBlockingConflict);

            Object.DestroyImmediate(dummyAction1);
            Object.DestroyImmediate(dummyAction2);
        }

        [Test]
        public void Test04_BatchExecutionReport_TracksSuccessAndFailures()
        {
            var report = new BatchExecutionReport { PlanTitle = "测试计划" };
            var item1 = new ChangePlanItem { TargetName = "Action1" };
            var item2 = new ChangePlanItem { TargetName = "Action2" };

            report.AddResult(item1, true, "成功创建");
            report.AddResult(item2, false, "写入失败");

            Assert.AreEqual(2, report.TotalExecuted);
            Assert.AreEqual(1, report.SuccessCount);
            Assert.AreEqual(1, report.FailedCount);
            Assert.IsTrue(report.HasFailures);
        }
    }
}
