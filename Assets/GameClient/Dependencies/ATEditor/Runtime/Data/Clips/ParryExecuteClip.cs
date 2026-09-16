using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "招架执行窗口")]
    public class ParryExecuteClip : ClipBase
    {
        [SkillProperty("基础/轻招架效果")]
        [Tooltip("招架反制命中效果 ID (配置失衡值、受击表现 Parried 等，支持 Luban 下拉选择)")]
        public int hitEffectId;

        [SkillProperty("基础/轻招架顿帧")]
        [Tooltip("招架成功时双方的顿帧时长 (秒)，支持在 Inspector 中微调打击感")]
        [Range(0.01f, 0.5f)]
        public float hitStopDuration = 0.1f;

        [SkillProperty("重招架效果 (可选)")]
        [Tooltip("重招架反制命中效果 ID (若为 0，则退回使用基础 hitEffectId)")]
        public int heavyHitEffectId;

        [SkillProperty("重招架顿帧 (可选)")]
        [Tooltip("重招架顿帧时长 (秒，若 <= 0，则退回使用基础 hitStopDuration)")]
        [Range(0f, 0.5f)]
        public float heavyHitStopDuration = 0.14f;

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
                heavyHitEffectId = this.heavyHitEffectId,
                heavyHitStopDuration = this.heavyHitStopDuration
            };
        }
    }
}
