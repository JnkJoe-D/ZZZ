using Game.FSM;
using Game.AI;
using UnityEngine;

namespace Game.Logic.Character
{

    public class PlayerLocomotionBlackboard
    {
        public bool IsFromDash;
    }

    /// <summary>
    /// 地表宏观状态类（HFSM 分层状态机的父节点容器）
    /// 不再处理具体的跑停布尔值判断，而是将事件无条件地下发给其激活的微状态 (SubState)
    /// </summary>
    public class CharacterGroundState : CharacterStateBase
    {
        public float JogSpeed = 5.0f;
        public float DashSpeed = 20.0f;
        
        // --- 共享数据黑板 ---
        public PlayerLocomotionBlackboard Blackboard { get; private set; } = new PlayerLocomotionBlackboard();

        // --- HFSM 子状态实例 ---
        public SubStates.GroundIdleSubState IdleState { get; private set; }
        public SubStates.GroundJogSubState JogState { get; private set; }
        public SubStates.GroundDashSubState DashState { get; private set; }
        public SubStates.GroundStopSubState StopState { get; private set; }
        
        public SubStates.GroundSubState CurrentSubState { get; private set; }

        private IInputCommandHandler _defaultInputHandler;
        public override IInputCommandHandler InputHandler => CurrentSubState?.InputHandler ?? _defaultInputHandler;

        // --- 全局地表物理硬直锁 ---
        private float _moveLockTimer = 0f;
        public bool IsMoveLocked => _moveLockTimer > 0;

        // --- 暴露给 SubState 专用的只读基类成员上下文 ---
        public CharacterEntity HostEntity => Entity;
        public FSMSystem<CharacterEntity> HostMachine => Machine;

        public CharacterGroundState()
        {
            // 在构造期装配子微状态
            IdleState = new SubStates.GroundIdleSubState();
            JogState = new SubStates.GroundJogSubState();
            DashState = new SubStates.GroundDashSubState();
            StopState = new SubStates.GroundStopSubState();        
        }

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _defaultInputHandler = new DefaultInputCommandHandler(Entity);
            IdleState.Initialize(this);
            JogState.Initialize(this);
            DashState.Initialize(this);
            StopState.Initialize(this);
        }

        public override void OnEnter()
        {
            _moveLockTimer = 0f;
            
            var provider = Entity.InputProvider;
            
            // 进场时根据当前输入动态决定初始子状态，而不是死板地进入 Idle
            if (provider != null)
            {
                if (provider.HasMovementInput())
                {
                    if (Entity.ForceDashNextFrame || provider.GetActionState(Game.Input.InputActionType.Dash))
                    {
                        Entity.ForceDashNextFrame = false;
                        ChangeSubState(DashState);
                    }
                    else
                    {
                        ChangeSubState(JogState);
                    }
                }
                else
                {
                    ChangeSubState(IdleState);
                }
            }
            else
            {
                ChangeSubState(IdleState);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            // 刷新本地全局硬直
            if (_moveLockTimer > 0)
            {
                _moveLockTimer -= deltaTime;
            }

            // 更新当前的子业务微状态
            CurrentSubState?.OnUpdate(deltaTime);
        }

        public override void OnExit()
        {
            
            CurrentSubState?.OnExit();
            CurrentSubState = null;
        }

        // --- HFSM 核心提供给子状态的能力 ---

        public bool ChangeSubState(SubStates.GroundSubState newState)
        {
            if (CurrentSubState == newState) return false;

            // --- HFSM 微状态的准入/准出协商 ---
            if (CurrentSubState != null && !CurrentSubState.CanExit()) return false;
            if (newState != null && !newState.CanEnter()) return false;

            CurrentSubState?.OnExit();
            CurrentSubState = newState;
            CurrentSubState?.OnEnter();

            return true;
        }

        public void SetMoveLock(float duration)
        {
            _moveLockTimer = duration;
        }

        public void ClearMoveLock()
        {
            _moveLockTimer = 0f;
        }

        /// <summary>
        /// 提供给子类：将 2D 的手柄拉动或者 WASD 转换为考虑主相机的绝对世界朝向
        /// </summary>
        public Vector3 CalculateWorldDirection(Vector2 inputDir)
        {
            if (Entity.InputProvider is AIInputProvider aiInputProvider &&
                aiInputProvider.TryGetWorldMovementDirection(out Vector3 aiWorldDirection))
            {
                return aiWorldDirection.normalized;
            }

            if (Entity.CameraController != null)
            {
                Vector3 camForward = Entity.CameraController.GetForward();
                Vector3 camRight = Entity.CameraController.GetRight();
                return (camForward * inputDir.y + camRight * inputDir.x).normalized;
            }
            else
            {
                return new Vector3(inputDir.x, 0, inputDir.y).normalized;
            }
        }

    }
}
