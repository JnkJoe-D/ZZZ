using System;
using UnityEngine;

namespace ATEditor
{
    public enum WarningSignalType
    {
        Yellow_Parryable, // 可招架（弹刀）
        Red_Unparryable   // 不可招架（仅限闪避）
    }

    public enum AttackWeight
    {
        Light_Interruptible,  // 轻攻击，被弹刀会打断
        Heavy_Uninterruptible // 重攻击，被弹刀只顿帧，不打断
    }
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "攻击预警 (黄/红光)")]
    public class AttackWarningClip : ClipBase
    {
        [Header("Warning Type")]
        [Tooltip("黄光表示可被弹刀/招架，红光表示不可弹刀只能闪避")]
        public WarningSignalType SignalType = WarningSignalType.Yellow_Parryable;

        [Tooltip("轻攻击(可被打断) 还是 重攻击(只能局部顿帧)")]
        public AttackWeight Weight = AttackWeight.Light_Interruptible;

        [Header("Detection Area")]
        [Tooltip("检测半径（通常为攻击的最远距离）")]
        public float DetectionRadius = 5.0f;

        [Tooltip("检测角度（扇形夹角，前方为0度）")]
        public float DetectionAngle = 180.0f;

        public override ClipBase Clone()
        {
            return new AttackWarningClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                SignalType = this.SignalType,
                Weight = this.Weight,
                DetectionRadius = this.DetectionRadius,
                DetectionAngle = this.DetectionAngle
            };
        }
    }
}
