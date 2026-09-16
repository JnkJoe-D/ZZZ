using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(TimelineSkipTrack), "跳跃片段")]
    public class TimelineSkipClip : ClipBase
    {
        [SkillProperty("跳跃条件 (TimelineSkip)")]
        [Tooltip("如果 ProcessContext.Flags 中包含该标记，则【取消跳跃】（即有标记不跳，没标记跳）")]
        public string CancelFlag = "TimelineSkip";

        public TimelineSkipClip()
        {
            clipName = "Timeline Skip Clip";
            duration = 0.1f; // 默认长度
        }

        public override ClipBase Clone()
        {
            return new TimelineSkipClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                CancelFlag = this.CancelFlag
            };
        }
    }
}
