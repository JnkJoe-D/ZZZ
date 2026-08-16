using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#3399CC")]
    public class SetStateNode : LeafNode
    {
        [Tooltip("要切换到的AI状态")]
        public AIState targetState;

        protected override void DoStart()
        {
            if (Tree != null && Tree.blackboard != null)
            {
                Tree.blackboard.Set("AIState", targetState);
            }
            Stopped(true);
        }

        protected override void DoStop()
        {
        }
    }
}
