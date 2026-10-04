using System;
using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 目标锁定器参数配置，定义在 TeamConfigAsset 中由策划统一调优。
    /// 拒绝代码硬编码，支持进出迟滞区间、降频采样与偏角权重配置。
    /// </summary>
    [Serializable]
    public class TargetLockerConfig
    {
        [Tooltip("进入锁定最大半径 (米，初始索敌或重新切锁的生效范围)")]
        public float LockRadius = 14f;

        [Tooltip("脱离锁定最大半径 (米，必须 > LockRadius，形成迟滞区间防抖动)")]
        public float LoseRadius = 18f;

        [Tooltip("未锁定状态下的扫描周期 (秒，降频更新)")]
        public float ScanInterval = 0.08f;

        [Tooltip("正面朝向权重偏好 (0=纯距离优先, 1=强正面朝向优先)")]
        [Range(0f, 1f)]
        public float ForwardAngleWeight = 0.35f;

        [Tooltip("优先锁定的最大水平视场半角 (度)")]
        [Range(30f, 180f)]
        public float ViewFovHalfAngle = 90f;

        [Tooltip("近身 360 度全向索敌半径 (米，处于此距离内的近身怪物不受视场角度限制)")]
        public float CloseDetectRadius = 6f;
    }

    /// <summary>
    /// 纯领域层玩家目标锁定器，唯一挂载于 RoleTeamContext。
    /// 实现 IEntityModule 规范生命周期，采用“迟滞锁 (Hysteresis) + 降频时间片采样 + 连续内存数组扫描”。
    /// </summary>
    public class TargetLocker : IEntityModule
    {
        private readonly TargetLockerConfig _config;
        private CharacterEntity _owner;
        private MonsterEntity _lockedMonster;
        private float _scanTimer;
        private bool _isSubscribed;

        public MonsterEntity CurrentLockedMonster
        {
            get
            {
                // 即时有效性与距离检查：防止外部调用时读取到已超出 LoseRadius 的过期目标
                if (_lockedMonster != null && _owner != null && !IsTargetValidAndInRange(_lockedMonster, _config.LoseRadius))
                {
                    SetLockedTarget(null);
                }
                return _lockedMonster;
            }
        }
        public Transform CurrentLockedTransform => CurrentLockedMonster != null ? CurrentLockedMonster.transform : null;
        public bool HasTarget => CurrentLockedMonster != null;
        public TargetLockerConfig Config => _config;

        /// <summary>
        /// 锁定目标变更事件：参数为 (oldTarget, newTarget)
        /// 供表现层（锁定框 UI、相机注视提示）单向只读消费。
        /// </summary>
        public event Action<MonsterEntity, MonsterEntity> OnLockedTargetChanged;

        public TargetLocker(TargetLockerConfig config = null)
        {
            _config = config ?? new TargetLockerConfig();
            // 确保脱锁半径不小于进入半径，避免迟滞区间反向失效
            if (_config.LoseRadius < _config.LockRadius)
            {
                _config.LoseRadius = _config.LockRadius;
            }
        }

        public void Initialize(CharacterEntity owner)
        {
            _owner = owner;

            if (!_isSubscribed)
            {
                EventCenter.Subscribe<EntityDiedEvent>(OnEntityDied);
                _isSubscribed = true;
            }

            // 若已有锁定目标但换了新角色，立即校验新角色到该目标的距离
            if (_lockedMonster != null && !IsTargetValidAndInRange(_lockedMonster, _config.LoseRadius))
            {
                SetLockedTarget(null);
            }
        }

        public void LogicTick(float logicDeltaTime)
        {
            if (_owner == null) return;

            // 1. 若当前已有锁定目标：执行 O(1) 迟滞健康检查
            if (_lockedMonster != null)
            {
                if (IsTargetValidAndInRange(_lockedMonster, _config.LoseRadius))
                {
                    // 目标存活且处于脱离半径内，直接保持锁定（耗时约 2ns，直接返回）
                    return;
                }

                // 目标死亡、失效或超出 LoseRadius，解除锁定并准备切锁
                SetLockedTarget(null);
                _scanTimer = _config.ScanInterval; // 触发立即重扫描
            }

            // 2. 无锁定目标时：根据降频采样周期轮询最佳目标
            _scanTimer += logicDeltaTime;
            if (_scanTimer >= _config.ScanInterval)
            {
                _scanTimer = 0f;
                FindAndLockBestTarget();
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                EventCenter.Unsubscribe<EntityDiedEvent>(OnEntityDied);
                _isSubscribed = false;
            }

            SetLockedTarget(null);
            _owner = null;
        }

        /// <summary>
        /// 主动刷新并搜寻最佳目标（供无锁时外部强行立即索敌）
        /// </summary>
        public void RefreshBestTarget()
        {
            if (_owner == null) return;
            FindAndLockBestTarget();
        }

        /// <summary>
        /// 外部强制锁定指定怪物实体（如切锁按键操作）
        /// </summary>
        public void ForceLock(MonsterEntity monster)
        {
            if (monster != null && IsMonsterTargetable(monster))
            {
                SetLockedTarget(monster);
            }
        }

        /// <summary>
        /// 外部主动清空当前锁定
        /// </summary>
        public void Unlock()
        {
            SetLockedTarget(null);
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (_lockedMonster != null && evt.Victim == _lockedMonster)
            {
                // 当前锁定的怪物死亡：零延迟解除锁定并立即寻找下一目标（明确排除已死亡的受害者）
                var deadVictim = _lockedMonster;
                SetLockedTarget(null);
                if (_owner != null)
                {
                    FindAndLockBestTarget(excludeMonster: deadVictim);
                }
            }
        }

        private void FindAndLockBestTarget(MonsterEntity excludeMonster = null)
        {
            var monsterMgr = MonsterManager.Instance;
            if (monsterMgr == null) return;

            var activeMonsters = monsterMgr.ActiveMonsters;
            if (activeMonsters == null || activeMonsters.Count == 0) return;

            Vector3 ownerPos = _owner.transform.position;
            Vector3 refForward = _owner.transform.forward;
            if (Camera.main != null)
            {
                Vector3 camFwd = Camera.main.transform.forward;
                camFwd.y = 0;
                if (camFwd.sqrMagnitude > 0.001f)
                {
                    refForward = camFwd.normalized;
                }
            }

            MonsterEntity bestMonster = null;
            float bestScore = float.MaxValue;
            float maxLockSqr = _config.LockRadius * _config.LockRadius;

            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var monster = activeMonsters[i];
                if (excludeMonster != null && monster == excludeMonster) continue;
                if (!IsMonsterTargetable(monster)) continue;

                Vector3 toMonster = monster.transform.position - ownerPos;
                toMonster.y = 0; // 平面化水平距离计算
                float sqrDist = toMonster.sqrMagnitude;
                if (sqrDist > maxLockSqr) continue;

                float dist = Mathf.Sqrt(sqrDist);
                float angle = Vector3.Angle(refForward, toMonster);

                // 综合打分机制：
                // 1. 若处于 CloseDetectRadius 内，全向 360 度感知近战威胁，完全免除视场偏角惩罚；
                // 2. 超出 CloseDetectRadius 且处于 ViewFovHalfAngle 内时，施加正面偏角加权；
                // 3. 超出视场半角的背面/盲区追击敌人，施加盲区偏角惩罚（确保正面敌人优先，但当正面无怪且背后有怪追击时仍能兜底锁住）。
                float score;
                if (dist <= _config.CloseDetectRadius)
                {
                    score = dist;
                }
                else if (angle <= _config.ViewFovHalfAngle)
                {
                    float angleRatio = angle / _config.ViewFovHalfAngle;
                    score = dist * (1f + angleRatio * _config.ForwardAngleWeight);
                }
                else
                {
                    // 盲区追击惩罚倍率
                    score = dist * (1f + _config.ForwardAngleWeight + 2.0f);
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMonster = monster;
                }
            }

            if (bestMonster != null)
            {
                SetLockedTarget(bestMonster);
            }
        }

        private bool IsTargetValidAndInRange(MonsterEntity monster, float maxRadius)
        {
            if (!IsMonsterTargetable(monster)) return false;

            Vector3 toMonster = monster.transform.position - _owner.transform.position;
            toMonster.y = 0;
            return toMonster.sqrMagnitude <= maxRadius * maxRadius;
        }

        private bool IsMonsterTargetable(MonsterEntity monster)
        {
            if (monster == null || monster.gameObject == null) return false;
            if (!monster.gameObject.activeInHierarchy) return false;
            if (monster.DataModule?.Get<LifecycleRuntimeData>()?.IsDead ?? false) return false;
            return true;
        }

        private void SetLockedTarget(MonsterEntity newTarget)
        {
            if (ReferenceEquals(_lockedMonster, newTarget)) return;

            var oldTarget = _lockedMonster;
            _lockedMonster = newTarget;
            OnLockedTargetChanged?.Invoke(oldTarget, newTarget);
        }
    }
}
