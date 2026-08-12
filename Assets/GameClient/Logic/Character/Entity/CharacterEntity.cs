using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using Game.MAnimSystem;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public abstract class CharacterEntity : MonoBehaviour, IEventHandler
    {
        private AnimComponent _animComponent;
        private CharacterInputEventAdapter _inputEventAdapter;
        protected bool _isInputBound;
        private IInputProvider _boundInputProvider;
        private IInputProvider _inputProvider;
        private ITargetFinder _targetFinder;
        private readonly Dictionary<Renderer, bool> _rendererVisibleStates = new();
        private readonly Dictionary<Collider, bool> _colliderEnabledStates = new();

        public virtual IInputProvider InputProvider => _inputProvider;
        public virtual ITargetFinder TargetFinder => _targetFinder;
        public virtual ICameraController CameraController => null;
        public virtual FSMSystem<CharacterEntity> StateMachine => null;
        public FSMSystem<CharacterEntity> Machine => StateMachine;
        public virtual CharacterTeamContext TeamContext => null;

        public CharacterConfigAsset Config { get; private set; }
        public IMovementController MovementController { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }
        public FootIKModule FootIKModule { get; protected set; }

        public ActionPlayer ActionPlayer { get; private set; }
        public CommandBuffer CommandBuffer { get; private set; }
        public ActionController ActionController { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public CharacterRuntimeData RuntimeData { get; private set; }
        public StatusModule StatusModule { get; private set; }

        public bool IsRuntimeInitialized { get; protected set; }
        public bool IsControlActive { get; protected set; }
        public bool IsPresentationVisible { get; private set; } = true;

        public IRouteWindowHandler SkillComboWindowHandler => ActionController;
        public IMotionWindowHandler SkillMotionWindowHandler => MotionWindowHandler;

        protected virtual IInputCommandHandler GetCurrentInputHandler() => null;

        protected virtual bool AutoBindInputOnStart => true;

        protected virtual void Awake()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Register(this);
            _animComponent = gameObject.AddComponent<AnimComponent>();
            InitRequiredComponents();

            if (ActionPlayer == null) ActionPlayer = new ActionPlayer(this);
            if (CommandBuffer == null) CommandBuffer = new Game.Logic.CommandBuffer();
            if (ActionController == null) ActionController = new Game.Logic.ActionController(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new SkillMotionWindowHandler(this);
            if (RuntimeData == null) RuntimeData = new CharacterRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();
            if (_inputEventAdapter == null) _inputEventAdapter = new CharacterInputEventAdapter(() => GetCurrentInputHandler());
            CachePresentationState();
        }

        protected abstract void InitRequiredComponents();

        public virtual void Init(Game.Logic.CharacterConfigAsset config)
        {
            Config = config;

            if (ActionPlayer == null) ActionPlayer = new ActionPlayer(this);
            if (CommandBuffer == null) CommandBuffer = new Game.Logic.CommandBuffer();
            if (ActionController == null) ActionController = new Game.Logic.ActionController(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new SkillMotionWindowHandler(this);
            if (RuntimeData == null) RuntimeData = new CharacterRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();
            if (_inputEventAdapter == null) _inputEventAdapter = new CharacterInputEventAdapter(() => GetCurrentInputHandler());

            MovementController?.Init(this);
            HitReactionModule?.Init(this);
            FootIKModule?.Init(this);
            
        }

        public virtual void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized || Config == null)
            {
                return;
            }

            if (Config.ActionRoot != null)
            {
                ActionPlayer?.PlayAction(Config.ActionRoot);
            }

            IsRuntimeInitialized = true;
        }

        public virtual void SetControlActive(bool active, bool assignCameraTarget = true)
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

        public float GetCharcterRadius()
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                return cc.radius + cc.skinWidth;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                return capsule.radius;
            }

            return 0.5f; // 默认值
        }

        protected virtual void Start()
        {
            EnsureRuntimeInitialized();

            if (AutoBindInputOnStart)
            {
                BindInput();
                IsControlActive = true;
            }
        }

        public virtual void OnSkillEvent(string eventName, List<ATEventParam> parameters)
        {
        }

        protected virtual void Update()
        {
            ActionController?.Update(Time.deltaTime);
            ActionPlayer?.Tick(Time.deltaTime);
            RuntimeData?.Update(Time.deltaTime);
            StatusModule?.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Unregister(this);
            UnbindInput();
            StatusModule?.Clear();
            Game.Logic.ActionManager.Instance?.RemoveCache(this);
        }

        protected virtual void ActivateControl(bool assignCameraTarget)
        {
            EnsureRuntimeInitialized();

            if (InputProvider is Behaviour inputBehaviour)
            {
                inputBehaviour.enabled = true;
            }

            BindInput();
            IsControlActive = true;
        }

        protected virtual void DeactivateControl()
        {
            UnbindInput();
            IsControlActive = false;
        }

        protected void BindInput()
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

        protected void UnbindInput()
        {
            if (!_isInputBound)
            {
                return;
            }

            _inputEventAdapter?.Unbind(_boundInputProvider);
            _boundInputProvider = null;
            _isInputBound = false;
        }

        protected void SetInputProvider(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        protected void SetTargetFinder(ITargetFinder targetFinder)
        {
            _targetFinder = targetFinder;
        }

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
