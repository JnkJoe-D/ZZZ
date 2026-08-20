namespace Game.Logic
{
    public class MonsterActionController : ActionController
    {
        private MonsterEntity Monster => (MonsterEntity)_entity;

        public MonsterActionController(MonsterEntity entity) : base(entity)
        {
        }
    }
}
