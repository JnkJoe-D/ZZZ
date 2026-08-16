using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public class MonsterEntity : CharacterEntity
    {
        protected override void InitRequiredComponents()
        {
            CharacterMotor = GetComponent<CharacterMotor>();
            if (CharacterMotor == null) CharacterMotor = gameObject.AddComponent<CharacterMotor>();

            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();

            // FootIKModule 如果需要的话也可以在这里挂载
            // FootIKModule = GetComponent<FootIKModule>();
            // if (FootIKModule == null) FootIKModule = gameObject.AddComponent<FootIKModule>();
        }

        public override void Init(Game.Logic.CharacterConfigAsset config)
        {
            base.Init(config);
            
            if (config is MonsterConfigAsset monsterConfig)
            {
                // 初始化真实怪物索敌 (测试环境或缺少TeamManager时直接传null)
                // 后续如果有全局 TeamManager，也可以通过类似 TeamManager.Instance 传入
                _targetFinder = new MonsterTargetFinder(monsterConfig.SensorConfig, transform, null);
            }
        }

    }
}
