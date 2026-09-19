using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 玩家角色专用的索敌实现，使用球形范围检测并根据权重/距离筛选目标。
    /// 实现 ITargetFinder 与 IEntityComponent，内聚目标仲裁与 CombatContextTarget 维护。
    /// </summary>
    public class RoleTargetFinder : ITargetFinder
    {
        [Serializable]
        public class RoleTargetFinderCfg
        {
            [Tooltip("搜索半径")]
            public float SearchRadius = 15f;
            
            [Tooltip("搜索的层级过滤")]
            public LayerMask SearchLayerMask = -1; // 默认 All
            
            [Tooltip("优先级标签，越靠前获取时优先级越高")]
            public List<string> PriorityTags = new List<string> { "Enemy", "Monster" };
        }

        private readonly RoleTargetFinderCfg _config;
        private static readonly Collider[] _overlapBuffer = new Collider[32];
        private CharacterEntity _owner;

        public CharacterEntity OwnerEntity => _owner;
        public CharacterEntity CombatContextTarget { get; set; }

        public RoleTargetFinder(RoleTargetFinderCfg config)
        {
            _config = config ?? new RoleTargetFinderCfg();
        }

        public void Initialize(CharacterEntity owner)
        {
            _owner = owner;
        }

        public void OnLogicTick(float logicDeltaTime)
        {
        }

        public void Dispose()
        {
            ClearCombatContextTarget();
            _owner = null;
        }

        public void SetCombatContextTarget(CharacterEntity target)
        {
            CombatContextTarget = target;
        }

        public void ClearCombatContextTarget()
        {
            CombatContextTarget = null;
        }

        public Transform GetTarget()
        {
            var activeEntity = _owner;
            if (activeEntity == null) return null;

            Transform center = activeEntity.transform;

            int hitCount = Physics.OverlapSphereNonAlloc(center.position, _config.SearchRadius, _overlapBuffer, _config.SearchLayerMask);
            Transform bestTarget = null;
            int bestPriority = int.MaxValue;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null || col.gameObject == center.gameObject) continue;

                int priority = -1;
                if (_config.PriorityTags != null)
                {
                    for (int p = 0; p < _config.PriorityTags.Count; p++)
                    {
                        if (col.CompareTag(_config.PriorityTags[p]))
                        {
                            priority = p;
                            break;
                        }
                    }
                }
                
                if (priority == -1)
                {
                    continue;
                }

                float sqrDist = (col.transform.position - center.position).sqrMagnitude;

                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
                else if (priority == bestPriority && sqrDist < closestSqrDist)
                {
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
            }

            Array.Clear(_overlapBuffer, 0, hitCount);

            return bestTarget;
        }

        public float GetDistanceToTarget()
        {
            var target = GetTarget();
            var activeEntity = _owner;
            if (target == null || activeEntity == null) return -1f;

            return Vector3.Distance(activeEntity.transform.position, target.position);
        }

        public Transform GetEffectiveTarget()
        {
            if (CombatContextTarget != null && CombatContextTarget.gameObject.activeInHierarchy && !CombatContextTarget.IsDead)
            {
                return CombatContextTarget.transform;
            }
            return GetTarget();
        }
    }
}
