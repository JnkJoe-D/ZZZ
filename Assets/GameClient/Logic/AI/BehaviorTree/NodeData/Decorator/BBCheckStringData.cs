using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#5588FF")]
    public class BBCheckStringData : DecoratorData
    {
        public BBKey key;
        public Operator op = Operator.IS_EQUAL;
        public string value;
        public Stops stopsOnChange = Stops.NONE;
    }
}
