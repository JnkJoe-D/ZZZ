using System;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.InputSystem
{
    public class MockInputProvider : IInputProvider
    {
        public Vector2 RawMovement { get; set; }
        public Vector2 RouteMovement { get; set; }
        public Vector2 ResidualMovement { get; set; }

        public Vector2 GetMovementDirection() => RouteMovement;
        public Vector2 GetRawMovementDirection() => RawMovement;
        public Vector2 GetResidualMovementDirection() => ResidualMovement;
        public Vector2 GetLastMovementDirection() => ResidualMovement;

        public bool HasMoveInput() => RouteMovement.sqrMagnitude > 0.001f;
        public bool HasRawMoveInput() => RawMovement.sqrMagnitude > 0.001f;
        public bool HasResidualMoveInput() => ResidualMovement.sqrMagnitude > 0.01f;

        private readonly System.Collections.Generic.HashSet<int> _heldKeys = new();
        public InputTapTracker TapTracker { get; } = new();

        public bool IsHeld(int actionKey) => _heldKeys.Contains(actionKey);
        public void SetHeld(int actionKey, bool held)
        {
            if (held) _heldKeys.Add(actionKey);
            else _heldKeys.Remove(actionKey);
        }

        public int GetTapCount(HardwareInputType actionKey, float windowSeconds) => TapTracker.GetTapCountInWindow(actionKey, windowSeconds, Time.unscaledTime);
        public int GetTapCountSince(HardwareInputType actionKey, float startTime) => TapTracker.GetTapCountSince(actionKey, startTime);
        public void ResetTapTracker(HardwareInputType actionKey) => TapTracker.Clear(actionKey);

#pragma warning disable CS0067
        public event Action OnSwitchNext;
        public event Action OnSwitchPre;
        public event Action OnMoveStarted;
        public event Action OnMovePerformed;
        public event Action OnMoveCanceled;
        public event Action OnMoveHeld;
        public event Action OnMovementZero;
        public event Action OnRawMovementZero;
        public event Action OnEvadeStarted;
        public event Action OnEvadePerformed;
        public event Action OnEvadeCanceled;
        public event Action OnEvadeHeld;
        public event Action OnBasicAttackStarted;
        public event Action OnBasicAttackPerformed;
        public event Action OnBasicAttackCanceled;
        public event Action OnBasicAttackHeld;
        public event Action OnSpecialAttackStarted;
        public event Action OnSpecialAttackPerformed;
        public event Action OnSpecialAttackCanceled;
        public event Action OnSpecialAttackHeld;
        public event Action OnUltimateStarted;
        public event Action OnGameplayInteractStarted;
#pragma warning restore CS0067
    }

    public class InputTurnRoutingTests
    {
        private GameObject _go;
        private RoleEntity _role;
        private MockInputProvider _mockInput;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("TestRole");
            _role = _go.AddComponent<RoleEntity>();
            _mockInput = new MockInputProvider();

            // 为实体注入 InputAdapter
            var adapter = _role.InputAdapter;
            adapter?.Bind(_mockInput);
            _role.SetControlActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void TurnAroundCondition_180DegreeTurn_MatchesSuccessfully()
        {
            var condition = new MoveTurnAroundCondition
            {
                MinAngle = 135f,
                RequireRawInput = true,
                RequireResidualInput = true
            };

            // 之前向前奔跑残留为 (0, 1)，当前原生物理按键突然向后拉 (0, -1)
            _mockInput.ResidualMovement = new Vector2(0, 1);
            _mockInput.RawMovement = new Vector2(0, -1);
            _mockInput.RouteMovement = new Vector2(0, -1);

            Assert.IsTrue(condition.Check(_role), "180度掉头应当满足转向条件");
        }

        [Test]
        public void TurnAroundCondition_90DegreeTurn_DoesNotTrigger180Turn()
        {
            var condition = new MoveTurnAroundCondition
            {
                MinAngle = 135f,
                RequireRawInput = true,
                RequireResidualInput = true
            };

            // 之前向前跑 (0, 1)，当前向右拉 (1, 0)，夹角 90 度
            _mockInput.ResidualMovement = new Vector2(0, 1);
            _mockInput.RawMovement = new Vector2(1, 0);

            Assert.IsFalse(condition.Check(_role), "90度直角转弯不应触发180度掉头条件");
        }

        [Test]
        public void TurnAroundCondition_StartFromIdle_DoesNotFalseTrigger()
        {
            var condition = new MoveTurnAroundCondition
            {
                MinAngle = 135f,
                RequireRawInput = true,
                RequireResidualInput = true
            };

            // 从完全静止站立起步，此前无残留输入
            _mockInput.ResidualMovement = Vector2.zero;
            _mockInput.RawMovement = new Vector2(0, -1);

            Assert.IsFalse(condition.Check(_role), "从静止起步无残留输入时，严禁误触发180度掉头动作");
        }

        [Test]
        public void MoveInputDeltaCondition_VectorDifference_CalculatesCorrectly()
        {
            var condition = new MoveInputDeltaCondition
            {
                MinDeltaMagnitude = 1.5f
            };

            // 前后掉头，Delta = (0, -1) - (0, 1) = (0, -2)，模长为 2.0
            _mockInput.ResidualMovement = new Vector2(0, 1);
            _mockInput.RawMovement = new Vector2(0, -1);

            Assert.IsTrue(condition.Check(_role), "180度反向输入 Delta 应达到 2.0，满足变化量条件");

            // 同向输入，Delta = (0, 1) - (0, 1) = (0, 0)，模长为 0
            _mockInput.RawMovement = new Vector2(0, 1);
            Assert.IsFalse(condition.Check(_role), "同向输入 Delta 为 0，不应触发变化量条件");
        }
    }
}
