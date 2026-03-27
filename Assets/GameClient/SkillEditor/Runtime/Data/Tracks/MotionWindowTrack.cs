using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("动画位移窗口轨道", "#36A9E1", "MoveTool", 5)]
    public class MotionWindowTrack : TrackBase
    {
        public override bool CanOverlap => false;

        public MotionWindowTrack()
        {
            trackName = "动画位移窗口轨道";
            trackType = "MotionWindowTrack";
        }

        public override TrackBase Clone()
        {
            MotionWindowTrack clone = new MotionWindowTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
