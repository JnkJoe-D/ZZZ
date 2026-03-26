using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("移动轨道", "#4C7FCC", "MoveTool", 4)]
    public class MovementTrack : TrackBase
    {
        public MovementTrack()
        {
            trackName = "移动轨道";
            trackType = "MovementTrack";
        }

        public override TrackBase Clone()
        {
            MovementTrack clone = new MovementTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
