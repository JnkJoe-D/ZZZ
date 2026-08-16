using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public class RoleEntity : CharacterEntity
    {
        private CharacterInputEventAdapter _inputEventAdapter;
        protected bool _isInputBound;
        private IInputProvider _boundInputProvider;
        private IInputProvider _inputProvider;
        private readonly Dictionary<Renderer, bool> _rendererVisibleStates = new();
        private readonly Dictionary<Collider, bool> _colliderEnabledStates = new();

        private ICameraController _cameraController;
        private CharacterTeamContext _teamContext;

        public virtual IInputProvider InputProvider => TeamContext?.InputProvider ?? _inputProvider;
        public override ITargetFinder TargetFinder => TeamContext?.TargetFinder ?? base.TargetFinder;
        public virtual ICameraController CameraController => _cameraController;

        public FSMSystem<RoleEntity> StateMachine { get; private set; }
        public FSMSystem<RoleEntity> Machine => StateMachine;

        public CommandBuffer CommandBuffer { get; private set; }
        public ActionController ActionController { get; private set; }
        public bool IsControlActive { get; protected set; }
        public bool IsPresentationVisible { get; private set; } = true;
        public bool IsRuntimeInitialized { get; protected set; }

        protected virtual bool AutoBindInputOnStart => false;
        protected bool AutoAssignCameraOnStart => false;

        protected CharacterTeamContext TeamContext => _teamContext;

        private bool UsesSharedInputProvider =>
            _teamContext != null &&
            _teamContext.InputProvider != null &&
            ReferenceEquals(InputProvider, _teamContext.InputProvider);

        protected virtual IInputCommandHandler GetCurrentInputHandler() =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        protected override void Awake()
        {
            base.Awake();
            if (CommandBuffer == null) CommandBuffer = new Game.Logic.CommandBuffer();
            if (ActionController == null) ActionController = new Game.Logic.ActionController(this);
            if (_inputEventAdapter == null) _inputEventAdapter = new CharacterInputEventAdapter(() => GetCurrentInputHandler());
            CachePresentationState();
        }

        protected override void InitRequiredComponents()
        {
            CharacterMotor = GetComponent<CharacterMotor>();
            if (CharacterMotor == null) CharacterMotor = gameObject.AddComponent<CharacterMotor>();

            CharacterCameraController cameraController = GetComponent<CharacterCameraController>();
            if (cameraController == null) cameraController = gameObject.AddComponent<CharacterCameraController>();
            _cameraController = cameraController;

            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();

            CameraPointBinder cameraPointBinder = GetComponent<CameraPointBinder>();
            if (cameraPointBinder == null) cameraPointBinder = gameObject.AddComponent<CameraPointBinder>();
        }

        public override void Init(Game.Logic.CharacterConfigAsset config)
        {
            base.Init(config);
            CameraController?.Init(this);
            StatusModule?.Init(this, config);
            
            if (CommandBuffer == null) CommandBuffer = new Game.Logic.CommandBuffer();
            if (ActionController == null) ActionController = new Game.Logic.ActionController(this);
            if (_inputEventAdapter == null) _inputEventAdapter = new CharacterInputEventAdapter(() => GetCurrentInputHandler());
        }

        public virtual void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized || Config == null)
            {
                return;
            }

            FSMManager fsmMgr = FSMManager.Instance;
            if (fsmMgr != null)
            {
                StateMachine = fsmMgr.CreateFSM<RoleEntity>(this);
                StateMachine.AddState(new CharacterGroundState());
                StateMachine.AddState(new CharacterSkillState());
                StateMachine.AddState(new CharacterEvadeState());
                StateMachine.AddState(new CharacterHitStunState());
                StateMachine.AddState(new CharacterSwitchState());
            }

            if (Config.ActionRoot != null)
            {
                ActionController?.PlayAction(Config.ActionRoot);
            }
            else
            {
                StateMachine?.ChangeState<CharacterGroundState>();
            }

            IsRuntimeInitialized = true;
        }

        protected override void Start()
        {
            base.Start();
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

        protected override void Update()
        {
            base.Update();
            ActionController?.Update(Time.deltaTime);
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

            CameraController?.SetCameraActive(true);
            CameraController?.EnableInput(true);

            if (assignCameraTarget)
            {
                GameCameraManager.Instance?.SetTarget(transform);
            }
        }

        protected virtual void DeactivateControl()
        {
            UnbindInput();
            IsControlActive = false;

            CameraController?.EnableInput(false);

            if (!UsesSharedInputProvider && InputProvider is Behaviour inputBehaviour)
            {
                inputBehaviour.enabled = false;
            }
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindInput();

            if (FSMManager.Instance != null && StateMachine != null)
            {
                FSMManager.Instance.DestroyFSM(StateMachine);
                StateMachine = null;
            }
        }

        public override void OnActionTimelineEvent(string eventName, List<ATEventParam> parameters)
        {
            Game.Framework.EventCenter.Publish(new CharacterTimelineEvent
            {
                SourceEntity = this,
                EventName = eventName,
                Parameters = parameters
            });
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

        public void ResetSwitchState()
        {
            CommandBuffer?.Clear();
        }

        public void SetCameraRigActive(bool active)
        {
            CameraController?.SetCameraActive(active);
            CameraController?.EnableInput(active && IsControlActive);
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
            if (_isInputBound)
            {
                UnbindInput();
                _inputProvider = inputProvider;
                BindInput();
            }
            else
            {
                _inputProvider = inputProvider;
            }
        }

        private void CachePresentationState()
        {
            _rendererVisibleStates.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    _rendererVisibleStates[r] = r.enabled;
                }
            }

            _colliderEnabledStates.Clear();
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c != null)
                {
                    _colliderEnabledStates[c] = c.enabled;
                }
            }
        }
    }
}
