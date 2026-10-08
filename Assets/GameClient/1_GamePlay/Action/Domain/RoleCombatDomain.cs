namespace Game.GamePlay
{
    /// <summary>
    /// 角色战斗与连招领域。
    /// 承载普攻、分支技、特殊技及终结技的连招生命周期与命中吸附上下文。
    /// </summary>
    public class RoleCombatDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.Combat;

        public int CurrentComboIndex { get; private set; }

        public override void OnEnter(ActionConfigAsset newAction, ActionDomain previousDomain)
        {
            base.OnEnter(newAction, previousDomain);
            CurrentComboIndex = 1;
        }

        public override void OnActionChangedWithinDomain(ActionConfigAsset previousAction, ActionConfigAsset newAction)
        {
            base.OnActionChangedWithinDomain(previousAction, newAction);
            CurrentComboIndex++;
        }

        public override void OnExit(ActionDomain nextDomain)
        {
            base.OnExit(nextDomain);
            CurrentComboIndex = 0;
        }
    }
}
