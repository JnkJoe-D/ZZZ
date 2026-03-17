using System.Collections.Generic;
using Game.Camera;
using Game.FSM;
using Game.Input;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 角色实体，组件管理，事件转发，上下文
    /// </summary>
    public abstract class CharacterEntity : MonoBehaviour, SkillEditor.ISkillEventHandler
    {
        /// <summary>
        ///  --- 配置资产 ---
        /// </summary>
        public Config.CharacterConfigAsset Config { get; private set; }
        // --- MonoBehaviour组件 ---
        public IInputProvider InputProvider { get; protected set; } // 输入
        public IMovementController MovementController { get; protected set; } // 移动转向
        public ICameraController CameraController { get; protected set; } // 相机
        /// <summary>
        ///  --- 状态机 ---
        /// </summary>
        public FSMSystem<CharacterEntity> StateMachine { get; private set; }
        public FSMSystem<CharacterEntity> Machine => StateMachine;
        /// --- C#类 ---
        public ActionPlayer ActionPlayer { get; private set; } // 时间轴播放
        public Action.Combo.CommandBuffer CommandBuffer { get; private set; } // 指令缓存
        public Action.Combo.ComboController ComboController { get; private set; } // 指令校验
        public CharacterRuntimeData RuntimeData { get; private set; } // 角色运行时数据
        // --- 时间轴事件 ---
        public event System.Action<string> OnSkillTimelineEvent;
        // 转发时间轴输入窗口处理
        public ISkillComboWindowHandler SkillComboWindowHandler => ComboController;
        // --- 当前状态的输入处理器 ---
        private IInputCommandHandler CurrentInputHandler =>
            (StateMachine?.CurrentState as CharacterStateBase)?.InputHandler ?? CharacterStateBase.InputHandlerStatic;

        protected virtual void Awake()
        {
            Game.AI.BehaviorTreeCharacterRegistry.Register(this);
            InitRequiredComponents();
            ActionPlayer = new ActionPlayer(this);
            CommandBuffer = new Game.Logic.Action.Combo.CommandBuffer();
            ComboController = new Game.Logic.Action.Combo.ComboController(this);
            RuntimeData = new CharacterRuntimeData();
        }

        /// <summary>
        /// 初始化必要的组件。子类通过 AddComponent 或其他逻辑在此处注入具体实现。
        /// </summary>
        protected abstract void InitRequiredComponents();

        public void Init(Game.Logic.Character.Config.CharacterConfigAsset config)
        {
            Config = config;

            CameraController?.Init(this);
            MovementController?.Init(this);
        }

        private void Start()
        {
            if (Config == null) return;

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
                    StateMachine.AddState(new CharacterHitStunState());

                    StateMachine.ChangeState<CharacterGroundState>();
                }
            }

            GameCameraManager.Instance?.SetTarget(transform);
            BindInputProviderEvents(InputProvider);
        }
        // 输入指令转发
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
        private void Update()
        {
            ActionPlayer?.Tick(Time.deltaTime);
            ComboController?.Update(Time.deltaTime);
            RuntimeData?.Update(Time.deltaTime);
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
