using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物待机/静止对峙状态。
    /// 响应行为树设置的宏观策略，向奔跑与周旋状态机派发控制权。
    /// 规则：
    /// 1. Strategy == Approach: 判定若已在射程/物理接触到位，回写 IsInRange 并尝试瞬发出刀；
    ///    若未到位，一律切入 MonsterRunState (全速奔跑突进，禁止切 Walk)；
    /// 2. Strategy == Strafe: 若超出追赶阈值切 MonsterRunState，否则切 MonsterWalkState。
    /// </summary>
    public class MonsterIdleState : MonsterStateBase
    {
        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterStun()) return;
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            if (Context == null) return;

            RotateTowardsTarget(deltaTime);

            float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;

            if (Context.Strategy == MonsterStrategy.Approach)
            {
                float contactTol = LocoConfig != null ? LocoConfig.contactTolerance : 0.15f;
                if (IsApproachedTarget(distance, Context.TargetRadius, contactTol))
                {
                    Context.IsInRange = true;
                    if (TryEnterAttackFromContext()) return;
                    return;
                }

                // 未到位：攻击逼近必须全速奔跑冲刺！
                Machine.ChangeState<MonsterRunState>();
            }
            else if (Context.Strategy == MonsterStrategy.Strafe)
            {
                if (distance < 0f)
                {
                    Machine.ChangeState<MonsterWalkState>();
                    return;
                }

                float x = LocoConfig != null ? LocoConfig.strafeDistanceTolerance : 0.8f;
                float catchUpDelta = LocoConfig != null ? LocoConfig.strafeCatchUpThreshold : 2.5f;
                float targetDis = Context.TargetRadius > 0f ? Context.TargetRadius : 3.5f;
                float catchDis = targetDis + x + catchUpDelta;

                if (distance > catchDis)
                {
                    Machine.ChangeState<MonsterRunState>();
                }
                else
                {
                    Machine.ChangeState<MonsterWalkState>();
                }
            }
        }
    }
}
