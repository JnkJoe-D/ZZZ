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
        public bool IsDead => LifecycleComponent != null && LifecycleComponent.IsDead;

        public virtual ActionController ActionController { get; protected set; }
        public CommandBuffer CommandBuffer { get; protected set; }
        public ActionPlayer ActionPlayer { get; private set; }
        public ATMotionWindowHandler MotionWindowHandler { get; private set; }
        public virtual ITargetFinder TargetFinder { get; protected set; }
        public EntityDataModule DataModule { get; } = new EntityDataModule();
        public StatusModule StatusModule { get; private set; }
        public virtual IAttributeResolver AttributeResolver { get; protected set; }

        /// <summary>实体专属层次化时钟叶子节点（单一真理源）</summary>
        public TimeClock Clock { get; private set; }

        protected virtual void Awake()
        {
            if (gameObject.GetComponent<AnimComponent>() == null)
            {
                gameObject.AddComponent<AnimComponent>();
            }
            InitRequiredComponents();

            if (LifecycleComponent == null)
            {
                var lifecycle = GetComponent<LifecycleComponent>();
                if (lifecycle == null) lifecycle = gameObject.AddComponent<LifecycleComponent>();
                LifecycleComponent = lifecycle;
                lifecycle.Init(this);
            }

            // 初始化实体专属时钟节点，并监听有效流速变更
            Clock = new TimeClock(gameObject.name);
            Clock.OnEffectiveScaleChanged += HandleClockEffectiveScaleChanged;

            DataModule[typeof(ActionRuntimeData)] ??= new ActionRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();
            DataModule[typeof(ParryRuntimeData)] ??= new ParryRuntimeData();

            if (ActionPlayer == null) ActionPlayer = EntityModuleFactory.Create<ActionPlayer>(this);
            if (StatusModule == null) StatusModule = EntityModuleFactory.Create<StatusModule>(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new ATMotionWindowHandler(this);
            if (AttributeResolver == null) AttributeResolver = new EntityAttributeResolver(this);
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

        public virtual void Init(Game.GamePlay.CharacterConfigAsset config)
        {
            Config = config;
            
            LifecycleComponent?.Init(this);
            MovementComponent?.Init(this);
            HitReactionComponent?.Init(this);
        }

        protected virtual void Start()
        {
        }

        public virtual void OnLogicTick(float logicDeltaTime)
        {
            // 1. 推进实体自身专属时钟节点（维护本地时间戳与 DeltaTime）
            Clock?.Advance(logicDeltaTime);

            // 2. 计算经实体层级时钟（子弹时间/顿帧）缩放后的有效逻辑步长
            float scaledDt = logicDeltaTime * (Clock != null ? Clock.EffectiveScale : 1.0f);

            // 3. 将缩放后的步长自顶向下单向传递给各领域子系统
            ActionController?.LogicTick(scaledDt);
            ActionPlayer?.LogicTick(scaledDt);
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
            ActionPlayer?.Dispose();
            StatusModule?.Clear();
        }
    }
}
