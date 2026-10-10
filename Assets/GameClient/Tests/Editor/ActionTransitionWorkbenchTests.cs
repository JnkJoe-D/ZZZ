using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.GamePlay;

namespace Game.Tests.ActionSystem
{
    public class DummyActionConfig : ActionConfigAsset
    {
    }

    [TestFixture]
    public class ActionTransitionWorkbenchTests
    {
        private DummyActionConfig _sourceAction;
        private DummyActionConfig _targetR1;
        private DummyActionConfig _targetR2;
        private DummyActionConfig _targetR3;
        private DummyActionConfig _completeAction;

        [SetUp]
        public void SetUp()
        {
            _sourceAction = ScriptableObject.CreateInstance<DummyActionConfig>();
            _sourceAction.name = "SourceAction_Loop";
            _sourceAction.CompleteMode = ActionCompleteMode.Stay; // 模拟循环动作

            _targetR1 = ScriptableObject.CreateInstance<DummyActionConfig>();
            _targetR1.name = "Target_R1_P50";

            _targetR2 = ScriptableObject.CreateInstance<DummyActionConfig>();
            _targetR2.name = "Target_R2_P100";

            _targetR3 = ScriptableObject.CreateInstance<DummyActionConfig>();
            _targetR3.name = "Target_R3_P20";

            _completeAction = ScriptableObject.CreateInstance<DummyActionConfig>();
            _completeAction.name = "Target_Complete";
        }

        [TearDown]
        public void TearDown()
        {
            if (_sourceAction != null) Object.DestroyImmediate(_sourceAction);
            if (_targetR1 != null) Object.DestroyImmediate(_targetR1);
            if (_targetR2 != null) Object.DestroyImmediate(_targetR2);
            if (_targetR3 != null) Object.DestroyImmediate(_targetR3);
            if (_completeAction != null) Object.DestroyImmediate(_completeAction);
        }

        [Test]
        public void TransitionTopology_OrderContract_FollowsPriorityThenLoopThenCompleteAction()
        {
            // 契约要求：普通路由（Priority 降序） -> Loop（倒数第二） -> CompleteAction（排最后）
            _sourceAction.Routes = new List<ActionRoute>
            {
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR1, Priority = 50 },
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR2, Priority = 100 },
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR3, Priority = 20 },
            };

            // 同时配置自然顺承动作
            _sourceAction.CompleteMode = ActionCompleteMode.TransitToAction;
            _sourceAction.CompleteAction = _completeAction;

            // 临时让 sourceAction 的时间轴处于 Loop 状态
            var timeline = ScriptableObject.CreateInstance<ATEditor.ActionTimeline>();
            timeline.isLoop = true;
            _sourceAction.actionTimelineSO = timeline;

            try
            {
                var targets = ActionTransitionTopologyService.DiscoverTargets(_sourceAction);

                Assert.AreEqual(5, targets.Count, "应提取出 3 个普通路由 + 1 个自循环 Loop + 1 个 CompleteAction");

                // 1. 普通路由分支按动作名称字母序排列 (Target_R1_P50 -> Target_R2_P100 -> Target_R3_P20)
                Assert.AreEqual(_targetR1, targets[0].TargetAction);
                Assert.AreEqual(50, targets[0].MaxPriority);
                Assert.AreEqual(TransitionTargetKind.RouteBranch, targets[0].Kind);

                Assert.AreEqual(_targetR2, targets[1].TargetAction);
                Assert.AreEqual(100, targets[1].MaxPriority);
                Assert.AreEqual(TransitionTargetKind.RouteBranch, targets[1].Kind);

                Assert.AreEqual(_targetR3, targets[2].TargetAction);
                Assert.AreEqual(20, targets[2].MaxPriority);
                Assert.AreEqual(TransitionTargetKind.RouteBranch, targets[2].Kind);

                // 2. Loop 自循环必须排倒数第二
                Assert.AreEqual(_sourceAction, targets[3].TargetAction);
                Assert.AreEqual(TransitionTargetKind.SelfLoop, targets[3].Kind);

                // 3. CompleteAction 必须排最后
                Assert.AreEqual(_completeAction, targets[4].TargetAction);
                Assert.AreEqual(TransitionTargetKind.CompleteAction, targets[4].Kind);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void TransitionTopology_DuplicateRoutesToSameTarget_AggregatesMaxPriorityAndDeduplicates()
        {
            // 同一个目标动作配置了多条路由，必须去重，且 MaxPriority 聚合最高值
            _sourceAction.CompleteMode = ActionCompleteMode.Default;
            _sourceAction.Routes = new List<ActionRoute>
            {
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR1, Priority = 30 },
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR1, Priority = 90 },
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR1, Priority = 60 },
            };

            var targets = ActionTransitionTopologyService.DiscoverTargets(_sourceAction);

            Assert.AreEqual(1, targets.Count, "重复的目标动作必须聚合去重");
            Assert.AreEqual(_targetR1, targets[0].TargetAction);
            Assert.AreEqual(90, targets[0].MaxPriority, "应提取最高优先级 90");
        }

        [Test]
        public void TransitionTable_CRUD_WorksAsExpected()
        {
            // 初始未配置时，默认返回 -1（表示回退使用目标动作自身起手 BlendIn）
            Assert.IsNull(_sourceAction.GetTransition(_targetR1));
            Assert.AreEqual(-1f, _sourceAction.GetTransitionCrossfade(_targetR1));

            // 配置覆盖混合时间
            _sourceAction.SetTransition(_targetR1, 0.35f, true, 0.8f);
            var item = _sourceAction.GetTransition(_targetR1);
            Assert.IsNotNull(item);
            Assert.AreEqual(0.35f, item.CrossfadeDuration);
            Assert.IsTrue(item.HasCustomExitTime);
            Assert.AreEqual(0.8f, item.CustomExitTime);
            Assert.AreEqual(0.35f, _sourceAction.GetTransitionCrossfade(_targetR1));

            // 设为 -1 恢复默认
            _sourceAction.SetTransition(_targetR1, -1f);
            Assert.AreEqual(-1f, _sourceAction.GetTransitionCrossfade(_targetR1));
        }

        [Test]
        public void WorkspaceService_GetAllWorkspaces_ReturnsConfiguredWorkspaces()
        {
            var workspaces = Game.Editor.ActionTransition.ActionTransitionWorkspaceService.GetAllWorkspaces();
            Assert.IsNotNull(workspaces);
            Assert.IsTrue(workspaces.Count > 0, "工程中应存在配置的角色工作区");

            var ellenWs = Game.Editor.ActionTransition.ActionTransitionWorkspaceService.GetWorkspaceById("Role_Ellen");
            Assert.IsNotNull(ellenWs);
            Assert.AreEqual("Role", ellenWs.Category);
            Assert.IsNotNull(ellenWs.PreviewPrefab, "艾莲工作区应绑定了默认预览模型");
        }

        [Test]
        public void CompleteAction_TransitionTarget_SupportsCustomExitTime()
        {
            _sourceAction.CompleteMode = ActionCompleteMode.TransitToAction;
            _sourceAction.CompleteAction = _targetR2;

            // 配置针对 CompleteAction 的提前 ExitTime 与 Crossfade
            _sourceAction.SetTransition(_targetR2, 0.25f, true, 0.65f);

            var targets = ActionTransitionTopologyService.DiscoverTargets(_sourceAction);
            var completeTarget = targets.Find(t => t.Kind == TransitionTargetKind.CompleteAction);

            Assert.IsNotNull(completeTarget);
            Assert.AreEqual(_targetR2, completeTarget.TargetAction);
            Assert.IsTrue(completeTarget.HasCustomExitTime);
            Assert.AreEqual(0.65f, completeTarget.CustomExitTime);
            Assert.AreEqual(0.25f, completeTarget.ConfiguredCrossfade);
        }

        [Test]
        public void TransitionTable_StartTimeAndAliases_WorkAsExpected()
        {
            // 配置全套新参数：EndTime, HasEndTime, BlendDuration, StartTime, HasStartTime
            _sourceAction.SetTransition(
                targetAction: _targetR1,
                blendDuration: 0.4f,
                hasEndTime: true,
                endTime: 0.75f,
                hasStartTime: true,
                startTime: 0.2f
            );

            var item = _sourceAction.GetTransition(_targetR1);
            Assert.IsNotNull(item);
            // 核心新字段断言
            Assert.AreEqual(0.4f, item.BlendDuration);
            Assert.IsTrue(item.HasEndTime);
            Assert.AreEqual(0.75f, item.EndTime);
            Assert.IsTrue(item.HasStartTime);
            Assert.AreEqual(0.2f, item.StartTime);

            // 向后兼容别名断言
            Assert.AreEqual(0.4f, item.CrossfadeDuration);
            Assert.IsTrue(item.HasCustomExitTime);
            Assert.AreEqual(0.75f, item.CustomExitTime);

            // Topology 扫描测试
            _sourceAction.Routes = new List<ActionRoute>
            {
                new ActionRoute { ExecuteType = ExecuteTarget.Action, ExecuteAction = _targetR1, Priority = 50 }
            };
            var targets = ActionTransitionTopologyService.DiscoverTargets(_sourceAction);
            var targetInfo = targets.Find(t => t.TargetAction == _targetR1);
            Assert.IsNotNull(targetInfo);
            Assert.IsTrue(targetInfo.HasStartTime);
            Assert.AreEqual(0.2f, targetInfo.StartTime);
            Assert.IsTrue(targetInfo.HasEndTime);
            Assert.AreEqual(0.75f, targetInfo.EndTime);
            Assert.AreEqual(0.4f, targetInfo.BlendDuration);
        }
    }
}
