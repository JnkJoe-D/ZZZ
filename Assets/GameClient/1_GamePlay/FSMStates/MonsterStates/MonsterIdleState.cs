namespace Game.GamePlay
{
    /// <summary>
    /// 怪物待机/静止对峙状态。
    /// 响应行为树设置的宏观策略，根据距离差值决定切入奔跑还是慢走。
    /// </summary>
    public class MonsterIdleState : MonsterStateBase
    {
        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            if (Context == null) return;

            if (Context.Strategy == MonsterStrategy.Approach)
            {
                float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;
                float delta = distance - Context.TargetRadius;
                float runThreshold = LocoConfig != null ? LocoConfig.runThresholdRadius : 4.0f;

                if (delta > runThreshold)
                {
                    Machine.ChangeState<MonsterRunState>();
                }
                else
                {
                    Machine.ChangeState<MonsterWalkState>();
                }
            }
            else if (Context.Strategy == MonsterStrategy.Strafe)
            {
                Machine.ChangeState<MonsterWalkState>();
            }
        }
    }
}
