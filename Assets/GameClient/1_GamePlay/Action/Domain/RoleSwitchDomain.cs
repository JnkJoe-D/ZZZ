namespace Game.GamePlay
{
    /// <summary>
    /// 角色换入/换出领域。
    /// 承载小队切人入场动作、连携换人及脱场无敌/残留生命周期。
    /// </summary>
    public class RoleSwitchDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.Switch;
    }
}
