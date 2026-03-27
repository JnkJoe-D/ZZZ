using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("变换轨道", "#4C7FCC", "MoveTool", 4)]
    public class TransformTrack : TrackBase
    {
        public TransformTrack()
        {
            trackName = "变换轨道";
            trackType = "TransformTrack";
        }

        public override TrackBase Clone()
        {
            TransformTrack clone = new TransformTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
