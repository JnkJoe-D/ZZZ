using Game.Framework;

namespace Game.GamePlay
{
    public class PlayerLocomotionBlackboard
    {
        public bool IsFromDash;
        public bool IsShortJog;
    }

    public class RoleGroundState : RoleStateBase
    {
        public PlayerLocomotionBlackboard Blackboard { get; private set; } = new PlayerLocomotionBlackboard();

        public RoleIdleSubState IdleState { get; private set; }
        public RoleWalkSubState WalkState { get; private set; }
        public RoleRunSubState RunState { get; private set; }
        public RoleStopSubState StopState { get; private set; }

        public RoleSubState CurrentSubState { get; private set; }

        private IActionCommandHandler _defaultInputHandler;
        public override IActionCommandHandler InputHandler => CurrentSubState?.InputHandler ?? _defaultInputHandler;

        public RoleEntity HostEntity => Entity;
        public FSMSystem<RoleEntity> HostMachine => Machine;

        public RoleGroundState()
        {
            IdleState = new RoleIdleSubState();
            WalkState = new RoleWalkSubState();
            RunState = new RoleRunSubState();
            StopState = new RoleStopSubState();
        }

        private ActionRuntimeData _actionData;

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _actionData = Entity.DataModule?.Get<ActionRuntimeData>();
            _defaultInputHandler = new DefaultInputCommandHandler(Entity);
            IdleState.Initialize(this);
            WalkState.Initialize(this);
            RunState.Initialize(this);
            StopState.Initialize(this);
        }

        public override void OnEnter()
        {
            ActionState targetState = ActionState.Idle;
            if (_actionData != null)
            {
                targetState = _actionData.TargetGroundSubState;
                _actionData.Set(nameof(_actionData.TargetGroundSubState), ActionState.Idle); // 消费请求
            }

            if (targetState == ActionState.Run)
            {
                ChangeSubState(RunState);
                return;
            }

            if (targetState == ActionState.Walk)
            {
                ChangeSubState(WalkState);
                return;
            }

            if (targetState == ActionState.Stop)
            {
                ChangeSubState(StopState);
                return;
            }

            ChangeSubState(IdleState);
        }

        public override void OnUpdate(float deltaTime)
        {
            CurrentSubState?.OnUpdate(deltaTime);
        }

        public override void OnExit()
        {
            CurrentSubState?.OnExit();
            CurrentSubState = null;
        }

        public bool ChangeSubState(RoleSubState newState)
        {
            if (CurrentSubState == newState)
            {
                return false;
            }

            if (CurrentSubState != null && !CurrentSubState.CanExit())
            {
                return false;
            }

            if (newState != null && !newState.CanEnter())
            {
                return false;
            }

            CurrentSubState?.OnExit();
            CurrentSubState = newState;
            CurrentSubState?.OnEnter();

            return true;
        }
    }
}
