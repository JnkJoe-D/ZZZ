using UnityEngine;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#55AA88")]
    public class BBCheckStateData : DecoratorData
    {
        [Tooltip("目标状态")]
        public MonsterAIState targetState;
        
        [Tooltip("条件变化时的中断策略。")]
        public NPBehave.Stops stopsOnChange = NPBehave.Stops.IMMEDIATE_RESTART;
    }
}
