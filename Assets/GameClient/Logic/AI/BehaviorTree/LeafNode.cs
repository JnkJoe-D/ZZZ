using System;

namespace Game.Logic.AI.BehaviorTree
{
    public abstract class LeafNode : Node
    {
        protected override void DoStart() { }
        protected override void DoStop() { }
    }
}
