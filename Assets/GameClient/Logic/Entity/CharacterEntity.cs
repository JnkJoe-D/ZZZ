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
        protected ITargetFinder _targetFinder;

        public virtual ITargetFinder TargetFinder => _targetFinder;

        public CharacterConfigAsset Config { get; private set; }
        public ICharacterMotor CharacterMotor { get; protected set; }
        public HitReactionModule HitReactionModule { get; protected set; }
        public FootIKModule FootIKModule { get; protected set; }

        public ActionPlayer ActionPlayer { get; private set; }
        public SkillMotionWindowHandler MotionWindowHandler { get; private set; }
        public CharacterRuntimeData RuntimeData { get; private set; }
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
            if (RuntimeData == null) RuntimeData = new CharacterRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();
        }

        protected abstract void InitRequiredComponents();

        public virtual void Init(Game.Logic.CharacterConfigAsset config)
        {
            Config = config;

            if (ActionPlayer == null) ActionPlayer = new ActionPlayer(this);
            if (MotionWindowHandler == null) MotionWindowHandler = new SkillMotionWindowHandler(this);
            if (RuntimeData == null) RuntimeData = new CharacterRuntimeData();
            if (StatusModule == null) StatusModule = new StatusModule();

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
            RuntimeData?.Update(Time.deltaTime);
            StatusModule?.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            StatusModule?.Clear();
            Game.Logic.ActionManager.Instance?.RemoveCache(this);
        }

        protected void SetTargetFinder(ITargetFinder targetFinder)
        {
            _targetFinder = targetFinder;
        }
    }
}
