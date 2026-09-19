using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物奔跑逼近状态。
    /// 负责远距离快速突进逼近目标。
    /// 微观自决策：
    /// 1. 差值 Δ <= 0 时到位，回写 IsInRange 并切入急停刹车状态 MonsterBrakeState；
    /// 2. 差值 Δ <= runThreshold 时减速切入缓步前压 MonsterWalkState；
    /// 3. 响应瞬时攻击与受击打断。
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
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            // 宏观策略脱离逼近状态
            if (Context.Strategy != MonsterStrategy.Approach)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            RotateTowardsTarget(deltaTime);

            float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;
            if (distance < 0f) return;

            float delta = distance - Context.TargetRadius;

            // 1. 达到指定攻击射程
            if (delta <= 0f)
            {
                Context.IsInRange = true;
                Machine.ChangeState<MonsterBrakeState>();
                return;
            }

            // 2. 距离缩小至慢速前压区
            float runThreshold = LocoConfig != null ? LocoConfig.runThresholdRadius : 4.0f;
            if (delta <= runThreshold)
            {
                Machine.ChangeState<MonsterWalkState>();
            }
        }
    }
}
