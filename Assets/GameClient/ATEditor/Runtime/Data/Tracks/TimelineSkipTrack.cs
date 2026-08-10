using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("时间轴跳跃轨道", "#338290", "Animation.Record", 10)]
    public class TimelineSkipTrack : TrackBase
    {
        public TimelineSkipTrack()
        {
            trackName = "时间轴跳跃轨道";
            trackType = "TimelineSkipTrack";
        }

        public override TrackBase Clone()
        {
            var clone = new TimelineSkipTrack();
            CloneBaseProperties(clone);

            foreach (var clip in this.clips)
            {
                clone.clips.Add(clip.Clone());
            }

            return clone;
        }
    }
}
