using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using Game.Logic.Character.Motion;
using Game.MAnimSystem;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public abstract class CharacterEntity : MonoBehaviour, ISkillEventHandler
    {
        private MAnimSystem.AnimComponent _animComponent;
        public Config.CharacterConfigAsset Config { get; private set; }

        public IInputProvider InputProvider { get; protected set; }
        public IMovementController MovementController { get; protected set; }
        public ICameraController CameraController { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }
        public TargetFinder TargetFinder { get; protected set; }

        public FSMSystem<CharacterEntity> StateMachine { get; private set; }
        public FSMSystem<CharacterEntity> Machine => StateMachine;

        public ActionPlayer ActionPlayer { get; private set; }
        public Action.Combo.CommandBuffer CommandBuffer { get; private set; }
        public Action.Combo.ActionController ActionController { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public CharacterRuntimeData RuntimeData { get; private set; }

        public event System.Action<string> OnSkillTimelineEvent;

        public ISkillComboWindowHandler SkillComboWindowHandler => ActionController;
        public ISkillMotionWindowHandler SkillMotionWindowHandler => MotionWindowHandler;

        private IInputCommandHandler CurrentInputHandler =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        private CharacterInputEventAdapter _inputEventAdapter;

        protected virtual void Awake()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Register(this);
            _animComponent = gameObject.AddComponent<AnimComponent>();
            InitRequiredComponents();

            ActionPlayer = new ActionPlayer(this);
            CommandBuffer = new Game.Logic.Action.Combo.CommandBuffer();
            ActionController = new Game.Logic.Action.Combo.ActionController(this);
            MotionWindowHandler = new SkillMotionWindowHandler(this);
            RuntimeData = new CharacterRuntimeData();
            _inputEventAdapter = new CharacterInputEventAdapter(() => CurrentInputHandler);
        }

        protected abstract void InitRequiredComponents();

        public void Init(Game.Logic.Character.Config.CharacterConfigAsset config)
        {
            Config = config;

            CameraController?.Init(this);
            MovementController?.Init(this);
            HitReactionModule?.Init(this);
        }

        private void Start()
        {
            if (Config == null)
            {
                return;
            }

            if (StateMachine == null)
            {
                FSMManager fsmMgr = FSMManager.Instance;
                if (fsmMgr != null)
                {
                    StateMachine = fsmMgr.CreateFSM<CharacterEntity>(this);
                    StateMachine.AddState(new CharacterGroundState());
                    StateMachine.AddState(new CharacterSkillState());
                    StateMachine.AddState(new CharacterEvadeState());
                    StateMachine.AddState(new CharacterActionBackswingState());
                    StateMachine.AddState(new CharacterHitStunState());
                    StateMachine.ChangeState<CharacterGroundState>();
                }
            }

            GameCameraManager.Instance?.SetTarget(transform);
            _inputEventAdapter?.Bind(InputProvider);
        }

        public void OnSkillEvent(string eventName, List<SkillEventParam> parameters)
        {
            OnSkillTimelineEvent?.Invoke(eventName);
        }

        private void Update()
        {
            ActionPlayer?.Tick(Time.deltaTime);
            ActionController?.Update(Time.deltaTime);
            RuntimeData?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Unregister(this);
            _inputEventAdapter?.Unbind(InputProvider);

            if (FSMManager.Instance != null && StateMachine != null)
            {
                FSMManager.Instance.DestroyFSM(StateMachine);
                StateMachine = null;
            }
        }
    }
}
