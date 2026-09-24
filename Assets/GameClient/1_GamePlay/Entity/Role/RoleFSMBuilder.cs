using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色状态机构建工厂。
    /// 纯领域工具类，集中装配角色的 6 大基础地面与技能状态。
    /// </summary>
    public static class RoleFSMBuilder
    {
        public static FSMSystem<RoleEntity> Build(RoleEntity role)
        {
            if (role == null) return null;

            var fsm = new FSMSystem<RoleEntity>(role);
            fsm.AddState(new RoleGroundState());
            fsm.AddState(new RoleSkillState());
            fsm.AddState(new RoleEvadeState());
            fsm.AddState(new RoleHitStunState());
            fsm.AddState(new CharacterSwitchState());
            fsm.AddState(new RoleParryState());

            return fsm;
        }
    }
}
