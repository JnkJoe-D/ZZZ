using System;
using UnityEngine;

namespace Game.GamePlay
{
    [Serializable]
    public class MonsterSensorConfig
    {
        [Tooltip("索敌/目标感知最大半径 (r1)")]
        public float DetectionRadius = 50f;

        [Tooltip("Run与Walk切换阈值距离 (r2)：当逼近缺口 > r2 时使用 Run，<= r2 时使用 Walk")]
        public float RunThresholdRadius = 4.0f;

        [Tooltip("全局统一攻击节奏间隔 (秒)：一套攻击序列打完后的攻防转换呼吸期")]
        public float AttackInterval = 2.5f;
    }

    /// <summary>
    /// 怪物专用的轻量级索敌实现，利用状态机迟滞机制（Hysteresis），时间复杂度 O(1)。
    /// 实现 ITargetFinder 与 IEntityComponent，内聚目标仲裁与 CombatContextTarget 维护。
    /// </summary>
    public class MonsterTargetFinder : ITargetFinder
    {
        private readonly MonsterSensorConfig _config;
        private Transform _ownerTransform;
        private CharacterEntity _owner;
        private Transform _currentTarget;

        public CharacterEntity OwnerEntity => _owner;
        public CharacterEntity CombatContextTarget { get; set; }

        public MonsterTargetFinder(MonsterSensorConfig config, Transform ownerTransform)
        {
            _config = config ?? new MonsterSensorConfig();
            _ownerTransform = ownerTransform;
        }

        public void Initialize(CharacterEntity owner)
        {
            _owner = owner;
            if (owner != null)
            {
                _ownerTransform = owner.transform;
            }
        }

        public void LogicTick(float logicDeltaTime)
        {
        }

        public void Dispose()
        {
            ClearCombatContextTarget();
            _currentTarget = null;
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
            if (_ownerTransform == null) return null;

            RoleEntity localRole = TeamManager.Instance?.LocalCharacter;
            if (localRole == null || (localRole.DataModule?.Get<LifecycleRuntimeData>()?.IsDead ?? false) || !localRole.gameObject.activeInHierarchy)
            {
                _currentTarget = null;
                return null;
            }

            // 感知半径 (DetectionRadius) 范围校验
            float maxSearchRadius = _config.DetectionRadius;
            if (maxSearchRadius > 0f)
            {
                float distanceSqr = (localRole.transform.position - _ownerTransform.position).sqrMagnitude;
                if (distanceSqr > maxSearchRadius * maxSearchRadius)
                {
                    _currentTarget = null;
                    return null;
                }
            }

            _currentTarget = localRole.transform;
            return _currentTarget;
        }

        public float GetDistanceToTarget()
        {
            var target = GetTarget();
            if (target == null || _ownerTransform == null) return -1f;

            return Vector3.Distance(_ownerTransform.position, target.position);
        }

        public Transform GetEffectiveTarget()
        {
            if (CombatContextTarget != null && CombatContextTarget.gameObject.activeInHierarchy && !(CombatContextTarget.DataModule?.Get<LifecycleRuntimeData>()?.IsDead ?? false))
            {
                return CombatContextTarget.transform;
            }
            return GetTarget();
        }
    }
}
