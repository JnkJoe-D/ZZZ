using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "招架捕获窗口")]
    public class ParryCaptureClip : ClipBase
    {
        public override ClipBase Clone()
        {
            return new ParryCaptureClip
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
