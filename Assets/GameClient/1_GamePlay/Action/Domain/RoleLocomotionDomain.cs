namespace Game.GamePlay
{

    /// <summary>
    /// 角色地面与空间位移领域。
    /// 承载移动/站立/跑步/急停等地面位移转向黑板与阻尼业务。
    /// </summary>
    public class RoleLocomotionDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.Locomotion;
    }
}
