using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("视觉轨道", "#808080", "d_ViewToolOrbit", 20)]
    public class VisualTrack : TrackBase
    {
        public VisualTrack()
        {
            trackName = "视觉轨道";
            trackType = "VisualTrack";
        }

        public override TrackBase Clone()
        {
            var clone = new VisualTrack();
            CloneBaseProperties(clone);
            foreach (var clip in this.clips)
            {
                clone.clips.Add(clip.Clone());
            }
            return clone;
        }
    }
}
