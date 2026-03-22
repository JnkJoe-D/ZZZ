using System;
using UnityEngine;

namespace SkillEditor
{
    public enum MotionReferenceMode
    {
        CharacterForwardAtEnter,
        InputDirectionAtEnter,
        TargetLineAtEnter,
        TargetLineContinuous
    }

    public enum MotionTrajectoryMode
    {
        Authored,
        ForwardOnly,
        ForwardKeepLateral
    }

    public enum MotionCharacterCollisionMode
    {
        BlockAll,
        IgnorePrimaryTarget,
        IgnoreAllCharacters
    }

    public enum MotionWorldCollisionMode
    {
        Block,
        Slide,
        Ignore
    }

    public enum MotionTargetMode
    {
        None,
        AcquireAtEnter,
        RequireTarget,
        ContinuousTrack
    }

    public enum MotionEndPlacementMode
    {
        None,
        StayCurrent,
        SnapFrontOfTarget,
        SnapBehindTarget
    }

    public enum MotionConstraintBoxMode
    {
        Block,
        SnapToInside
    }

    [Serializable]
    public class MotionWindowRuntimeData
    {
        /* 运行时快照：把窗口配置和进窗时上下文绑在一起，供 MovementController 每帧读取。 */ [NonSerialized] public MotionWindowClip Clip;
        [NonSerialized] public Transform PrimaryTarget;
        [NonSerialized] public Collider PrimaryTargetCollider;
        [NonSerialized] public Vector3 ReferenceForward = Vector3.forward;
        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public Quaternion StartRotation = Quaternion.identity;
        [NonSerialized] public Vector3 EnterCapsuleCenter;
        [NonSerialized] public float EnterTime;
        [NonSerialized] public int EnterOrder;
        [NonSerialized] public bool HasFrozenConstraintBox;
        [NonSerialized] public MotionConstraintBoxData FrozenConstraintBox;

        public bool HasTarget => PrimaryTarget != null;
    }

    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "位移窗口")]
    public class MotionWindowClip : ClipBase
    {
        /* 这个 Clip 不直接规定角色怎么位移，而是定义“这段时间如何解释动画根运动”。 */ [Header("Motion Window")]
        [SkillProperty("目标模式")]
        public MotionTargetMode targetMode = MotionTargetMode.AcquireAtEnter;

        [SkillProperty("参考方向")]
        public MotionReferenceMode referenceMode = MotionReferenceMode.TargetLineAtEnter;

        [SkillProperty("轨迹模式")]
        public MotionTrajectoryMode trajectoryMode = MotionTrajectoryMode.ForwardOnly;

        [SkillProperty("角色碰撞")]
        public MotionCharacterCollisionMode characterCollisionMode = MotionCharacterCollisionMode.BlockAll;

        [SkillProperty("世界碰撞")]
        public MotionWorldCollisionMode worldCollisionMode = MotionWorldCollisionMode.Block;

        [SkillProperty("结束落点")]
        public MotionEndPlacementMode endPlacementMode = MotionEndPlacementMode.None;

        [SkillProperty("前向缩放")]
        public float forwardScale = 1f;

        [SkillProperty("横向缩放")]
        public float lateralScale = 1f;

        [SkillProperty("停靠距离")]
        public float stopDistance = 0.15f;

        [SkillProperty("穿越偏移")]
        public float passThroughOffset = 1f;

        [SkillProperty("持续跟随转向")]
        [ShowIf("referenceMode", MotionReferenceMode.TargetLineContinuous)]
        public float continuousTurnRate = 30f;

        [SkillProperty("角色阻挡层")]
        public LayerMask characterBlockLayers;

        [SkillProperty("世界阻挡层")]
        public LayerMask worldBlockLayers;

        [SkillProperty("调试颜色")]
        public Color debugColor = new Color(0.22f, 0.78f, 1f, 0.75f);

        [SkillProperty("启用约束盒")]
        public bool enableConstraintBox;

        [ShowIf("enableConstraintBox", true)]
        public bool showConstraintBoxInScene = true;

        [SkillProperty("前边界贴目标碰撞体")]
        [ShowIf("enableConstraintBox", true)]
        public bool alignConstraintBoxFrontToTarget = true;

        [SkillProperty("约束盒类型")]
        [ShowIf("enableConstraintBox", true)]
        public MotionConstraintBoxMode constraintBoxMode = MotionConstraintBoxMode.Block;

        [SkillProperty("约束盒尺寸")]
        [ShowIf("enableConstraintBox", true)]
        public Vector3 constraintBoxSize = new Vector3(3f, 2f, 3f);

        [SkillProperty("约束盒偏移")]
        [ShowIf("enableConstraintBox", true)]
        public Vector3 constraintBoxCenterOffset = Vector3.zero;

        [SkillProperty("动态适配前后深度")]
        [ShowIf("enableConstraintBox", true)]
        public bool autoFitConstraintBoxDepthToTarget = true;

        [SkillProperty("后侧额外留量")]
        [ShowIf("enableConstraintBox", true)]
        public float constraintBoxBackPadding = 0f;

        [SkillProperty("沿目标方向的落点重置")]
        public bool projectEndPositionToReferenceLine;

        public MotionWindowClip()
        {
            clipName = "位移窗口";
            duration = 0.3f;
        }

        public override ClipBase Clone()
        {
            return new MotionWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                targetMode = targetMode,
                referenceMode = referenceMode,
                trajectoryMode = trajectoryMode,
                characterCollisionMode = characterCollisionMode,
                worldCollisionMode = worldCollisionMode,
                endPlacementMode = endPlacementMode,
                forwardScale = forwardScale,
                lateralScale = lateralScale,
                stopDistance = stopDistance,
                passThroughOffset = passThroughOffset,
                continuousTurnRate = continuousTurnRate,
                characterBlockLayers = characterBlockLayers,
                worldBlockLayers = worldBlockLayers,
                debugColor = debugColor,
                enableConstraintBox = enableConstraintBox,
                showConstraintBoxInScene = showConstraintBoxInScene,
                alignConstraintBoxFrontToTarget = alignConstraintBoxFrontToTarget,
                constraintBoxMode = constraintBoxMode,
                constraintBoxSize = constraintBoxSize,
                constraintBoxCenterOffset = constraintBoxCenterOffset,
                autoFitConstraintBoxDepthToTarget = autoFitConstraintBoxDepthToTarget,
                constraintBoxBackPadding = constraintBoxBackPadding,
                projectEndPositionToReferenceLine = projectEndPositionToReferenceLine
            };
        }
    }
}
