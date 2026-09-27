using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 动画位移轨道上的视觉模型旋转偏移窗口片段
    /// 解决如 TurnBack 等动作中“物理胶囊体即刻转向就位”与“模型骨骼反向补偿防止二次叠加闪现”的核心解耦问题。
    /// 在进入窗口瞬间对齐父节点，同时给视觉模型注入相反的偏航补偿，并在窗口期内平滑回正归零。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "视觉旋转偏移")]
    public class VisualRotationOffsetClip : ClipBase
    {
        [ActionProperty("进入时根节点对齐输入")]
        [Tooltip("进入窗口瞬间，强制将父节点 GameObject 瞬时旋转对齐到当前摇杆/按键输入方向（物理权威立即就位）。")]
        public bool snapRootOnEnter = true;

        [ActionProperty("模型反向补偿偏航角")]
        [Tooltip("进入窗口时注入视觉模型节点的初始反向偏航补偿角（度）。TurnBack 动作通常为 -180 度，抵消父节点的瞬时掉头。")]
        public float initialCounterYaw = -180f;

        [ActionProperty("窗口期内平滑回正")]
        [Tooltip("是否在窗口时间内按照淡出曲线自动将模型偏航补偿平滑回正到 0 度。")]
        public bool autoRecoverInWindow = true;

        [ActionProperty("回正淡出曲线")]
        [Tooltip("窗口时间内模型补偿角的回正曲线。时间 0~1，值 1.0 (全额补偿) 逐渐衰减至 0.0 (无补偿)。")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public VisualRotationOffsetClip()
        {
            clipName = "视觉旋转偏移";
            duration = 0.35f;
            blendOutDuration = 0.05f;
        }

        public override ClipBase Clone()
        {
            return new VisualRotationOffsetClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                snapRootOnEnter = snapRootOnEnter,
                initialCounterYaw = initialCounterYaw,
                autoRecoverInWindow = autoRecoverInWindow,
                fadeCurve = new AnimationCurve(fadeCurve.keys),
                blendOutDuration = blendOutDuration
            };
        }
    }
}
