using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#5588FF")]
    public class BBCheckBoolData : DecoratorData
    {
        public BBKey key;
        public Operator op = Operator.IS_EQUAL;
        public bool value = true;
        public Stops stopsOnChange = Stops.NONE;
    }
}
