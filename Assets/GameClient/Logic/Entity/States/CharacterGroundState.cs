using Game.FSM;
using Game.Logic;

namespace Game.Logic
{
    public class PlayerLocomotionBlackboard
    {
        public bool IsFromDash;
        public bool IsShortJog;
    }

    public class CharacterGroundState : CharacterStateBase
    {
        public float JogSpeed = 5.0f;
        public float DashSpeed = 20.0f;

        public PlayerLocomotionBlackboard Blackboard { get; private set; } = new PlayerLocomotionBlackboard();

        public GroundIdleSubState IdleState { get; private set; }
        public GroundJogSubState JogState { get; private set; }
        public GroundDashSubState DashState { get; private set; }
        public GroundStopSubState StopState { get; private set; }

        public GroundSubState CurrentSubState { get; private set; }

        private IInputCommandHandler _defaultInputHandler;
        public override IInputCommandHandler InputHandler => CurrentSubState?.InputHandler ?? _defaultInputHandler;

        public RoleEntity HostEntity => Entity;
        public FSMSystem<RoleEntity> HostMachine => Machine;

        public CharacterGroundState()
        {
            IdleState = new GroundIdleSubState();
            JogState = new GroundJogSubState();
            DashState = new GroundDashSubState();
            StopState = new GroundStopSubState();
        }

        private ActionRuntimeData _actionData;

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _actionData = Entity.DataModule?.Get<ActionRuntimeData>();
            _defaultInputHandler = new DefaultInputCommandHandler(Entity);
            IdleState.Initialize(this);
            JogState.Initialize(this);
            DashState.Initialize(this);
            StopState.Initialize(this);
        }

        public override void OnEnter()
        {
            ActionState targetState = ActionState.Idle;
            if (_actionData != null)
            {
                targetState = _actionData.TargetGroundSubState;
                _actionData.TargetGroundSubState = ActionState.Idle; // 消费请求
            }

            if (targetState == ActionState.Dash)
            {
                ChangeSubState(DashState);
                return;
            }

            if (targetState == ActionState.Jog)
            {
                ChangeSubState(JogState);
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

        public bool ChangeSubState(GroundSubState newState)
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
