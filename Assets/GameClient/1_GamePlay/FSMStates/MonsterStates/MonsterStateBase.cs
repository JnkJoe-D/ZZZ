using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物状态机基类。
    /// 封装快捷访问：Machine、Entity、Context、LocoConfig。
    /// 集中提供通用动作指令下发、受击判定与转向辅助。
    /// </summary>
    public abstract class MonsterStateBase : IFSMState<MonsterEntity>
    {
        protected FSMSystem<MonsterEntity> Machine;
        protected MonsterEntity Entity => Machine.Owner;
        protected MonsterTacticalContext Context => Entity.TacticalContext;
        protected MonsterLocomotionConfig LocoConfig => Context?.LocomotionConfig;

        public virtual void OnInit(FSMSystem<MonsterEntity> fsm)
        {
            Machine = fsm;
        }

        public virtual bool CanEnter() => true;
        public virtual bool CanExit() => true;
        public virtual void OnEnter() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnFixedUpdate(float fixedDeltaTime) { }
        public virtual void OnExit() { }
        public virtual void OnDestroy() { }

        // ── 通用管道工具 ──

        /// <summary>
        /// 向实体动作控制器压入资产指令
        /// </summary>
        protected bool SendCommand(ActionConfigAsset action, Action onComplete = null)
        {
            if (action == null || Entity == null || Entity.ActionController == null)
            {
                return false;
            }

            var cmd = CharacterCommandFactory.CreateDirectAssetCommand(action, onComplete: onComplete);
            Entity.ActionController.OnInput(cmd);
            return true;
        }

        /// <summary>
        /// 检查行为树是否下发了瞬时攻击意图（如在移动中收到 PendingAttack 瞬切出刀）
        /// </summary>
        protected bool TryEnterAttackFromContext()
        {
            if (Context != null && Context.PendingAttack != null)
            {
                Machine.ChangeState<MonsterAttackState>();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查失衡事实并跳转失衡瘫痪状态（最高优先级控制）
        /// </summary>
        protected bool TryEnterStun()
        {
            var attrs = Entity.StatusModule?.Attributes;
            if (attrs != null && attrs.Has(AttributeId.Daze) && attrs.Has(AttributeId.MaxDaze))
            {
                float daze = attrs.GetCurrent(AttributeId.Daze);
                float maxDaze = attrs.GetCurrent(AttributeId.MaxDaze);
                if (maxDaze > 0f && daze >= maxDaze)
                {
                    if (Machine != null && Machine.CurrentState is not MonsterStunState)
                    {
                        Machine.ChangeState<MonsterStunState>();
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查受击事实并跳转硬直状态（失衡状态下不被普通受击打断抢占）
        /// </summary>
        protected bool TryEnterHitStun()
        {
            if (Machine != null && Machine.CurrentState is MonsterStunState) return false;

            var hitData = Entity.DataModule?.Get<HitReactionRuntimeData>();
            if (hitData != null && hitData.InHitReaction)
            {
                Machine.ChangeState<MonsterHitStunState>();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 面向当前锁定目标旋转
        /// </summary>
        protected void RotateTowardsTarget(float deltaTime, float overrideTurnSpeed = -1f)
        {
            var target = Entity.TargetFinder?.GetTarget();
            if (target == null) return;

            if (Entity.MovementComponent != null)
            {
                if (deltaTime <= 0f)
                {
                    Entity.MovementComponent.FaceToTargetImmediately(target);
                }
                else
                {
                    Entity.MovementComponent.FaceToTarget(target, overrideTurnSpeed);
                }
                return;
            }

            // 兜底退化方案：直接修改 Transform
            Vector3 dir = target.position - Entity.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            float speed = overrideTurnSpeed > 0f ? overrideTurnSpeed : 360f;

            if (deltaTime <= 0f)
            {
                Entity.transform.rotation = targetRot;
            }
            else
            {
                Entity.transform.rotation = Quaternion.RotateTowards(Entity.transform.rotation, targetRot, speed * deltaTime);
            }
        }

        /// <summary>
        /// 获取怪物自身物理几何半径
        /// </summary>
        protected float GetSelfRadius()
        {
            if (Entity?.MovementComponent != null && Entity.MovementComponent.CharacterRadius > 0f)
            {
                return Entity.MovementComponent.CharacterRadius;
            }
            if (Entity != null && Entity.TryGetComponent<CharacterController>(out var cc))
            {
                return cc.radius;
            }
            return 0.8f;
        }

        /// <summary>
        /// 获取锁定目标的物理几何半径
        /// </summary>
        protected float GetTargetRadius()
        {
            var target = Entity?.TargetFinder?.GetTarget();
            if (target == null) return 0.5f;

            if (target.TryGetComponent<CharacterController>(out var cc))
            {
                return cc.radius;
            }
            return 0.5f;
        }

        /// <summary>
        /// 获取双方物理胶囊体半径之和（即发生物理接触阻挡时的中心距离理论极限）
        /// </summary>
        protected float GetCombinedPhysicalRadius()
        {
            return GetSelfRadius() + GetTargetRadius();
        }

        /// <summary>
        /// 判定是否逼近到位：满足招式射程，或已经与目标发生物理贴身接触（防止碰撞体阻挡卡死）
        /// </summary>
        protected bool IsApproachedTarget(float currentDistance, float targetRadius, float contactTolerance = 0.15f)
        {
            if (currentDistance < 0f) return false;
            if (currentDistance <= targetRadius) return true;

            float contactDistance = GetCombinedPhysicalRadius() + contactTolerance;
            return currentDistance <= contactDistance;
        }
    }
}
