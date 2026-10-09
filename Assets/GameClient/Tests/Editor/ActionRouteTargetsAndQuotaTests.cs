using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    public class ActionRouteTargetsAndQuotaTests
    {
        private GameObject _roleGo;
        private RoleEntity _role;

        [SetUp]
        public void SetUp()
        {
            _roleGo = new GameObject("TestRole_RouteTargets");
            _role = _roleGo.AddComponent<RoleEntity>();
            _role.EnsureRuntimeInitialized();
            _role.SetControlActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_roleGo != null)
            {
                Object.DestroyImmediate(_roleGo);
            }
        }

        [Test]
        public void Test01_ActionRoute_TargetProperties_Action()
        {
            var route = new ActionRoute();
            var targetAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();

            // 属性赋值会自动同步至目标列表
            route.ExecuteAction = targetAction;

            Assert.AreEqual(1, route.Targets.Count);
            Assert.IsInstanceOf<ActionRouteTarget>(route.Targets[0]);
            var actionTarget = route.Targets[0] as ActionRouteTarget;
            Assert.AreEqual(targetAction, actionTarget.Action);
            Assert.AreEqual(ExecuteTarget.Action, route.ExecuteType);
            Assert.IsTrue(route.IsValid());
        }

        [Test]
        public void Test02_ActionRoute_TargetProperties_Event()
        {
            var route = new ActionRoute();
            var targetAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            route.ExecuteAction = targetAction;
            route.RouteExecuteEvent = ExecuteEvent.TimelineSkip;

            // 包含一个动作目标与一个伴随事件目标
            Assert.AreEqual(2, route.Targets.Count);
            Assert.AreEqual(targetAction, route.GetActionTarget()?.Action);
            Assert.AreEqual(ExecuteEvent.TimelineSkip, route.GetFirstEventTarget()?.RouteExecuteEvent);
            Assert.AreEqual(ExecuteTarget.Action, route.ExecuteType);
            Assert.IsTrue(route.IsValid());
        }

        [Test]
        public void Test03_RouteExecutionTracker_QuotaAndIsConsumed()
        {
            var tracker = new RouteExecutionTracker();
            var route = new ActionRoute();

            var eventTarget = new EventRouteTarget
            {
                RouteExecuteEvent = ExecuteEvent.TimelineSkip,
                MaxExecuteCount = 1
            };
            route.Targets.Add(eventTarget);

            // 初始状态 未耗尽 可以执行
            Assert.IsFalse(tracker.IsRouteConsumed(route));
            Assert.IsTrue(tracker.CanExecuteTarget(eventTarget));
            Assert.AreEqual(0, tracker.GetExecutedCount(eventTarget));

            // 执行一次
            tracker.RecordExecuted(route, eventTarget);
            Assert.AreEqual(1, tracker.GetExecutedCount(eventTarget));

            // 执行一次后配额耗尽 无法再执行 且整条路由被标记为已耗尽
            Assert.IsFalse(tracker.CanExecuteTarget(eventTarget));
            Assert.IsTrue(tracker.IsRouteConsumed(route));

            // 模拟切动作 配额重置
            tracker.ResetForNewAction();
            Assert.IsFalse(tracker.IsRouteConsumed(route));
            Assert.IsTrue(tracker.CanExecuteTarget(eventTarget));
            Assert.AreEqual(0, tracker.GetExecutedCount(eventTarget));
        }

        [Test]
        public void Test04_ActionRoute_Evaluate_ShortCircuitsWhenConsumed()
        {
            var tracker = _role.ActionController.RouteExecutionTracker;
            var route = new ActionRoute
            {
                TriggerStrategy = new AutoTransitionTrigger
                {
                    Timing = RouteSingleModifierCheckTiming.EveryFrameInWindow
                }
            };

            var eventTarget = new EventRouteTarget
            {
                RouteExecuteEvent = ExecuteEvent.TimelineSkip,
                MaxExecuteCount = 1
            };
            route.Targets.Add(eventTarget);

            // 第一帧 配额未耗尽 评估成功通过
            Assert.IsTrue(route.Evaluate(null, null, _role, null, RouteSingleModifierCheckTiming.EveryFrameInWindow));

            // 模拟执行该路由并记录消耗
            tracker.RecordExecuted(route, eventTarget);

            // 第二帧 由于配额耗尽 评估应当立即短路返回假 杜绝重复触发
            Assert.IsFalse(route.Evaluate(null, null, _role, null, RouteSingleModifierCheckTiming.EveryFrameInWindow));
        }

        [Test]
        public void Test05_ActionRoute_MultiTargets_OrderAndConstraint()
        {
            var route = new ActionRoute();
            var targetAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();

            var eventTarget = new EventRouteTarget { RouteExecuteEvent = ExecuteEvent.ParryAidStart, MaxExecuteCount = 1 };
            var actionTarget = new ActionRouteTarget { Action = targetAction };

            // 按照事件与动作顺序添加
            route.Targets.Add(eventTarget);
            route.Targets.Add(actionTarget);

            Assert.AreEqual(2, route.Targets.Count);
            Assert.AreEqual(eventTarget, route.Targets[0]);
            Assert.AreEqual(actionTarget, route.Targets[1]);
            Assert.IsTrue(route.IsValid());

            // 约束校验 一条路由最多只能有一个动作目标 若加了第二个则校验应该为假
            route.Targets.Add(new ActionRouteTarget { Action = ScriptableObject.CreateInstance<RoleActionConfigAsset>() });
            Assert.IsFalse(route.IsValid());
        }

        [Test]
        public void Test06_ActionRoute_ActionTargetMustBeExactlyOne()
        {
            var route = new ActionRoute();
            var action1 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            var action2 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();

            // 测试若清空目标列表 完整性检查会自动补齐一个动作目标
            route.Targets.Clear();
            route.EnsureActionTargetIntegrity();
            Assert.AreEqual(1, route.Targets.Count);
            Assert.IsInstanceOf<ActionRouteTarget>(route.Targets[0]);

            // 测试若配置了多个动作目标 完整性检查自动保留有效项并剔除多余项
            route.Targets.Clear();
            route.Targets.Add(new ActionRouteTarget { Action = action1 });
            route.Targets.Add(new ActionRouteTarget { Action = null });
            route.Targets.Add(new ActionRouteTarget { Action = action2 });
            route.EnsureActionTargetIntegrity();

            Assert.AreEqual(1, route.Targets.Count);
            var preserved = route.Targets[0] as ActionRouteTarget;
            Assert.IsNotNull(preserved);
            Assert.AreEqual(action1, preserved.Action);
            Assert.IsTrue(route.IsValid());
        }

        [Test]
        public void Test07_ActionRoute_PureEventRoute_IsValid()
        {
            var route = new ActionRoute();
            var eventTarget = new EventRouteTarget
            {
                RouteExecuteEvent = ExecuteEvent.ParryAidStart,
                MaxExecuteCount = 1
            };
            route.Targets.Add(eventTarget);

            // 纯事件路由不包含动作目标 依然判定为合法有效
            Assert.AreEqual(1, route.Targets.Count);
            Assert.IsNull(route.GetActionTarget());
            Assert.AreEqual(ExecuteTarget.Event, route.ExecuteType);
            Assert.IsTrue(route.IsValid());
        }
    }
}
