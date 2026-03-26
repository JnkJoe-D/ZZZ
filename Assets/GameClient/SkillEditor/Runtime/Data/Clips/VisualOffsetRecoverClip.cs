using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "视觉偏移矫正")]
    public class VisualOffsetRecoverClip : ClipBase
    {
        [SkillProperty("强制回正速度")]
        [Tooltip("当动画位移不足以回正时，提供的最小向心速度。")]
        public float recoverySpeed = 2f;

        public VisualOffsetRecoverClip()
        {
            clipName = "视觉偏移矫正";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new VisualOffsetRecoverClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                recoverySpeed = recoverySpeed
            };
        }
    }
}
