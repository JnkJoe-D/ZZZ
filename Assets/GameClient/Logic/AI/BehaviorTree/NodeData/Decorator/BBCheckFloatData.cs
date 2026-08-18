using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#5588FF")]
    public class BBCheckFloatData : DecoratorData
    {
        public BBKey key;
        public Operator op = Operator.IS_EQUAL;
        public float value;
        public float tolerance = 0.0001f;
        public Stops stopsOnChange = Stops.NONE;
    }
}
