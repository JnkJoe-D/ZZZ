using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "招架执行窗口")]
    public class ParryExecuteClip : ClipBase
    {
        [ActionProperty("招架效果")]
        [Tooltip("招架反制命中效果 ID (配置失衡值、受击表现 Parried 等，支持 Luban 下拉选择)")]
        public int hitEffectId;

        [ActionProperty("招架顿帧")]
        [Tooltip("招架成功时双方的顿帧时长")]
        [Range(0.01f, 0.5f)]
        public float hitStopDuration = 0.1f;

        public override ClipBase Clone()
        {
            return new ParryExecuteClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                hitEffectId = this.hitEffectId,
                hitStopDuration = this.hitStopDuration,
            };
        }
    }
}
