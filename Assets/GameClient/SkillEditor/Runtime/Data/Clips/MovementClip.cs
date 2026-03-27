using System;
using UnityEngine;

namespace SkillEditor
{
    public enum ReferenceDestination
    {
        Fixed,          // 固定
        Target         // 有目标
    }

    public enum CoordinateSystem
    {
        Local,          // 局部
        World           // 世界
    }

    public enum TargetPositionType
    {
        EnemyFront,     // 敌人前侧
        EnemyBack       // 敌人后侧
    }

    public enum DisplacementType
    {
        Instant,        // 瞬时：直接赋值完成位移
        Continuous     // 连续：根据位移曲线用 cc.move 累积完成
    }

    public enum MovementCurve
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut
    }

    [Serializable]
    [ClipDefinition(typeof(TransformTrack), "移动片段")]
    public class MovementClip : ClipBase
    {
        [SkillProperty("参考目标")]
        public ReferenceDestination referenceDestination = ReferenceDestination.Fixed;

        // referenceDestination == Fixed
        [SkillProperty("参考坐标系")]
        [ShowIf("referenceDestination", ReferenceDestination.Fixed)]
        public CoordinateSystem referenceCoordinate = CoordinateSystem.Local;

        [SkillProperty("目标位置")]
        public Vector3 targetPosition;

        // referenceDestination == ReferenceTarget
        [SkillProperty("目标位置枚举")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public TargetPositionType targetPositionEnum = TargetPositionType.EnemyFront;

        [Header("通用设置")]
        [SkillProperty("位移方式")]
        public DisplacementType displacementType = DisplacementType.Continuous;

        [SkillProperty("移动曲线")]
        public MovementCurve movementCurve = MovementCurve.Linear;

        [SkillProperty("忽略的碰撞层级")]
        [ShowIf("displacementType", DisplacementType.Continuous)]
        public LayerMask ignoreLayerMask;

        public MovementClip()
        {
            clipName = "Movement Clip";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new MovementClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                referenceDestination = this.referenceDestination,
                referenceCoordinate = this.referenceCoordinate,
                targetPosition = this.targetPosition,
                targetPositionEnum = this.targetPositionEnum,
                displacementType = this.displacementType,
                movementCurve = this.movementCurve,
                ignoreLayerMask = this.ignoreLayerMask
            };
        }
    }
}
