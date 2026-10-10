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
    public class ActionMappingDiagnosticTests
    {
        [Test]
        public void Test01_CalculateMatchScore_PrioritizesSameId()
        {
            var skill = new SkillTableItem { Id = 140080, Name = "Ellen_Dash_Start_Front" };

            var dummyAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction.ID = 140080;
            dummyAction.Name = "Ellen_Action_Attack_Dash_Start_Front";

            var actionItem = new ActionAssetIndexItem("guid-dash", "Assets/Fake/Ellen_Action_Attack_Dash_Start_Front.asset", dummyAction);

            float score = ActionMappingDiagnosticService.CalculateMatchScore(skill, actionItem, "Ellen");

            // 同 ID 且核心词素重合，得分应高于 1500 分
            Assert.IsTrue(score >= 1500f, $"期望得分 >= 1500，实际得分: {score}");

            Object.DestroyImmediate(dummyAction);
        }

        [Test]
        public void Test02_CalculateMatchScore_PenalizesConflictingNumbers()
        {
            // 01_01 与 01_02 决不能混淆
            var skill01 = new SkillTableItem { Id = 140140, Name = "Ellen_Attack_01_01" };

            var dummyAction02 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction02.ID = 0; // 未写入 ID
            dummyAction02.Name = "Ellen_Action_Attack_Normal_01_02";

            var actionItem02 = new ActionAssetIndexItem("guid-02", "Assets/Fake/Ellen_Action_Attack_Normal_01_02.asset", dummyAction02);

            float score = ActionMappingDiagnosticService.CalculateMatchScore(skill01, actionItem02, "Ellen");

            // 编号冲突时应被严重扣分（负分）
            Assert.IsTrue(score < 0f, $"期望编号冲突得负分，实际得分: {score}");

            Object.DestroyImmediate(dummyAction02);
        }

        [Test]
        public void Test03_CalculateMatchScore_RewardsSameNumberSuffix()
        {
            // 01_01 与 Normal_01_01 编号一致，分词重合
            var skill01 = new SkillTableItem { Id = 140140, Name = "Ellen_Attack_01_01" };

            var dummyAction01 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction01.ID = 0; // 即使 ID 尚未写入
            dummyAction01.Name = "Ellen_Action_Attack_Normal_01_01";

            var actionItem01 = new ActionAssetIndexItem("guid-01", "Assets/Fake/Ellen_Action_Attack_Normal_01_01.asset", dummyAction01);

            float score = ActionMappingDiagnosticService.CalculateMatchScore(skill01, actionItem01, "Ellen");

            // 应获得正向高分
            Assert.IsTrue(score >= 500f, $"期望同编号得高分，实际得分: {score}");

            Object.DestroyImmediate(dummyAction01);
        }

        [Test]
        public void Test04_SyncActionId_UpdatesAssetId()
        {
            var dummyAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            dummyAction.ID = 0;

            bool success = ActionMappingDiagnosticService.SyncActionId(dummyAction, 140080);

            Assert.IsTrue(success);
            Assert.AreEqual(140080, dummyAction.ID);

            Object.DestroyImmediate(dummyAction);
        }
    }
}
