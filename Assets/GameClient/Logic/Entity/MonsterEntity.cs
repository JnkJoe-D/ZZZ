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

        protected override void OnDestroy()
        {
        }

        protected override void Update()
        {
            base.Update();
            ActionController?.Update(Time.deltaTime);
            DataModule.Get<MonSterBehaviorRuntimeData>()?.Update(Time.deltaTime);
        }
    }
}
