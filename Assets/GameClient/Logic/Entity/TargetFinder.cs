using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 索敌接口，解耦实体与具体的索敌实现。
    /// </summary>
    public interface ITargetFinder
    {
        /// <summary>
        /// 获取当前的索敌目标。
        /// </summary>
        Transform GetTarget();
    }

    /// <summary>
    /// 玩家角色专用的索敌实现，使用球形范围检测并根据权重/距离筛选目标。
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

        public RoleTargetFinder(RoleTargetFinderCfg config)
        {
            _config = config ?? new RoleTargetFinderCfg();
        }

        public Transform GetTarget()
        {
            var activeEntity = TeamManager.Instance?.LocalCharacter;
            if (activeEntity == null) return null;

            Transform center = activeEntity.transform;

            Collider[] colliders = Physics.OverlapSphere(center.position, _config.SearchRadius, _config.SearchLayerMask);
            Transform bestTarget = null;
            int bestPriority = int.MaxValue;
            float closestSqrDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == center.gameObject) continue;

                int priority = _config.PriorityTags.IndexOf(col.tag);
                
                // 如果对象的 Tag 不在优先级配置列表中，直接跳过
                if (priority == -1)
                {
                    continue;
                }

                float sqrDist = (col.transform.position - center.position).sqrMagnitude;

                // 优先级数值越小越优先（索引靠前）
                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
                else if (priority == bestPriority && sqrDist < closestSqrDist) // 同等优先级取距离最近
                {
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
            }

            return bestTarget;
        }
    }

    [Serializable]
    public class MonsterSensorConfig
    {
        [Tooltip("警戒/发现玩家的半径")]
        public float DetectionRadius = 10f;
        [Tooltip("追击半径（超出此半径则丢失目标，返回巡逻）")]
        public float PursuitRadius = 20f;
        [Tooltip("视野夹角（角度，例如 120度表示前方宽广视野）")]
        [Range(0, 360)]
        public float FieldOfView = 120f;
        [Tooltip("攻击范围")]
        public float AttackRange = 2f;
        [Tooltip("攻击间隔冷却 (秒)")]
        public float AttackCooldown = 3f;
    }

    /// <summary>
    /// 怪物专用的轻量级索敌实现，利用状态机迟滞机制（Hysteresis），时间复杂度 O(1)。
    /// </summary>
    public class MonsterTargetFinder : ITargetFinder
    {
        private readonly MonsterSensorConfig _config;
        private readonly Transform _ownerTransform;
        
        private Transform _currentTarget;

        public MonsterTargetFinder(MonsterSensorConfig config, Transform ownerTransform)
        {
            _config = config ?? new MonsterSensorConfig();
            _ownerTransform = ownerTransform;
        }

        public Transform GetTarget()
        {
            if (_ownerTransform == null) return null;

            // [测试环境] 暂时注释掉基于 TeamManager 的索敌逻辑
            /*
            if (TeamManager.Instance == null) return null;
            
            // 获取当前上场的玩家角色
            var activeEntity = TeamManager.Instance.LocalCharacter;
            if (activeEntity == null) 
            {
                _currentTarget = null;
                return null;
            }

            Transform player = activeEntity.transform;
            */

            // [测试环境] 改为广域搜索 (OverlapSphere)
            Transform player = null;
            LayerMask localRoleMask = LayerMask.GetMask("LocalRole");
            // 搜索半径应取 DetectionRadius 和 PursuitRadius 的最大值，否则会导致 DetectionRadius > PursuitRadius 时远距离无法索敌
            float maxSearchRadius = Mathf.Max(_config.DetectionRadius, _config.PursuitRadius);
            Collider[] colliders = Physics.OverlapSphere(_ownerTransform.position, maxSearchRadius, localRoleMask);
            foreach (var col in colliders)
            {
                if (col.CompareTag("LocalRole"))
                {
                    // 过滤掉未上场(后台Standby)的角色，防止索敌锁定在原地的隐形队友身上
                    var entity = col.GetComponentInParent<RoleEntity>();
                    if (entity != null && !entity.IsControlActive)
                    {
                        continue;
                    }

                    player = col.transform;
                    break;
                }
            }

            if (player == null)
            {
                _currentTarget = null;
                return null;
            }

            float distanceSqr = (player.position - _ownerTransform.position).sqrMagnitude;

            if (_currentTarget == null)
            {
                // 还没发现目标时，使用 DetectionRadius (视野范围) 判定
                if (distanceSqr <= _config.DetectionRadius * _config.DetectionRadius)
                {
                    _currentTarget = player;
                }
            }
            else
            {
                // 已经发现目标时，使用 PursuitRadius (追击容差范围) 判定
                // 防止策划配置错误（视野大于追击）导致的索敌闪烁，追击半径不能小于视野半径
                float actualPursuitRadius = Mathf.Max(_config.PursuitRadius, _config.DetectionRadius);
                if (distanceSqr > actualPursuitRadius * actualPursuitRadius)
                {
                    _currentTarget = null; // 跑太远，丢失目标
                }
            }

            return _currentTarget;
        }
    }
}
