using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "格挡判定窗口")]
    public class ParryWindowClip : ClipBase
    {
        [SkillProperty("基础/轻招架效果")]
        [Tooltip("招架反制命中效果 ID (配置失衡值、受击表现 Parried 等，支持 Luban 下拉选择；在自适应起手片段中作为轻招架效果)")]
        public int hitEffectId;

        [SkillProperty("基础/轻招架顿帧")]
        [Tooltip("招架成功时双方的顿帧时长 (秒)，支持在 Inspector 中微调打击感；在自适应起手片段中作为轻招架顿帧")]
        [Range(0.01f, 0.5f)]
        public float hitStopDuration = 0.1f;

        [SkillProperty("重招架效果 (可选)")]
        [Tooltip("重招架反制命中效果 ID (仅自适应起手片段需配置；若为 0，则退回使用基础 hitEffectId)")]
        public int heavyHitEffectId;

        [SkillProperty("重招架顿帧 (可选)")]
        [Tooltip("重招架顿帧时长 (秒，仅自适应起手片段需配置；若 <= 0，则退回使用基础 hitStopDuration)")]
        [Range(0f, 0.5f)]
        public float heavyHitStopDuration = 0.14f;

        public override ClipBase Clone()
        {
            return new ParryWindowClip
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
