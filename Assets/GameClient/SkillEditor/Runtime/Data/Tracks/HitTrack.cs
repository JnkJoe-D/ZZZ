using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("命中判定轨道",  typeof(HitClip), "#E57F33", "Animation.EventMarker", 3)]
    public class HitTrack : TrackBase
    {
        public HitTrack()
        {
            trackName = "命中判定轨道";
            trackType = "DamageTrack";
        }

        public override TrackBase Clone()
        {
            HitTrack clone = new HitTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
