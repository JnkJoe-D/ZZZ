namespace Game.GamePlay
{
    /// <summary>
    /// 被动动作领域（空对象模式）。
    /// 专用于怪物实体（MonsterEntity）或不需要宏观角色领域状态机的实体。
    /// 确保零额外开销，且 100% 隔离怪物自身的 AI 状态机 (MonsterFSM)。
    /// </summary>
    public sealed class PassiveActionDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.None;
    }
}
