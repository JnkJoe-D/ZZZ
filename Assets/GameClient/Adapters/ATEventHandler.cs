using System.Collections.Generic;
using Game.Framework;
using ATEditor;

namespace Game.Adapters
{
    public struct ATGlobalEvent : IGameEvent
    {
        public string EventName;
        public IReadOnlyList<ATEventParam> Parameters;
    }

    public class ATEventHandler : IEventHandler
    {
        public void OnActionTimelineEvent(string eventName, List<ATEventParam> parameters)
        {
            var e = new ATGlobalEvent
            {
                EventName = eventName,
                Parameters = parameters
            };

            EventCenter.Publish(e);
        }
    }
}
