using System;
using UnityEngine;

namespace ATEditor
{
    public enum AttackRepeatMode { OncePerWindow, OncePerSample, Cooldown }

    /// <summary>只描述空间与采样策略；伤害、阵营和配表由观察结果的消费者解释。</summary>
    [Serializable, ClipDefinition(typeof(CombatTrack), "攻击检测（通用）")]
    public sealed class AttackClip : ClipBase
    {
        [ActionProperty("检测范围")] public HitBoxScopeConfig scope = new HitBoxScopeConfig();
        [ActionProperty("采样次数"), Min(1)] public int sampleCount = 1;
        [ActionProperty("同目标重复策略")] public AttackRepeatMode repeatMode = AttackRepeatMode.OncePerWindow;
        [ActionProperty("重复间隔（动作秒）"), Min(0f)] public float cooldown = 0.2f;
        [ActionProperty("窗口最多不同目标（0不限）"), Min(0)] public int maxTargets;
        [ActionProperty("目标排序")] public TargetSortMode sortMode = TargetSortMode.Closest;
        [ActionProperty("检测层")] public LayerMask layers = -1;
        [ActionProperty("触发器")] public QueryTriggerInteraction triggers = QueryTriggerInteraction.Collide;
        [ActionProperty("允许检测自身")] public bool includeSelf;

        public AttackClip() { clipName = "攻击检测"; Duration = 0.5f; }

        public override ClipBase Clone()
        {
            return new AttackClip
            {
                clipName = clipName, isEnabled = isEnabled, StartTime = StartTime, Duration = Duration,
                scope = scope?.Clone() ?? new HitBoxScopeConfig(), sampleCount = sampleCount,
                repeatMode = repeatMode, cooldown = cooldown, maxTargets = maxTargets,
                sortMode = sortMode, layers = layers, triggers = triggers, includeSelf = includeSelf
            };
        }
    }
}
