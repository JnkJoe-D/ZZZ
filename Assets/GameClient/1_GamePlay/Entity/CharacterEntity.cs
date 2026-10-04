using System.Collections.Generic;
using MAnimSystem;
using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public abstract class CharacterEntity : MonoBehaviour, ILogicTickable
    {
        public CharacterConfigAsset Config { get; private set; }

        public IMovementComponent MovementComponent { get; protected set; }
        public HitReactionComponent HitReactionComponent { get; protected set; }
        public ILifecycleComponent LifecycleComponent { get; protected set; }

        public virtual ActionController ActionController { get; protected set; }
        public RouteArbitrator RouteArbitrator { get; protected set; }
        /// <summary>
        /// 动作底层播放器：归属 ActionController 统一调度与驱动，此处提供只读快捷转发，保障外部现有组件平滑兼容。
        /// </summary>
        public ActionPlayer ActionPlayer => ActionController?.ActionPlayer;
        public ATMotionWindowHandler MotionWindowHandler { get; private set; }
        public virtual ITargetFinder TargetFinder { get; protected set; }
        public EntityDataModule DataModule { get; } = new EntityDataModule();
        public StatusModule StatusModule { get; protected set; }
        public virtual IAttributeResolver AttributeResolver { get; protected set; }
        public bool IsRuntimeInitialized { get; protected set; }

        /// <summary>实体专属层次化时钟叶子节点（单一真理源）</summary>
        public TimeClock Clock { get; private set; }

        protected virtual void Awake()
        {
            // 阶段 0: 引擎就绪（底层先行）
            if (gameObject.GetComponent<AnimComponent>() == null)
            {
                gameObject.AddComponent<AnimComponent>();
            }

            // 初始化实体专属时钟节点，并监听有效流速变更
            Clock = new TimeClock(gameObject.name);
            Clock.OnEffectiveScaleChanged += HandleClockEffectiveScaleChanged;

            InitRequiredComponents();

            if (LifecycleComponent == null)
            {
                var lifecycle = GetComponent<LifecycleComponent>();
                if (lifecycle == null) lifecycle = gameObject.AddComponent<LifecycleComponent>();
                LifecycleComponent = lifecycle;
            }

            // 预置基础运行时状态数据容器（确保任何极早期只读访问安全）
            DataModule[typeof(ActionRuntimeData)] ??= new ActionRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();
            DataModule[typeof(ParryRuntimeData)] ??= new ParryRuntimeData();
            DataModule[typeof(LifecycleRuntimeData)] ??= new LifecycleRuntimeData();

            MotionWindowHandler = new ATMotionWindowHandler(this);
        }

        private void HandleClockEffectiveScaleChanged(float effectiveScale)
        {
            ActionPlayer?.SetExternalTimeScale(effectiveScale);
        }

        /// <summary>
        /// 供生成器或管理器（TeamCharacterSpawner / MonsterManager）进行时钟树装配
        /// </summary>
        public void AttachToClock(TimeClock parentClock)
        {
            if (parentClock != null && Clock != null)
            {
                parentClock.AddChild(Clock);
            }
        }

        /// <summary>
        /// 解除时钟树挂载，恢复为独立根节点
        /// </summary>
        public void DetachFromClock()
        {
            Clock?.Detach();
        }

        protected abstract void InitRequiredComponents();

        /// <summary>
        /// 实体标准化四阶段初始化管线（唯一真理入口）
        /// 严格遵循优先级：静态配置注入(Phase 1) -> 运行时状态容器(Phase 2) -> 底层引擎适配与表现装配(Phase 3) -> 领域控制器与状态机(Phase 4)
        /// </summary>
        public void Init(CharacterConfigAsset config)
        {
            if (config == null)
            {
                GLog.Error(LogTags.Combat, $"[Entity] {name} Init 失败: config 为 null！");
                return;
            }

            // 阶段 1: 静态配置数据注入 (最高优先级)
            SetupConfig(config);

            // 阶段 2: 运行时状态容器装配 (次高优先级)
            SetupRuntimeData();

            // 阶段 3: 底层引擎适配与表现组件装配 (第三优先级，底层先行)
            SetupComponents();

            // 阶段 4: 领域模块、控制器中枢与状态机 (第四优先级)
            SetupDomainModulesAndFSM();

            IsRuntimeInitialized = true;

            // 实体各子系统与状态机全部装配完毕后，启动默认根节点动作 (ActionRoot / 默认待机态)
            ActionController?.PlayRootAction();
        }

        protected virtual void SetupConfig(CharacterConfigAsset config)
        {
            Config = config;
        }

        protected virtual void SetupRuntimeData()
        {
            DataModule[typeof(ActionRuntimeData)] ??= new ActionRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();
            DataModule[typeof(ParryRuntimeData)] ??= new ParryRuntimeData();
            DataModule[typeof(LifecycleRuntimeData)] ??= new LifecycleRuntimeData();
        }

        protected virtual void SetupComponents()
        {
            LifecycleComponent?.Init(this);
            MovementComponent?.Init(this);
            HitReactionComponent?.Init(this);
            MotionWindowHandler ??= new ATMotionWindowHandler(this);
        }

        protected abstract void SetupDomainModulesAndFSM();

        protected virtual void Start()
        {
        }

        public virtual void OnLogicTick(float logicDeltaTime)
        {
            // 1. 推进实体自身专属时钟节点（维护本地时间戳与 DeltaTime）
            Clock?.Advance(logicDeltaTime);

            // 2. 计算经实体层级时钟（子弹时间/顿帧）缩放后的有效逻辑步长
            float scaledDt = logicDeltaTime * (Clock != null ? Clock.EffectiveScale : 1.0f);

            // 3. 将缩放后的步长自顶向下单向传递给各领域子系统（ActionController 内部自顶向下驱动 ActionPlayer）
            ActionController?.LogicTick(scaledDt);
            StatusModule?.LogicTick(scaledDt);
            OnSubLogicTick(scaledDt);
        }

        protected virtual void OnSubLogicTick(float scaledDeltaTime)
        {
        }

        protected virtual void Update()
        {
        }

        protected virtual void OnDestroy()
        {
            Clock?.Detach();
            ActionController?.Dispose();
            StatusModule?.Clear();
        }
    }
}
