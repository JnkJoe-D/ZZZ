namespace Game.GamePlay
{
    public class MonsterActionController : ActionController
    {
        private MonsterEntity Monster => (MonsterEntity)_entity;

        public override void Initialize(CharacterEntity owner)
        {
            base.Initialize(owner);
            _routeEventReceiver ??= new MonsterRouteEventReceiver();
            _skillCostHandler ??= new DefaultSkillCostHandler();
        }
    }
}
