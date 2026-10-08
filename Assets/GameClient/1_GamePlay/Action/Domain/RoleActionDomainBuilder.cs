namespace Game.GamePlay
{
    /// <summary>
    /// 角色动作领域装配构建器。
    /// 纯领域工厂类，负责为 RoleEntity 集中装配与注册其 6 大核心动作领域。
    /// 遵循依赖倒置与单一职责原则，避免实体组合根与具体领域实例强耦合。
    /// </summary>
    public static class RoleActionDomainBuilder
    {
        public static ActionDomainContextModule Build(RoleEntity role)
        {
            if (role == null) return null;

            var context = EntityModuleFactory.Create<ActionDomainContextModule>(role);
            context.RegisterDomain(new RoleLocomotionDomain());
            context.RegisterDomain(new RoleCombatDomain());
            context.RegisterDomain(new RoleEvasionDomain());
            context.RegisterDomain(new RoleDefenseDomain());
            context.RegisterDomain(new RoleHitReactionDomain());
            context.RegisterDomain(new RoleSwitchDomain());

            return context;
        }
    }
}
