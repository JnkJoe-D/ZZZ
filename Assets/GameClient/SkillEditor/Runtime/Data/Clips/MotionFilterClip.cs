using System;
using UnityEngine;

namespace SkillEditor
{
    public enum MotionWindowLocalDeltaFilterMode
    {
        None,
        ZeroLocalX,
        ZeroLocalZ,
        ZeroLocalXZ
    }
    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "位移窗口")]
    public class MotionFilterClip : ClipBase
    {
        [SkillProperty("局部向轴变化过滤")]
        public MotionWindowLocalDeltaFilterMode localDeltaFilterMode = MotionWindowLocalDeltaFilterMode.None;
        public MotionFilterClip()
        {
            clipName = "位移窗口";
            duration = 0.3f;
        }

        public override ClipBase Clone()
        {
            return new MotionFilterClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                localDeltaFilterMode = localDeltaFilterMode,
            };
        }
    }
}
