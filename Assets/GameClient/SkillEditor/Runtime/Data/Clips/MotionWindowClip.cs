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

    public enum MotionTargetMode
    {
        None,
        AcquireAtEnter,
        RequireTarget,
        ContinuousTrack
    }

    public enum MotionConstraintBoxMode
    {
        Block,
        SnapToInside
    }

    public enum MotionWindowConstraintMode
    {
        None,
        IgnoreCollision,
        ConstraintBox
    }

    public enum MotionConstraintBoxUpdateMode
    {
        Dynamic,
        FreezeOnEnter
    }

    public enum MotionConstraintBoxLimitMode
    {
        ForwardOnly,
        LateralOnly,
        ForwardAndLateral
    }

    public enum MotionWindowLocalDeltaFilterMode
    {
        None,
        ZeroLocalX,
        ZeroLocalZ,
        ZeroLocalXZ
    }

    public enum MotionConstraintBoxFrontBoundarySource
    {
        LocalConfigured,
        TargetNearestSurface,
        TargetFarthestSurface,
        TargetBackPlusOwnerDiameter
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
        [NonSerialized] public bool HasAppliedEnterSnap;

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

        [SkillProperty("调试颜色")]
        public Color debugColor = new Color(0.22f, 0.78f, 1f, 0.75f);

        [SkillProperty("约束模式")]
        public MotionWindowConstraintMode constraintMode = MotionWindowConstraintMode.ConstraintBox;

        [SkillProperty("本地轴过滤")]
        public MotionWindowLocalDeltaFilterMode localDeltaFilterMode = MotionWindowLocalDeltaFilterMode.None;

        [Header("约束盒")]
        [Tooltip("约束盒更新方式。")]
        public MotionConstraintBoxUpdateMode constraintBoxUpdateMode = MotionConstraintBoxUpdateMode.Dynamic;

        [Tooltip("约束盒限制轴向。")]
        public MotionConstraintBoxLimitMode constraintBoxLimitMode = MotionConstraintBoxLimitMode.ForwardAndLateral;

        [Tooltip("忽略碰撞层，仅在 IgnoreCollision 模式下使用。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.IgnoreCollision)]
        public LayerMask ignoreCollisionLayers;

        [Tooltip("约束盒障碍层。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public LayerMask constraintBoxObstacleLayers;

        [Tooltip("前边界来源。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public MotionConstraintBoxFrontBoundarySource constraintBoxFrontBoundarySource =
            MotionConstraintBoxFrontBoundarySource.TargetNearestSurface;

        [Tooltip("前边界额外偏移。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public float constraintBoxFrontBoundaryOffset = 0f;

        [Tooltip("约束盒最小深度。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public float minConstraintBoxDepth = 0.6f;

        [Tooltip("角色掉到盒外时是否回收到合法边界。")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public bool recoverWhenOutside = true;

        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public bool showConstraintBoxInScene = true;

        [SkillProperty("约束盒类型")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public MotionConstraintBoxMode constraintBoxMode = MotionConstraintBoxMode.Block;

        [SkillProperty("约束盒尺寸")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public Vector3 constraintBoxSize = new Vector3(3f, 2f, 3f);

        [SkillProperty("约束盒偏移")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public Vector3 constraintBoxCenterOffset = Vector3.zero;

        [SkillProperty("动态适配前后深度")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public bool autoFitConstraintBoxDepthToTarget = true;

        [SkillProperty("后侧额外留量")]
        [ShowIf("constraintMode", MotionWindowConstraintMode.ConstraintBox)]
        public float constraintBoxBackPadding = 0f;

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
                debugColor = debugColor,
                constraintMode = constraintMode,
                constraintBoxUpdateMode = constraintBoxUpdateMode,
                constraintBoxLimitMode = constraintBoxLimitMode,
                localDeltaFilterMode = localDeltaFilterMode,
                ignoreCollisionLayers = ignoreCollisionLayers,
                constraintBoxObstacleLayers = constraintBoxObstacleLayers,
                constraintBoxFrontBoundarySource = constraintBoxFrontBoundarySource,
                constraintBoxFrontBoundaryOffset = constraintBoxFrontBoundaryOffset,
                minConstraintBoxDepth = minConstraintBoxDepth,
                recoverWhenOutside = recoverWhenOutside,
                showConstraintBoxInScene = showConstraintBoxInScene,
                constraintBoxMode = constraintBoxMode,
                constraintBoxSize = constraintBoxSize,
                constraintBoxCenterOffset = constraintBoxCenterOffset,
                autoFitConstraintBoxDepthToTarget = autoFitConstraintBoxDepthToTarget,
                constraintBoxBackPadding = constraintBoxBackPadding
            };
        }

        public bool UsesMotionReference()
        {
            return UsesAxisFilter() || UsesConstraintBox();
        }

        public bool UsesAxisFilter()
        {
            return localDeltaFilterMode != MotionWindowLocalDeltaFilterMode.None;
        }

        public bool UsesConstraintBox()
        {
            return constraintMode == MotionWindowConstraintMode.ConstraintBox;
        }

        public bool UsesIgnoreCollision()
        {
            return constraintMode == MotionWindowConstraintMode.IgnoreCollision;
        }

        public bool HasRuntimeConstraint()
        {
            return UsesAxisFilter() || UsesConstraintBox();
        }

        public MotionConstraintBoxFrontBoundarySource ResolveFrontBoundarySource()
        {
            return constraintBoxFrontBoundarySource;
        }
    }
}
