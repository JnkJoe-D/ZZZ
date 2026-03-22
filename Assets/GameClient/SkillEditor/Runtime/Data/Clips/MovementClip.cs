using System;
using UnityEngine;

namespace SkillEditor
{
    public enum MovementType
    {
        Translation, // 纯位移
        Rotation     // 仅旋转(如朝向目标/输入)
    }

    public enum RotationTargetMode
    {
        InputDirection,  // 输入方向
        EnemyPriority    // 敌人优先
    }

    public enum RotationExecuteMode
    {
        Once,       // 进入时单次执行
        Continuous  // Update 中持续执行
    }

    [Serializable]
    [ClipDefinition(typeof(MovementTrack), "位移")]
    public class MovementClip : ClipBase
    {
        [SkillProperty("行为类型")]
        public MovementType movementType = MovementType.Translation;

        // --- Translation Fields ---
        [SkillProperty("目标位置")]
        [ShowIf("movementType", MovementType.Translation)]
        public Vector3 targetPosition;
        
        [SkillProperty("移动速度")]
        [ShowIf("movementType", MovementType.Translation)]
        public float speed = 5f;

        // --- Rotation Fields ---
        [SkillProperty("转向目标")]
        [ShowIf("movementType", MovementType.Rotation)]
        public RotationTargetMode rotationTargetMode = RotationTargetMode.EnemyPriority;

        [SkillProperty("执行方式")]
        [ShowIf("movementType", MovementType.Rotation)]
        public RotationExecuteMode rotationExecuteMode = RotationExecuteMode.Continuous;

        [SkillProperty("转向速度(-1使用默认)")]
        [ShowIf("movementType", MovementType.Rotation)]
        public float turnSpeed = -1f;

        public MovementClip()
        {
            clipName = "Movement Clip";
            duration = 1.0f;
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
                movementType = this.movementType,
                targetPosition = this.targetPosition,
                speed = this.speed,
                rotationTargetMode = this.rotationTargetMode,
                rotationExecuteMode = this.rotationExecuteMode,
                turnSpeed = this.turnSpeed
            };
        }
    }
}
