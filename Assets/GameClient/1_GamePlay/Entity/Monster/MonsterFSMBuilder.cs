using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物状态机构建工厂。
    /// 纯领域工具类，集中装配怪物的 6 大基础移动与战斗状态。
    /// </summary>
    public static class MonsterFSMBuilder
    {
        public static FSMSystem<MonsterEntity> Build(MonsterEntity monster)
        {
            if (monster == null) return null;

            var fsm = new FSMSystem<MonsterEntity>(monster);
            fsm.AddState(new MonsterIdleState());
            fsm.AddState(new MonsterRunState());
            fsm.AddState(new MonsterBrakeState());
            fsm.AddState(new MonsterWalkState());
            fsm.AddState(new MonsterAttackState());
            fsm.AddState(new MonsterHitStunState());
            fsm.ChangeState<MonsterIdleState>();

            return fsm;
        }
    }
}
