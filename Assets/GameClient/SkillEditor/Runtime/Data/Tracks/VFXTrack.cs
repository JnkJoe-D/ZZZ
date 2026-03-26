using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("特效轨道", "#CC4C4C", "Particle Effect", 1)]
    public class VFXTrack : TrackBase
    {
        public VFXTrack()
        {
            trackName = "特效轨道";
            trackType = "VFXTrack";
        }

        public override TrackBase Clone()
        {
            VFXTrack clone = new VFXTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
