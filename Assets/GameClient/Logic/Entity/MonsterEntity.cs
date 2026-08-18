using Game.Logic;
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

            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();

            BTRunner = GetComponent<Game.Logic.AI.BehaviorTree.BTRunner>();
            if (BTRunner == null) BTRunner = gameObject.AddComponent<Game.Logic.AI.BehaviorTree.BTRunner>();

            // FootIKModule 如果需要的话也可以在这里挂载
            // FootIKModule = GetComponent<FootIKModule>();
            // if (FootIKModule == null) FootIKModule = gameObject.AddComponent<FootIKModule>();
        }

        public override void Init(Game.Logic.CharacterConfigAsset config)
        {
            base.Init(config);
            
            if (config is MonsterConfigAsset monsterConfig)
            {
                // 自己创建专属的索敌雷达 (不再需要显式注入全局单例)
                _targetFinder = new MonsterTargetFinder(monsterConfig.SensorConfig, transform);

                if (monsterConfig.BehaviorTree != null && BTRunner != null)
                {
                    BTRunner.Init(monsterConfig.BehaviorTree);
                    BTRunner.StartTree();
                }
            }

            if (ActionPlayer != null)
            {
                ActionPlayer.OnActionComplete += RecordAttackTime;
                ActionPlayer.OnActionInterrupt += RecordAttackTime;
            }
        }

        protected override void OnDestroy()
        {
            if (ActionPlayer != null)
            {
                ActionPlayer.OnActionComplete -= RecordAttackTime;
                ActionPlayer.OnActionInterrupt -= RecordAttackTime;
            }
        }

        private float _lastAttackTime = -9999f;

        private void RecordAttackTime()
        {
            // TODO: implement logic in NPBehave tasks
            _lastAttackTime = Time.time;
        }

        protected override void Update()
        {
            base.Update();
            // All legacy BT state update code removed. 
            // In NPBehave, Blackboard synchronization will be handled by Sensor Services within the tree.
        }
    }
}
