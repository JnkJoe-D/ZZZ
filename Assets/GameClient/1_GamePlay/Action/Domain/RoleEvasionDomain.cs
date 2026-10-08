namespace Game.GamePlay
{
    /// <summary>
    /// 角色闪避与冲刺领域。
    /// 承载闪避使用记录、充能扣减、无敌窗口与极限闪避时停触发。
    /// </summary>
    public class RoleEvasionDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.Evasion;

        private EvadeRuntimeData _evadeData;

        public override void Initialize(CharacterEntity entity, ActionDomainContextModule context)
        {
            base.Initialize(entity, context);
            _evadeData = entity?.DataModule?.Get<EvadeRuntimeData>();
        }

        public override void OnEnter(ActionConfigAsset newAction, ActionDomain previousDomain)
        {
            base.OnEnter(newAction, previousDomain);
            _evadeData?.RecordEvade(Entity?.Config);
        }
    }
}
