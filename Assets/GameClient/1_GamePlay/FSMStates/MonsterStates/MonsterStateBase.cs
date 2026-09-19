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
        protected bool SendCommand(ActionConfigAsset action)
        {
            if (action == null || Entity == null || Entity.ActionController == null)
            {
                return false;
            }

            var cmd = CharacterCommandFactory.CreateDirectAssetCommand(action);
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
        /// 检查受击事实并跳转硬直状态
        /// </summary>
        protected bool TryEnterHitStun()
        {
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
    }
}
