using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 玩家角色专用的索敌实现，实现 ITargetFinder 契约。
    /// 内聚常规目标（由小队 TargetLocker 迟滞锁定驱动）与 CombatContextTarget（招架/反击等交互目标）的高优先级仲裁。
    /// 彻底移除低效且易出 Bug 的物理球体重叠扫描 (Physics.OverlapSphereNonAlloc)。
    /// </summary>
    public class RoleTargetFinder : ITargetFinder
    {
        [Serializable]
        public class RoleTargetFinderCfg
        {
            [Tooltip("搜索半径 (作为 TargetLocker 缺失时的兜底扫描半径)")]
            public float SearchRadius = 15f;
            
            [Tooltip("搜索的层级过滤 (保留字段以兼容旧配置序列化)")]
            public LayerMask SearchLayerMask = -1;
            
            [Tooltip("优先级标签 (保留字段以兼容旧配置序列化)")]
            public List<string> PriorityTags = new List<string> { "Enemy", "Monster" };
        }

        private readonly RoleTargetFinderCfg _config;
        private TargetLocker _targetLocker;
        private CharacterEntity _owner;

        public CharacterEntity OwnerEntity => _owner;
        public CharacterEntity CombatContextTarget { get; set; }
        public TargetLocker TargetLocker => _targetLocker;

        public RoleTargetFinder(RoleTargetFinderCfg config = null, TargetLocker targetLocker = null)
        {
            _config = config ?? new RoleTargetFinderCfg();
            _targetLocker = targetLocker;
        }

        public void BindTargetLocker(TargetLocker targetLocker)
        {
            _targetLocker = targetLocker;
        }

        public void Initialize(CharacterEntity owner)
        {
            _owner = owner;
        }

        public void LogicTick(float logicDeltaTime)
        {
            if (CombatContextTarget != null && !IsCombatContextTargetValid())
            {
                ClearCombatContextTarget();
            }
        }

        public void Dispose()
        {
            ClearCombatContextTarget();
            _targetLocker = null;
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

        /// <summary>
        /// 获取常规锁定目标：优先读取 TargetLocker 当前维持的锁定目标；
        /// 若 Locker 暂无目标，则触发一次快速搜寻；若未挂载 Locker 则兜底扫描 MonsterManager 活跃列表。
        /// </summary>
        public Transform GetTarget()
        {
            if (_owner == null) return null;

            // 1. 优先从 TargetLocker 读取当前维持的锁定目标 (O(1) 内存访问)
            if (_targetLocker != null)
            {
                if (_targetLocker.CurrentLockedTransform != null)
                {
                    return _targetLocker.CurrentLockedTransform;
                }

                // 若处于无目标状态，触发一次即时搜寻（如刚进入场景尚未 Tick）
                _targetLocker.RefreshBestTarget();
                if (_targetLocker.CurrentLockedTransform != null)
                {
                    return _targetLocker.CurrentLockedTransform;
                }
                return null;
            }

            // 2. 兜底方案（未注入 Locker 时的降级兼容）：直接内存线性遍历 MonsterManager.ActiveMonsters
            return FallbackScanActiveMonsters();
        }

        public float GetDistanceToTarget()
        {
            var target = GetEffectiveTarget();
            var activeEntity = _owner;
            if (target == null || activeEntity == null) return -1f;

            return Vector3.Distance(activeEntity.transform.position, target.position);
        }

        public Transform GetEffectiveTarget()
        {
            // 最高优先级：动作/战斗交互上下文目标（如招架黄光攻击者、弹刀反击目标）
            if (CombatContextTarget != null)
            {
                if (IsCombatContextTargetValid())
                {
                    return CombatContextTarget.transform;
                }

                // 超出脱锁距离或失效时立即自愈清理，防止永久黏死
                ClearCombatContextTarget();
            }

            // 次级优先级：常规锁定目标 (TargetLocker)
            return GetTarget();
        }

        private bool IsCombatContextTargetValid()
        {
            if (CombatContextTarget == null || CombatContextTarget.gameObject == null) return false;
            if (!CombatContextTarget.gameObject.activeInHierarchy || CombatContextTarget.IsDead) return false;
            if (_owner == null) return false;

            float maxDist = _targetLocker?.Config?.LoseRadius ?? _config.SearchRadius;
            Vector3 toTarget = CombatContextTarget.transform.position - _owner.transform.position;
            toTarget.y = 0;
            return toTarget.sqrMagnitude <= maxDist * maxDist;
        }

        private Transform FallbackScanActiveMonsters()
        {
            var monsterMgr = MonsterManager.Instance;
            if (monsterMgr == null || monsterMgr.ActiveMonsters == null) return null;

            var activeMonsters = monsterMgr.ActiveMonsters;
            Vector3 ownerPos = _owner.transform.position;
            float maxRadiusSqr = _config.SearchRadius * _config.SearchRadius;

            Transform closestTarget = null;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var monster = activeMonsters[i];
                if (monster == null || monster.gameObject == null) continue;
                if (!monster.gameObject.activeInHierarchy || monster.IsDead) continue;

                Vector3 toMonster = monster.transform.position - ownerPos;
                toMonster.y = 0;
                float sqrDist = toMonster.sqrMagnitude;

                if (sqrDist <= maxRadiusSqr && sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closestTarget = monster.transform;
                }
            }

            return closestTarget;
        }
    }
}
