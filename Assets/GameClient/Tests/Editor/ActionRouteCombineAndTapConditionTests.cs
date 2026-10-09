using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using Game.Tests.InputSystem;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    public class ActionRouteCombineAndTapConditionTests
    {
        private GameObject _roleGo;
        private RoleEntity _role;
        private MockInputProvider _mockInput;

        [SetUp]
        public void SetUp()
        {
            _roleGo = new GameObject("TestRole_RouteCombine");
            _role = _roleGo.AddComponent<RoleEntity>();
            _mockInput = new MockInputProvider();

            var adapter = _role.InputAdapter;
            adapter?.Bind(_mockInput);
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
        public void Test01_CommandRouteEvaluator_InputModifiers_AND_Logic()
        {
            // 配置两个条件：条件A (MoveRaw=true), 条件B (KeyHeld=BasicAttack)
            var modA = new RouteModifierCheck
            {
                Category = ModifierCategory.Condition,
                InputCondition = new MoveInputCondition { }
            };
            var modB = new RouteModifierCheck
            {
                Category = ModifierCategory.KeyState,
                RequiredKey = HardwareInputType.BasicAttack,
                Inverse = false
            };
            var list = new List<RouteModifierCheck> { modA, modB };

            // 初始：两者均未满足
            Assert.IsFalse(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AllMatch_AND));

            // 只满足 A
            _mockInput.RawMovement = Vector2.up;
            Assert.IsFalse(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AllMatch_AND));

            // 两者均满足
            _mockInput.SetHeld((int)HardwareInputType.BasicAttack, true);
            Assert.IsTrue(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AllMatch_AND));
        }

        [Test]
        public void Test02_CommandRouteEvaluator_InputModifiers_OR_Logic()
        {
            var modA = new RouteModifierCheck
            {
                Category = ModifierCategory.Condition,
                InputCondition = new MoveInputCondition {  }
            };
            var modB = new RouteModifierCheck
            {
                Category = ModifierCategory.KeyState,
                RequiredKey = HardwareInputType.BasicAttack,
                Inverse = false
            };
            var list = new List<RouteModifierCheck> { modA, modB };

            // 初始：两者均未满足 -> OR 返回 false
            Assert.IsFalse(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AnyMatch_OR));

            // 只满足 A -> OR 返回 true
            _mockInput.RawMovement = Vector2.up;
            Assert.IsTrue(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AnyMatch_OR));

            // A 不满足，只满足 B -> OR 返回 true
            _mockInput.RawMovement = Vector2.zero;
            _mockInput.SetHeld((int)HardwareInputType.BasicAttack, true);
            Assert.IsTrue(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AnyMatch_OR));

            // 两者均满足 -> OR 返回 true
            _mockInput.RawMovement = Vector2.up;
            Assert.IsTrue(CommandRouteEvaluator.MatchesInputModifiers(list, _role, ConditionCombineMode.AnyMatch_OR));
        }

        [Test]
        public void Test03_KeyTapCountCondition_Evaluation()
        {
            var cond = new KeyTapCountCondition
            {
                TargetKey = HardwareInputType.BasicAttack,
                WindowMode = TapTimeWindowMode.SlidingWindow,
                WindowDuration = 0.3f,
                Operator = ConditionCompareOperator.LessOrEqual,
                ThresholdCount = 0
            };

            // 1. 未点击过，满足 <= 0
            Assert.IsTrue(cond.Check(_role));

            // 2. 模拟点击一次
            _mockInput.TapTracker.RecordTap(HardwareInputType.BasicAttack, Time.unscaledTime);
            // 此时点击数 1，不满足 <= 0
            Assert.IsFalse(cond.Check(_role));

            // 3. 修改运算符为 >= 1
            cond.Operator = ConditionCompareOperator.GreaterOrEqual;
            cond.ThresholdCount = 1;
            Assert.IsTrue(cond.Check(_role));

            // 4. 重置清空
            _mockInput.ResetTapTracker(HardwareInputType.BasicAttack);
            Assert.IsFalse(cond.Check(_role));
        }

        [Test]
        public void Test04_KeyInputConditionGroup_CompositeNesting()
        {
            // 组合组：(MoveRaw == true OR TapCount >= 2)
            var group = new KeyInputConditionGroup
            {
                CombineMode = ConditionCombineMode.AnyMatch_OR,
                SubConditions = new List<IKeyInputCondition>
                {
                    new MoveInputCondition(),
                    new KeyTapCountCondition
                    {
                        TargetKey = HardwareInputType.BasicAttack,
                        Operator = ConditionCompareOperator.GreaterOrEqual,
                        ThresholdCount = 2,
                        WindowDuration = 0.5f
                    }
                }
            };

            // 初始：两者都不满足
            Assert.IsFalse(group.Check(_role));

            // 只满足连点 2 次
            _mockInput.TapTracker.RecordTap(HardwareInputType.BasicAttack, Time.unscaledTime);
            _mockInput.TapTracker.RecordTap(HardwareInputType.BasicAttack, Time.unscaledTime);
            Assert.IsTrue(group.Check(_role));

            // 清空并只满足移动
            _mockInput.TapTracker.ClearAll();
            Assert.IsFalse(group.Check(_role));
            _mockInput.RawMovement = Vector2.right;
            Assert.IsTrue(group.Check(_role));
        }

        [Test]
        public void Test05_UserScenario_AutoTransition_NoRapidTap_OR_Hold()
        {
            // 需求场景：在 AutoTransitionTrigger (OnWindowExit) 下，
            // 玩家【没有连续点击】(TapCount <= 0) 或 【保持长按】(IsHeld == true)，二选一流转到目标动作。
            var trigger = new AutoTransitionTrigger
            {
                Timing = RouteSingleModifierCheckTiming.OnWindowExit,
                ConditionCombine = ConditionCombineMode.AnyMatch_OR,
                InputConditions = new List<RouteModifierCheck>
                {
                    // 条件 1: 没有点击 (TapCount <= 0)
                    new RouteModifierCheck
                    {
                        Category = ModifierCategory.Condition,
                        InputCondition = new KeyTapCountCondition
                        {
                            TargetKey = HardwareInputType.BasicAttack,
                            Operator = ConditionCompareOperator.LessOrEqual,
                            ThresholdCount = 0,
                            WindowDuration = 0.3f
                        }
                    },
                    // 条件 2: 保持长按 (IsHeld == true)
                    new RouteModifierCheck
                    {
                        Category = ModifierCategory.KeyState,
                        RequiredKey = HardwareInputType.BasicAttack,
                        Inverse = false
                    }
                }
            };

            // Case A: 玩家停手 (未连点 Tap=0, 未长按 Hold=false)
            // 期望：条件1满足，条件2不满足 -> OR 整体满足 (True，流转到派生/收刀)
            _mockInput.ResetTapTracker(HardwareInputType.BasicAttack);
            _mockInput.SetHeld((int)HardwareInputType.BasicAttack, false);
            Assert.IsTrue(trigger.Evaluate(null, null, _role, RouteSingleModifierCheckTiming.OnWindowExit));

            // Case B: 玩家长按鼠标 (Hold=true)
            // 期望：条件2满足 -> OR 整体满足 (True，流转到蓄力/派生)
            _mockInput.SetHeld((int)HardwareInputType.BasicAttack, true);
            Assert.IsTrue(trigger.Evaluate(null, null, _role, RouteSingleModifierCheckTiming.OnWindowExit));

            // Case C: 玩家狂按鼠标连续平 A (Tap=2 次，且每次迅速松开 Hold=false)
            // 期望：条件1不满足 (TapCount=2 > 0)，条件2不满足 (Hold=false) -> OR 整体不满足 (False，不走此路由，顺利流向普攻连击)
            _mockInput.SetHeld((int)HardwareInputType.BasicAttack, false);
            _mockInput.TapTracker.RecordTap(HardwareInputType.BasicAttack, Time.unscaledTime);
            _mockInput.TapTracker.RecordTap(HardwareInputType.BasicAttack, Time.unscaledTime);
            Assert.IsFalse(trigger.Evaluate(null, null, _role, RouteSingleModifierCheckTiming.OnWindowExit));
        }
    }
}
