using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "格挡判定窗口")]
    public class ParryWindowClip : ClipBase
    {
        public override ClipBase Clone()
        {
            return new ParryWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled
            };
        }
    }
}
