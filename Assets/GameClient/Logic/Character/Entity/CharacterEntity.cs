using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using Game.MAnimSystem;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public abstract class CharacterEntity : MonoBehaviour, ISkillEventHandler
    {
        private AnimComponent _animComponent;
        private CharacterInputEventAdapter _inputEventAdapter;
        private bool _isInputBound;
        private IInputProvider _boundInputProvider;
        private IInputProvider _inputProvider;
        private TargetFinder _targetFinder;
        private ICameraController _cameraController;
        private CharacterTeamContext _teamContext;
        private readonly Dictionary<Renderer, bool> _rendererVisibleStates = new();
        private readonly Dictionary<Collider, bool> _colliderEnabledStates = new();

        public virtual IInputProvider InputProvider => _inputProvider;
        public virtual TargetFinder TargetFinder => _targetFinder;
        public virtual ICameraController CameraController => _cameraController;

        public CharacterConfigAsset Config { get; private set; }
        public IMovementController MovementController { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }


        public FSMSystem<CharacterEntity> StateMachine { get; private set; }
        public FSMSystem<CharacterEntity> Machine => StateMachine;

        public ActionPlayer ActionPlayer { get; private set; }
        public CommandBuffer CommandBuffer { get; private set; }
        public ActionController ActionController { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public CharacterRuntimeData RuntimeData { get; private set; }

        public bool IsRuntimeInitialized { get; private set; }
        public bool IsControlActive { get; private set; }
        public bool IsPresentationVisible { get; private set; } = true;

        public ISkillComboWindowHandler SkillComboWindowHandler => ActionController;
        public ISkillMotionWindowHandler SkillMotionWindowHandler => MotionWindowHandler;

        private IInputCommandHandler CurrentInputHandler =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        protected virtual bool AutoBindInputOnStart => true;
        protected virtual bool AutoAssignCameraOnStart => false;

        protected virtual void Awake()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Register(this);
            _animComponent = gameObject.AddComponent<AnimComponent>();
            InitRequiredComponents();

            ActionPlayer = new ActionPlayer(this);
            CommandBuffer = new Game.Logic.CommandBuffer();
            ActionController = new Game.Logic.ActionController(this);
            MotionWindowHandler = new SkillMotionWindowHandler(this);
            RuntimeData = new CharacterRuntimeData();
            _inputEventAdapter = new CharacterInputEventAdapter(() => CurrentInputHandler);
            CachePresentationState();
        }

        protected abstract void InitRequiredComponents();

        public void Init(Game.Logic.CharacterConfigAsset config)
        {
            Config = config;

            CameraController?.Init(this);
            MovementController?.Init(this);
            HitReactionModule?.Init(this);
        }

        public void AssignTeamContext(CharacterTeamContext teamContext)
        {
            IInputProvider previousProvider = InputProvider;
            bool wasInputBound = _isInputBound;
            if (wasInputBound)
            {
                UnbindInput();
            }

            _teamContext = teamContext;

            IInputProvider currentProvider = InputProvider;
            if (!ReferenceEquals(previousProvider, currentProvider))
            {
                DisableReplacedInputProvider(previousProvider, currentProvider);
            }

            if (wasInputBound)
            {
                BindInput();
            }
        }

        public void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized || Config == null)
            {
                return;
            }

            FSMManager fsmMgr = FSMManager.Instance;
            if (fsmMgr == null)
            {
                return;
            }

            StateMachine = fsmMgr.CreateFSM<CharacterEntity>(this);
            StateMachine.AddState(new CharacterGroundState());
            StateMachine.AddState(new CharacterSkillState());
            StateMachine.AddState(new CharacterEvadeState());
            StateMachine.AddState(new CharacterHitStunState());
            StateMachine.AddState(new CharacterSwitchState());

            if (Config.ActionRoot != null)
            {
                ActionController.PlayAction(Config.ActionRoot);
            }
            else
            {
                StateMachine.ChangeState<CharacterGroundState>();
            }

            IsRuntimeInitialized = true;
        }

        public void SetControlActive(bool active, bool assignCameraTarget = true)
        {
            if (active)
            {
                ActivateControl(assignCameraTarget);
            }
            else
            {
                DeactivateControl();
            }
        }

        public void SetCameraRigActive(bool active)
        {
            CameraController?.SetCameraActive(active);
            CameraController?.EnableInput(active && IsControlActive);
        }

        public void ResetSwitchState()
        {
            CommandBuffer?.Clear();
        }

        public void SetPresentationVisible(bool visible)
        {
            IsPresentationVisible = visible;

            foreach (KeyValuePair<Renderer, bool> pair in _rendererVisibleStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = visible && pair.Value;
                }
            }
        }
        public void SetColliderActive(bool active)
        {
            LayerMask excludeMask = 0;
            if (!active)
            {
                excludeMask = LayerMask.GetMask("LocalRole", "Character", "CharHit");
            }

            foreach (KeyValuePair<Collider, bool> pair in _colliderEnabledStates)
            {
                if (pair.Key != null)
                {
                    // 保持碰撞体自身的启用状态与其初始状态一致
                    pair.Key.enabled = pair.Value;
                    // 设置排除层级
                    pair.Key.excludeLayers = excludeMask;
                }
            }
        }

        private void Start()
        {
            EnsureRuntimeInitialized();

            if (AutoBindInputOnStart)
            {
                BindInput();
                IsControlActive = true;
            }

            if (AutoAssignCameraOnStart)
            {
                GameCameraManager.Instance?.SetTarget(transform);
                CameraController?.EnableInput(true);
                CameraController?.SetCameraActive(true);
            }
        }

        public void OnSkillEvent(string eventName, List<SkillEventParam> parameters)
        {
            if (this is RoleEntity roleEntity)
            {
                Game.Framework.EventCenter.Publish(new CharacterTimelineEvent
                {
                    SourceEntity = roleEntity,
                    EventName = eventName,
                    Parameters = parameters
                });
            }
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
            UnbindInput();
            Game.Logic.ActionManager.Instance?.RemoveCache(this);

            if (FSMManager.Instance != null && StateMachine != null)
            {
                FSMManager.Instance.DestroyFSM(StateMachine);
                StateMachine = null;
            }
        }

        private void ActivateControl(bool assignCameraTarget)
        {
            EnsureRuntimeInitialized();

            if (InputProvider is Behaviour inputBehaviour)
            {
                inputBehaviour.enabled = true;
            }

            BindInput();
            IsControlActive = true;

            CameraController?.SetCameraActive(true);
            CameraController?.EnableInput(true);

            if (assignCameraTarget)
            {
                GameCameraManager.Instance?.SetTarget(transform);
            }
        }

        private void DeactivateControl()
        {
            UnbindInput();
            IsControlActive = false;
            CameraController?.EnableInput(false);

            if (!UsesSharedInputProvider && InputProvider is Behaviour inputBehaviour)
            {
                inputBehaviour.enabled = false;
            }
        }

        private void BindInput()
        {
            IInputProvider provider = InputProvider;
            if (provider == null)
            {
                return;
            }

            if (_isInputBound)
            {
                if (ReferenceEquals(_boundInputProvider, provider))
                {
                    return;
                }

                _inputEventAdapter?.Unbind(_boundInputProvider);
                _isInputBound = false;
                _boundInputProvider = null;
            }

            _inputEventAdapter?.Bind(provider);
            _boundInputProvider = provider;
            _isInputBound = true;
        }

        private void UnbindInput()
        {
            if (!_isInputBound)
            {
                return;
            }

            _inputEventAdapter?.Unbind(_boundInputProvider);
            _boundInputProvider = null;
            _isInputBound = false;
        }

        private static void DisableReplacedInputProvider(IInputProvider previousProvider, IInputProvider currentProvider)
        {
            if (previousProvider == null || ReferenceEquals(previousProvider, currentProvider))
            {
                return;
            }

            if (previousProvider is Behaviour previousBehaviour)
            {
                previousBehaviour.enabled = false;
            }
        }

        protected void SetInputProvider(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        protected void SetTargetFinder(TargetFinder targetFinder)
        {
            _targetFinder = targetFinder;
        }

        protected void SetCameraController(ICameraController cameraController)
        {
            _cameraController = cameraController;
        }

        protected CharacterTeamContext TeamContext => _teamContext;

        private bool UsesSharedInputProvider =>
            _teamContext != null &&
            _teamContext.InputProvider != null &&
            ReferenceEquals(InputProvider, _teamContext.InputProvider);

        private void CachePresentationState()
        {
            _rendererVisibleStates.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && !_rendererVisibleStates.ContainsKey(renderer))
                {
                    _rendererVisibleStates.Add(renderer, renderer.enabled);
                }
            }

            _colliderEnabledStates.Clear();
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider != null && !_colliderEnabledStates.ContainsKey(collider))
                {
                    _colliderEnabledStates.Add(collider, collider.enabled);
                }
            }
        }
    }
}
