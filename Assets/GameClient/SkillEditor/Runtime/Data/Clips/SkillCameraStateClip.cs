using System;

namespace SkillEditor
{
    /// <summary>
    /// Keeps a named CinemachineSkillDrivenCamera state active for the clip duration.
    /// </summary>
    [Serializable]
    public class SkillCameraStateClip : ClipBase
    {
        [SkillProperty("相机编号")]
        public int sikllCameraId;
        [SkillProperty("State")]
        public string stateName = string.Empty;

        [SkillProperty("Priority")]
        public int priority = 0;
        public override float Duration { get => 0.1f; set => duration=0.1f; }

        public SkillCameraStateClip()
        {
            clipName = "Skill Camera State";
        }

        public override ClipBase Clone()
        {
            return new SkillCameraStateClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                stateName = this.stateName,
                priority = this.priority
            };
        }
    }
}
