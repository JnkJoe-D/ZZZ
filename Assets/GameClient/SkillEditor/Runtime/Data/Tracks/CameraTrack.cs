using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("摄像机轨道", "#994CB2", "Camera Icon", 5)]
    public class CameraTrack : TrackBase
    {
        public CameraTrack()
        {
            trackName = "摄像机轨道";
            trackType = "CameraTrack";
        }

        public override TrackBase Clone()
        {
            CameraTrack clone = new CameraTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
