using System.Collections.Generic;
using Game.Framework;
using SkillEditor;

namespace Game.Adapters
{
    public struct SkillGlobalEvent : IGameEvent
    {
        public string EventName;
        public IReadOnlyList<SkillEventParam> Parameters;
    }

    public class SkillEventHandler : ISkillEventHandler
    {
        public void OnSkillEvent(string eventName, List<SkillEventParam> parameters)
        {
            var e = new SkillGlobalEvent
            {
                EventName = eventName,
                Parameters = parameters
            };

            EventCenter.Publish(e);
        }
    }
}
