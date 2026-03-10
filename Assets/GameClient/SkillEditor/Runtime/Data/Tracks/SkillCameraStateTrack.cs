using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("技能相机状态轨道", typeof(SkillCameraStateClip), "#3F8EFC", "Camera Icon", 6)]
    public class SkillCameraStateTrack : TrackBase
    {
        public SkillCameraStateTrack()
        {
            trackName = "技能相机状态轨道";
            trackType = "SkillCameraStateTrack";
        }

        public override TrackBase Clone()
        {
            var clone = new SkillCameraStateTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
