using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class AIContext
    {
        public CharacterEntity Owner { get; private set; }
        public ITargetFinder TargetFinder { get; private set; }

        public AIContext(CharacterEntity owner, ITargetFinder targetFinder)
        {
            Owner = owner;
            TargetFinder = targetFinder;
        }

        // 快捷属性：方便节点直接获取
        public Vector3 OwnerPosition => Owner.transform.position;
        public Transform CurrentTarget => TargetFinder?.GetTarget();
        public bool HasTarget => CurrentTarget != null;
        
        // 距离计算等通用查询
        public float DistanceToTarget => HasTarget ? Vector3.Distance(OwnerPosition, CurrentTarget.position) : -1f;
    }
}
