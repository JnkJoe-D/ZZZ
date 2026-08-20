using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class MonsterAttackData : TaskData
    {
        public ActionConfigAsset action; // 攻击动作
        public float cooldown; // 攻击后触发的冷却时间
    }
}
