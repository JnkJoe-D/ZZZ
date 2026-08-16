namespace Game.Logic.AI.BehaviorTree
{
    public abstract class MonsterTaskNode : LeafNode
    {
        private AIContext _injectedContext;
        
        protected AIContext Context 
        { 
            get 
            {
                if (_injectedContext != null) return _injectedContext;
                if (Tree != null && Tree.blackboard != null)
                {
                    return Tree.blackboard.Get<AIContext>("Context");
                }
                return null;
            }
        }
        
        public MonsterTaskNode() {}
        public MonsterTaskNode(AIContext context) { _injectedContext = context; }
    }
}
