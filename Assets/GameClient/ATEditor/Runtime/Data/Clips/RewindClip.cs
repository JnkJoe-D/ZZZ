using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(RewindTrack), "回溯循环")]
    public class RewindClip : ClipBase
    {
        [SkillProperty("最大回溯次数")]
        public int MaxRewindCount = 3;

        public RewindClip()
        {
            clipName = "Rewind Clip";
            duration = 1.0f; // 默认长度
        }

        public override ClipBase Clone()
        {
            return new RewindClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                MaxRewindCount = this.MaxRewindCount
            };
        }
    }
}
