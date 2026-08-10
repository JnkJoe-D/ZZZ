using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("回溯循环轨道", "#E91E63", "d_Refresh", 10)]
    public class RewindTrack : TrackBase
    {
        public RewindTrack()
        {
            trackName = "回溯循环轨道";
            trackType = "RewindTrack";
        }

        public override TrackBase Clone()
        {
            var clone = new RewindTrack();
            CloneBaseProperties(clone);

            foreach (var clip in this.clips)
            {
                clone.clips.Add(clip.Clone());
            }

            return clone;
        }
    }
}
