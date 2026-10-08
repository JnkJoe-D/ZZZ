namespace Game.GamePlay
{
    /// <summary>
    /// 角色招架与防御领域。
    /// 承载极限招架、弹刀冲突判定与支援招架上下文。
    /// </summary>
    public class RoleDefenseDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.Defense;
    }
}
