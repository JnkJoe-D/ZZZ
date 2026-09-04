namespace Game.Logic
{
    public class MonsterActionController : ActionController
    {
        private MonsterEntity Monster => (MonsterEntity)_entity;

        public MonsterActionController(MonsterEntity entity, 
                                       IRouteEventReceiver receiver = null,
                                       ISkillCostHandler skillCostHandler = null) 
            : base(entity, 
                   receiver ?? new MonsterRouteEventReceiver(),
                   skillCostHandler ?? new DefaultSkillCostHandler())
        {
        }
    }
}
