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
        private ICameraController _cameraController;
        private CharacterTeamContext _teamContext;
        private FSMSystem<CharacterEntity> _stateMachine;

        public override IInputProvider InputProvider => TeamContext?.InputProvider ?? base.InputProvider;
        public override ITargetFinder TargetFinder => TeamContext?.TargetFinder ?? base.TargetFinder;
        public override ICameraController CameraController => _cameraController;

        public override FSMSystem<CharacterEntity> StateMachine => _stateMachine;

        protected override bool AutoBindInputOnStart => false;
        protected bool AutoAssignCameraOnStart => false;

        public override CharacterTeamContext TeamContext => _teamContext;

        private bool UsesSharedInputProvider =>
            _teamContext != null &&
            _teamContext.InputProvider != null &&
            ReferenceEquals(InputProvider, _teamContext.InputProvider);

        protected override IInputCommandHandler GetCurrentInputHandler() =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        protected override void InitRequiredComponents()
        {
            MovementController = GetComponent<MovementController>();
            if (MovementController == null) MovementController = gameObject.AddComponent<MovementController>();

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
            StatusModule?.Init(this, config);
            CameraController?.Init(this);
        }

        public override void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized || Config == null)
            {
                return;
            }

            FSMManager fsmMgr = FSMManager.Instance;
            if (fsmMgr != null)
            {
                _stateMachine = fsmMgr.CreateFSM<CharacterEntity>(this);
                _stateMachine.AddState(new CharacterGroundState());
                _stateMachine.AddState(new CharacterSkillState());
                _stateMachine.AddState(new CharacterEvadeState());
                _stateMachine.AddState(new CharacterHitStunState());
                _stateMachine.AddState(new CharacterSwitchState());
            }

            if (Config.ActionRoot != null)
            {
                ActionController.PlayAction(Config.ActionRoot);
            }
            else
            {
                _stateMachine?.ChangeState<CharacterGroundState>();
            }

            IsRuntimeInitialized = true;
        }

        protected override void Start()
        {
            base.Start();

            if (AutoAssignCameraOnStart)
            {
                GameCameraManager.Instance?.SetTarget(transform);
                CameraController?.EnableInput(true);
                CameraController?.SetCameraActive(true);
            }
        }

        protected override void ActivateControl(bool assignCameraTarget)
        {
            base.ActivateControl(assignCameraTarget);

            CameraController?.SetCameraActive(true);
            CameraController?.EnableInput(true);

            if (assignCameraTarget)
            {
                GameCameraManager.Instance?.SetTarget(transform);
            }
        }

        protected override void DeactivateControl()
        {
            base.DeactivateControl();

            CameraController?.EnableInput(false);

            if (!UsesSharedInputProvider && InputProvider is Behaviour inputBehaviour)
            {
                inputBehaviour.enabled = false;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (FSMManager.Instance != null && StateMachine != null)
            {
                FSMManager.Instance.DestroyFSM(StateMachine);
                _stateMachine = null;
            }
        }

        public override void OnSkillEvent(string eventName, List<ATEventParam> parameters)
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
    }
}
