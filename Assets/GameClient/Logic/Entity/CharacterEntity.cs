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

        public virtual ITargetFinder TargetFinder { get; protected set; }

        /// <summary>
        /// 当前动作/战斗上下文关联的目标实体（如招架黄光攻击者、弹刀反击目标）。
        /// 优先级高于常规 TargetFinder。
        /// </summary>
        public CharacterEntity CombatContextTarget { get; set; }

        public void SetCombatContextTarget(CharacterEntity target)
        {
            CombatContextTarget = target;
        }

        public void ClearCombatContextTarget()
        {
            CombatContextTarget = null;
        }

        /// <summary>
        /// 获取当前生效的目标 Transform：优先返回存活的 CombatContextTarget，无上下文目标时回退至 TargetFinder。
        /// </summary>
        public Transform GetEffectiveTarget()
        {
            if (CombatContextTarget != null && CombatContextTarget.gameObject.activeInHierarchy && !CombatContextTarget.IsDead)
            {
                return CombatContextTarget.transform;
            }
            return TargetFinder?.GetTarget();
        }

        public CharacterConfigAsset Config { get; private set; }
        public ICharacterMotor CharacterMotor { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }
        public FootIKModule FootIKModule { get; protected set; }
        public ILifecycleModule LifecycleModule { get; protected set; }
        public bool IsDead => LifecycleModule != null && LifecycleModule.IsDead;
        public virtual bool IsPresentationVisible { get; protected set; } = true;

        public virtual ActionController ActionController { get; protected set; }
        public CommandBuffer CommandBuffer { get; protected set; }
        public ActionPlayer ActionPlayer { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public EntityDataModule DataModule { get; } = new EntityDataModule();
        public StatusModule StatusModule { get; private set; }
        public virtual IAttributeResolver AttributeResolver { get; protected set; }


        protected virtual void Awake()
        {
            if (gameObject.GetComponent<AnimComponent>() == null)
            {
                gameObject.AddComponent<AnimComponent>();
            }
            InitRequiredComponents();

            if (LifecycleModule == null)
            {
                var lifecycle = GetComponent<EntityLifecycleModule>();
                if (lifecycle == null) lifecycle = gameObject.AddComponent<EntityLifecycleModule>();
                LifecycleModule = lifecycle;
                lifecycle.Init(this);
            }

            if (ActionPlayer == null) ActionPlayer = new ActionPlayer(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new SkillMotionWindowHandler(this);
            DataModule[typeof(ActionRuntimeData)] ??= new ActionRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();
            DataModule[typeof(ParryRuntimeData)] ??= new ParryRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();
            if (AttributeResolver == null) AttributeResolver = new EntityAttributeResolver(this);
        }

        protected abstract void InitRequiredComponents();

        public virtual void Init(Game.Logic.CharacterConfigAsset config)
        {
            Config = config;
            
            LifecycleModule?.Init(this);
            CharacterMotor?.Init(this);
            HitReactionModule?.Init(this);
            FootIKModule?.Init(this);
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
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick += OnLogicTick;
            }
        }

        public virtual void OnActionTimelineEvent(string eventName, List<ATEventParam> parameters)
        {
        }

        protected virtual void OnLogicTick(float logicDeltaTime)
        {
            ActionPlayer?.Tick(logicDeltaTime);
            StatusModule?.Tick(logicDeltaTime);
        }

        protected virtual void Update()
        {
        }

        protected virtual void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick -= OnLogicTick;
            }
            StatusModule?.Clear();
            Game.Logic.ActionManager.Instance?.RemoveCache(this);
        }
    }
}
