using Game.Logic;
using Game.Framework;
using UnityEngine;

namespace Game.Logic
{
    public class MonsterEntity : CharacterEntity
    {
        public new MonsterConfigAsset Config => (MonsterConfigAsset)base.Config;
        
        public Game.Logic.AI.BehaviorTree.BTRunner BTRunner { get; private set; }
        protected override void InitRequiredComponents()
        {
            CharacterMotor = GetComponent<CharacterMotor>();
            if (CharacterMotor == null) CharacterMotor = gameObject.AddComponent<CharacterMotor>();

            HitReactionModule = GetComponent<MonsterHitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<MonsterHitReactionModule>();

            BTRunner = GetComponent<Game.Logic.AI.BehaviorTree.BTRunner>();
            if (BTRunner == null) BTRunner = gameObject.AddComponent<Game.Logic.AI.BehaviorTree.BTRunner>();

            LifecycleModule = GetComponent<MonsterLifecycleModule>();
            if (LifecycleModule == null) LifecycleModule = gameObject.AddComponent<MonsterLifecycleModule>();

            // FootIKModule 如果需要的话也可以在这里挂载
            // FootIKModule = GetComponent<FootIKModule>();
            // if (FootIKModule == null) FootIKModule = gameObject.AddComponent<FootIKModule>();
        }

        public override void Init(Game.Logic.CharacterConfigAsset config)
        {
            base.Init(config);
            
            if (CommandBuffer == null) CommandBuffer = new CommandBuffer(BufferMode.SingleOverride);
            if (ActionController == null) ActionController = new MonsterActionController(this);
            DataModule[typeof(MonSterBehaviorRuntimeData)] ??= new MonSterBehaviorRuntimeData();
            DataModule[typeof(TimeDilationRuntimeData)] ??= new TimeDilationRuntimeData();

            if (config is MonsterConfigAsset monsterConfig)
            {
                TargetFinder = new MonsterTargetFinder(monsterConfig.SensorConfig, transform);

                if (monsterConfig.ActionRoot != null)
                {
                    ActionController.PlayAction(monsterConfig.ActionRoot);
                }

                if (monsterConfig.BehaviorTree != null && BTRunner != null)
                {
                    BTRunner.Init(monsterConfig.BehaviorTree);
                    BTRunner.StartTree();
                }
            }
        }

        /// <summary>
        /// 应用子弹时间减速流速（仅更新数据模型并驱动动作播放器，实体保持零字段）
        /// </summary>
        public void ApplyBulletTime(float scale)
        {
            var timeData = DataModule.Get<TimeDilationRuntimeData>();
            timeData?.ApplyBulletTime(scale);
            ActionPlayer?.SetPlaySpeed(scale);
        }

        /// <summary>
        /// 解除子弹时间（打醒恢复或倒计时结束）
        /// </summary>
        public void ExitBulletTime()
        {
            var timeData = DataModule.Get<TimeDilationRuntimeData>();
            timeData?.ExitBulletTime();
            ActionPlayer?.RestorePlaySpeed();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            ExitBulletTime();
            DataModule.Get<TimeDilationRuntimeData>()?.Reset();

            if (BTRunner != null)
            {
                BTRunner.StopTree();
            }
            TargetFinder = null;
        }

        protected override void Update()
        {
            base.Update();
            var timeData = DataModule.Get<TimeDilationRuntimeData>();
            float dt = Time.deltaTime * (timeData != null ? timeData.TimeScale : 1.0f);
            ActionController?.Update(dt);
            DataModule.Get<MonSterBehaviorRuntimeData>()?.Update(dt);
        }
    }
}
