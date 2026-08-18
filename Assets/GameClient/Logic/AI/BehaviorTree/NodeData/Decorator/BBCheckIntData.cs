using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#5588FF")]
    public class BBCheckIntData : DecoratorData
    {
        public BBKey key;
        public Operator op = Operator.IS_EQUAL;
        public int value;
        public Stops stopsOnChange = Stops.NONE;
    }
}
