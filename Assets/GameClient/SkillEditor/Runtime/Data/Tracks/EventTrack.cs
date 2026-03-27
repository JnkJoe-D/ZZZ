using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("事件轨道", "#9C27B0", "d_EventSystem Icon", 5)]
    public class EventTrack : TrackBase
    {
        public EventTrack()
        {
            trackName = "事件轨道";
            trackType = "EventTrack";
        }

        public override TrackBase Clone()
        {
            EventTrack clone = new EventTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
