using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Editor.Workspace;
using Game.Editor.Workbench;
using Game.Editor.ActionTransition;

namespace GameClient.Tests.Editor
{
    /// <summary>
    /// Phase 3: 业务管线接入与统一消费单元测试
    /// 验证资产扫描、Timeline 匹配对齐、孤立排查与角色业务上下文构建
    /// </summary>
    public class WorkbenchPipelineTests
    {
        private SharedWorkspaceDefinition _ellenWs;

        [SetUp]
        public void SetUp()
        {
            var sharedData = SharedWorkspaceFileIO.Load();
            _ellenWs = sharedData != null ? sharedData.FindById("Role_Ellen") : null;

            if (_ellenWs == null)
            {
                _ellenWs = new SharedWorkspaceDefinition(
                    "Role_Ellen",
                    "Role",
                    "艾莲",
                    "Role/Ellen",
                    0,
                    "测试用艾莲角色"
                );
            }
        }

        [Test]
        public void Test01_BuildContext_BuildsValidContextWithActionsAndTimelines()
        {
            var context = WorkbenchAssetScanner.BuildContext(_ellenWs);

            Assert.IsNotNull(context);
            Assert.AreEqual("Role_Ellen", context.Workspace.Id);
            Assert.IsNotNull(context.EffectiveConfig);
            Assert.AreEqual("Ellen", context.EffectiveConfig.RoleName);

            // 验证真实工程资产扫描结果（Ellen 角色在工程中真实存在动作资产）
            Assert.IsTrue(context.ActionCount > 0, "艾莲角色应扫描到工程中的 Action 资产");
            Assert.IsTrue(context.TimelineJsonCount > 0 || context.TimelineSoCount > 0, "艾莲角色应扫描到工程中的时间轴资产");

            Debug.Log($"[Test01] 艾莲 Context 扫描成功: 动作={context.ActionCount}, JSON={context.TimelineJsonCount}, SO={context.TimelineSoCount}");
        }

        [Test]
        public void Test02_ActionTimelineMatcher_DiagnosesMatch()
        {
            var context = WorkbenchAssetScanner.BuildContext(_ellenWs);
            Assert.IsTrue(context.ActionCount > 0);

            var firstAction = context.Actions[0];
            var result = ActionTimelineMatcher.DiagnoseMatch(firstAction, context);

            Assert.IsNotNull(result);
            Assert.AreEqual(firstAction, result.ActionItem);
            Assert.IsFalse(string.IsNullOrEmpty(result.ExpectedJsonPath));
            Assert.IsFalse(string.IsNullOrEmpty(result.ExpectedSoPath));

            Debug.Log($"[Test02] 首个动作 [{firstAction.ActionName}] 匹配诊断状态: {result.Status}, JSON 路径={result.ExpectedJsonPath}");
        }

        [Test]
        public void Test03_FindOrphanTimelines_RunsWithoutException()
        {
            var context = WorkbenchAssetScanner.BuildContext(_ellenWs);
            var orphans = ActionTimelineMatcher.FindOrphanTimelines(context);

            Assert.IsNotNull(orphans);
            // 孤立时间轴数量可以为 0 或大于 0，核心验证方法正常执行且路径比对稳健
            Debug.Log($"[Test03] 艾莲孤立时间轴数量: {orphans.Count}");
        }

        [Test]
        public void Test04_ActionTransitionWorkspaceService_Integration()
        {
            // 验证上层业务服务通过 SharedWorkspaceDefinition 获取资产管线
            var actions = ActionTransitionWorkspaceService.GetActionsForWorkspace(_ellenWs);

            Assert.IsNotNull(actions);
            Assert.IsTrue(actions.Count > 0, "ActionTransitionWorkspaceService 应能成功返回艾莲的动作列表");
            Assert.IsTrue(actions.TrueForAll(a => a != null));
        }

        [Test]
        public void Test05_Context_LookupHelpers()
        {
            var context = WorkbenchAssetScanner.BuildContext(_ellenWs);
            if (context.ActionCount > 0)
            {
                var target = context.Actions[0];
                var foundByName = context.FindAction(target.ActionName);
                Assert.IsNotNull(foundByName);
                Assert.AreEqual(target.Guid, foundByName.Guid);

                var foundByGuid = context.FindActionByGuid(target.Guid);
                Assert.IsNotNull(foundByGuid);
                Assert.AreEqual(target.ActionName, foundByGuid.ActionName);
            }
        }
    }
}
