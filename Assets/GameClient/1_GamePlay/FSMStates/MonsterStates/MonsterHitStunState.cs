namespace Game.GamePlay
{
    /// <summary>
    /// 怪物受击硬直状态。
    /// OnEnter：清空战术上下文意图，打断任何待发指令。
    /// OnUpdate：监控 HitReactionRuntimeData，硬直结束后恢复 Idle 待机。
    /// </summary>
    public class MonsterHitStunState : MonsterStateBase
    {
        public override void OnEnter()
        {
            // 受击打断当前战术待发指令
            Context?.Reset();
        }

        public override void OnUpdate(float deltaTime)
        {
            var hitData = Entity.DataModule?.Get<HitReactionRuntimeData>();
            if (hitData == null || !hitData.InHitReaction)
            {
                Machine.ChangeState<MonsterIdleState>();
            }
        }
    }
}
