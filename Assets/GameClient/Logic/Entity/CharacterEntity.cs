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

        public CharacterConfigAsset Config { get; private set; }
        public ICharacterMotor CharacterMotor { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }
        public FootIKModule FootIKModule { get; protected set; }

        public virtual ActionController ActionController { get; protected set; }
        public CommandBuffer CommandBuffer { get; protected set; }
        public ActionPlayer ActionPlayer { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public EntityDataModule DataModule { get; } = new EntityDataModule();
        public StatusModule StatusModule { get; private set; }


        protected virtual void Awake()
        {
            if (gameObject.GetComponent<AnimComponent>() == null)
            {
                gameObject.AddComponent<AnimComponent>();
            }
            InitRequiredComponents();

            if (ActionPlayer == null) ActionPlayer = new ActionPlayer(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new SkillMotionWindowHandler(this);
            DataModule[typeof(ActionRuntimeData)] ??= new ActionRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();
            DataModule[typeof(ParryRuntimeData)] ??= new ParryRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();
        }

        protected abstract void InitRequiredComponents();

        public virtual void Init(Game.Logic.CharacterConfigAsset config)
        {
            Config = config;
            
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
        }

        public virtual void OnActionTimelineEvent(string eventName, List<ATEventParam> parameters)
        {
        }

        protected virtual void Update()
        {
            ActionPlayer?.Tick(Time.deltaTime);
            StatusModule?.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            StatusModule?.Clear();
            Game.Logic.ActionManager.Instance?.RemoveCache(this);
        }
    }
}
