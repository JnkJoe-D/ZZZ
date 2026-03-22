using System;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 相机脉冲片段数据
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "相机脉冲")]
    public class CameraImpluseClip : ClipBase
    {
        [SkillProperty("冲击力")]
        public Vector3 velocity = Vector3.zero;

        [SkillProperty("强度系数")]
        public float force = 0.1f;

        public CameraImpluseClip()
        {
            clipName = "Camera Impulse Clip";
        }

        public override ClipBase Clone()
        {
            return new CameraImpluseClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                velocity = this.velocity,
                force = this.force
            };
        }
    }
}
