using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// Player-controlled character entity that coordinates input, movement, FSM, and action playback.
    /// </summary>
    public class CharacterEntity : MonoBehaviour, SkillEditor.ISkillEventHandler, SkillEditor.ISkillComboWindowHandler
    {
        private bool _hasStarted;

        public IInputProvider InputProvider { get; private set; }
        public IMovementController MovementController { get; private set; }
        public ICameraController CameraController { get; private set; }

        public FSMSystem<CharacterEntity> StateMachine { get; private set; }
        public FSMSystem<CharacterEntity> Machine => StateMachine;

        public ActionPlayer ActionPlayer { get; private set; }
        public Game.Logic.Character.Config.CharacterConfigAsset Config { get; private set; }

        public event System.Action<string> OnSkillTimelineEvent;
        public Game.Logic.Action.Config.ActionConfigAsset NextActionToCast { get; set; }

        public Game.Logic.Action.Combo.CommandBuffer CommandBuffer { get; private set; }
        public Game.Logic.Action.Combo.ComboController ComboController { get; private set; }

        public class ComboWindowData
        {
            public string Tag;
            public SkillEditor.ComboWindowType Type;
        }

        public List<ComboWindowData> ActiveComboWindows { get; private set; } = new List<ComboWindowData>();

        public event System.Action<string, SkillEditor.ComboWindowType> OnComboWindowEnterEvent;
        public event System.Action<string, SkillEditor.ComboWindowType> OnComboWindowExitEvent;

        public bool ForceDashNextFrame { get; set; }
        public float EvadeTimer { get; set; }
        public int EvadeCount { get; set; }

        private IInputCommandHandler CurrentInputHandler =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        public bool HasComboTag(string tag)
        {
            return ActiveComboWindows.Exists(x => x.Tag == tag);
        }

        public bool HasWindowType(SkillEditor.ComboWindowType type)
        {
            return ActiveComboWindows.Exists(x => x.Type == type);
        }

        public bool CanEvade()
        {
            if (Config == null) return false;
            if (EvadeCount >= Config.evadeLimitedTimes && EvadeTimer > 0f) return false;
            return true;
        }

        public void RecordEvade()
        {
            if (Config == null) return;
            EvadeCount++;
            EvadeTimer = Config.evadeCoolDown;
        }

        private void Awake()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Register(this);
            InputProvider = GetComponent<IInputProvider>();
            MovementController = GetComponent<IMovementController>();
            CameraController = GetComponent<ICameraController>();

            if (InputProvider == null || MovementController == null)
            {
                Debug.LogWarning($"[CharacterEntity] {gameObject.name} is missing required control components.");
            }
        }

        public void Init(Game.Logic.Character.Config.CharacterConfigAsset config)
        {
            Config = config;
            Debug.Log($"[CharacterEntity] Config Injected: Role={config.RoleName}");
        }

        private void Start()
        {
            _hasStarted = true;

            if (Config == null)
            {
                Debug.LogWarning("[CharacterEntity] Config was not injected before Start.");
            }

            if (StateMachine == null)
            {
                var fsmMgr = FSMManager.Instance;
                if (fsmMgr != null)
                {
                    StateMachine = fsmMgr.CreateFSM<CharacterEntity>(this);
                    StateMachine.AddState(new CharacterGroundState());
                    StateMachine.AddState(new CharacterAirborneState());
                    StateMachine.AddState(new CharacterSkillState());
                    StateMachine.AddState(new CharacterEvadeState());
                    StateMachine.AddState(new CharacterActionBackswingState());

                    ActionPlayer = new ActionPlayer(this);
                    CommandBuffer = new Game.Logic.Action.Combo.CommandBuffer();
                    ComboController = new Game.Logic.Action.Combo.ComboController(this);

                    StateMachine.ChangeState<CharacterGroundState>();
                }
            }

            GameCameraManager.Instance?.SetTarget(transform);
            BindInputProviderEvents(InputProvider);
        }

        public void SetInputProvider(IInputProvider provider)
        {
            if (ReferenceEquals(InputProvider, provider)) return;

            if (_hasStarted) UnbindInputProviderEvents(InputProvider);
            InputProvider = provider;
            if (_hasStarted) BindInputProviderEvents(InputProvider);
        }

        private void OnBasicAttackStarted() => CurrentInputHandler.OnBasicAttackStarted();
        private void OnBasicAttackCanceled() => CurrentInputHandler.OnBasicAttackCanceled();
        private void OnBasicAttackHoldStart() => CurrentInputHandler.OnBasicAttackHoldStart();
        private void OnBasicAttackHold() => CurrentInputHandler.OnBasicAttackHold();
        private void OnBasicAttackHoldCancel() => CurrentInputHandler.OnBasicAttackHoldCancel();
        private void OnSpecialAttack() => CurrentInputHandler.OnSpecialAttack();
        private void OnUltimateAttack() => CurrentInputHandler.OnUltimate();
        private void OnEvadeFront() => CurrentInputHandler.OnEvadeFront();
        private void OnEvadeBack() => CurrentInputHandler.OnEvadeBack();

        public void OnSkillEvent(string eventName, List<SkillEditor.SkillEventParam> parameters)
        {
            OnSkillTimelineEvent?.Invoke(eventName);
        }

        public void OnComboWindowEnter(string comboTag, SkillEditor.ComboWindowType windowType)
        {
            ActiveComboWindows.Add(new ComboWindowData { Tag = comboTag, Type = windowType });
            OnComboWindowEnterEvent?.Invoke(comboTag, windowType);
            ComboController?.OnWindowEnter(comboTag, windowType);
        }

        public void OnComboWindowExit(string comboTag, SkillEditor.ComboWindowType windowType)
        {
            ActiveComboWindows.RemoveAll(x => x.Tag == comboTag && x.Type == windowType);
            OnComboWindowExitEvent?.Invoke(comboTag, windowType);
            ComboController?.OnWindowExit(comboTag, windowType);
        }

        private void Update()
        {
            ActionPlayer?.Tick(Time.deltaTime);
            ComboController?.Update(Time.deltaTime);

            if (EvadeTimer > 0f)
            {
                EvadeTimer -= Time.deltaTime;
                if (EvadeTimer <= 0f)
                {
                    EvadeCount = 0;
                    EvadeTimer = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Unregister(this);
            UnbindInputProviderEvents(InputProvider);

            if (FSMManager.Instance != null && StateMachine != null)
            {
                FSMManager.Instance.DestroyFSM(StateMachine);
                StateMachine = null;
            }
        }

        private void BindInputProviderEvents(IInputProvider provider)
        {
            if (provider == null) return;
            provider.OnBasicAttackStarted += OnBasicAttackStarted;
            provider.OnBasicAttackCanceled += OnBasicAttackCanceled;
            provider.OnBasicAttackHoldStart += OnBasicAttackHoldStart;
            provider.OnBasicAttackHold += OnBasicAttackHold;
            provider.OnBasicAttackHoldCancel += OnBasicAttackHoldCancel;
            provider.OnSpecialAttack += OnSpecialAttack;
            provider.OnUltimate += OnUltimateAttack;
            provider.OnEvadeFrontStarted += OnEvadeFront;
            provider.OnEvadeBackStarted += OnEvadeBack;
        }

        private void UnbindInputProviderEvents(IInputProvider provider)
        {
            if (provider == null) return;
            provider.OnBasicAttackStarted -= OnBasicAttackStarted;
            provider.OnBasicAttackCanceled -= OnBasicAttackCanceled;
            provider.OnBasicAttackHoldStart -= OnBasicAttackHoldStart;
            provider.OnBasicAttackHold -= OnBasicAttackHold;
            provider.OnBasicAttackHoldCancel -= OnBasicAttackHoldCancel;
            provider.OnSpecialAttack -= OnSpecialAttack;
            provider.OnUltimate -= OnUltimateAttack;
            provider.OnEvadeFrontStarted -= OnEvadeFront;
            provider.OnEvadeBackStarted -= OnEvadeBack;
        }
    }
}
