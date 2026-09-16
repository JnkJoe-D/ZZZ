using UnityEngine;
using NPBehave;

namespace Game.GamePlay 
{
    [NodeColor("#FF5555")]
    public class BBTriggerData : DecoratorData
    {
        public BBKey key;
        public Stops stopsOnChange = Stops.IMMEDIATE_RESTART;
    }
}
