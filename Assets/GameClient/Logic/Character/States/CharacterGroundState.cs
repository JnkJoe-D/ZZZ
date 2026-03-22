using Game.AI;
using Game.FSM;

namespace Game.Logic.Character
{
    public class PlayerLocomotionBlackboard
    {
        public bool IsFromDash;
    }

    public class CharacterGroundState : CharacterStateBase
    {
        public float JogSpeed = 5.0f;
        public float DashSpeed = 20.0f;

        public PlayerLocomotionBlackboard Blackboard { get; private set; } = new PlayerLocomotionBlackboard();

        public SubStates.GroundIdleSubState IdleState { get; private set; }
        public SubStates.GroundJogSubState JogState { get; private set; }
        public SubStates.GroundDashSubState DashState { get; private set; }
        public SubStates.GroundStopSubState StopState { get; private set; }

        public SubStates.GroundSubState CurrentSubState { get; private set; }

        private IInputCommandHandler _defaultInputHandler;
        public override IInputCommandHandler InputHandler => CurrentSubState?.InputHandler ?? _defaultInputHandler;

        public CharacterEntity HostEntity => Entity;
        public FSMSystem<CharacterEntity> HostMachine => Machine;

        public CharacterGroundState()
        {
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
            var provider = Entity.InputProvider;
            bool hasMovementInput = provider != null && provider.HasMovementInput();
            bool shouldEnterDash = hasMovementInput && Entity.RuntimeData.ForceDashNextFrame;

            Entity.RuntimeData.ConsumeDashContinuation();

            if (provider == null)
            {
                ChangeSubState(IdleState);
                return;
            }

            if (shouldEnterDash)
            {
                ChangeSubState(DashState);
                return;
            }

            if (hasMovementInput)
            {
                ChangeSubState(JogState);
            }
            else
            {
                ChangeSubState(IdleState);
            }
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

        public bool ChangeSubState(SubStates.GroundSubState newState)
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
