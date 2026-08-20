using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#FFB366")]
    public class EvaluateStateData : TaskData
    {
        [Tooltip("默认周旋距离")]
        public float chaseDistance = 5f;
    }
}
