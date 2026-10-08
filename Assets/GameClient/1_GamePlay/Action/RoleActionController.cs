 

namespace Game.GamePlay
{
    public class RoleActionController : ActionController
    {
        private RoleEntity Role => (RoleEntity)_entity;

        private ComboRouteRuntimeData _comboData;

        public override void Initialize(CharacterEntity owner)
        {
            base.Initialize(owner);
            _routeEventReceiver ??= new RoleRouteEventReceiver();
            _skillCostHandler ??= new DefaultSkillCostHandler();
            _comboData = _entity.DataModule?.Get<ComboRouteRuntimeData>();
        }

        protected override CharacterEntity GetRouteEvalActor() => Role;

        protected override void OnActionPlaySucceed(ActionConfigAsset action)
        {
            // 由多态 ActionDomain 系统权威驱动生命周期流转
            Role?.DomainContext?.HandleActionChanged(action);
        }

        protected override void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action)
        {
            _comboData?.RecordResolvedRoute(source, tag, payload, action);
        }
    }
}
