using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物奔跑状态 (RunF)。
    /// 严格收敛为两种奔跑业务场景：
    /// 1. 场景 1 [攻击突进]: 攻击冷却完毕 (Strategy == Approach)，全程 RunF 冲刺逼近；
    ///    达到攻击射程或物理接触卡滞时，立即回写 IsInRange 并瞬切出刀，跳过 runEnd 刹车；
    /// 2. 场景 2 [冷却脱离追赶]: 攻击冷却中 (Strategy == Strafe)，玩家跑远超过追赶阈值，
    ///    以 RunF 快速跟进；拉近至对峙区间上限后，降档切回 MonsterWalkState 挑选周旋步态。
    /// </summary>
    public class MonsterRunState : MonsterStateBase
    {
        public override void OnEnter()
        {
            var runAction = LocoConfig?.RunStart != null ? LocoConfig.RunStart : LocoConfig?.RunLoop;
            if (runAction != null)
            {
                SendCommand(runAction);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterStun()) return;
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            if (Context == null)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            // 保持对目标的平滑朝向跟踪
            RotateTowardsTarget(deltaTime);

            float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;
            if (distance < 0f) return;

            float contactTol = LocoConfig != null ? LocoConfig.contactTolerance : 0.15f;

            // ── 场景 1：攻击突进追击 (Approach) ──
            if (Context.Strategy == MonsterStrategy.Approach)
            {
                // 到位判定双重保护：招式有效射程 或 胶囊体物理接触碰撞（解决撞人原地狂跑）
                if (IsApproachedTarget(distance, Context.TargetRadius, contactTol))
                {
                    Context.IsInRange = true;
                    // 若已有待执行攻击，立即瞬发攻击，不播 runEnd 刹车
                    if (TryEnterAttackFromContext()) return;

                    Machine.ChangeState<MonsterIdleState>();
                    return;
                }

                // 攻击突进模式下全程保持 RunF，绝不中途降档切为 Walk
                EnsureRunLoopPlaying();
            }
            // ── 场景 2：冷却中远距离追赶 (Strafe) ──
            else if (Context.Strategy == MonsterStrategy.Strafe)
            {
                float x = LocoConfig != null ? LocoConfig.strafeDistanceTolerance : 0.8f;
                float targetRadius = Context.TargetRadius > 0f ? Context.TargetRadius : 3.5f;
                float maxStrafeDistance = targetRadius + x;

                // 已经追回到对峙区上限，降档切回 WalkState 自主选择周旋步态
                if (distance <= maxStrafeDistance || IsApproachedTarget(distance, targetRadius, contactTol))
                {
                    Machine.ChangeState<MonsterWalkState>();
                    return;
                }

                EnsureRunLoopPlaying();
            }
            // ── 其他策略 (Idle) ──
            else
            {
                Machine.ChangeState<MonsterIdleState>();
            }
        }

        private void EnsureRunLoopPlaying()
        {
            if (LocoConfig?.RunLoop == null || Entity.ActionController == null) return;

            var current = Entity.ActionController.CurrentPlayingAction;
            if (current != LocoConfig.RunStart && current != LocoConfig.RunLoop)
            {
                SendCommand(LocoConfig.RunLoop);
            }
        }
    }
}
