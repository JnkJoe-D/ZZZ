using UnityEngine;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#FFB366")]
    public class ChangeAIStateData : TaskData
    {
        [Tooltip("要变更的目标状态")]
        public MonsterAIState targetState;
    }
}
