using System;
using UnityEngine;

namespace ATEditor
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
        [InspectorName("敌人前侧")]
        EnemyFront,
        [InspectorName("敌人后侧")]
        EnemyBack,
        [InspectorName("敌人左侧")]
        EnemyLeft,
        [InspectorName("敌人右侧")]
        EnemyRight,
        [InspectorName("自定义角度")]
        CustomAngle,
        [InspectorName("输入方向")]
        InputDirection,
        [InspectorName("智能多级候选列表")]
        CandidateList
    }

    public enum CandidatePositionType
    {
        [InspectorName("敌人前侧")]
        EnemyFront,
        [InspectorName("敌人后侧")]
        EnemyBack,
        [InspectorName("敌人左侧")]
        EnemyLeft,
        [InspectorName("敌人右侧")]
        EnemyRight,
        [InspectorName("自定义角度")]
        CustomAngle,
        [InspectorName("输入方向")]
        InputDirection
    }

    public enum TargetBaseDirection
    {
        [InspectorName("角色-敌人连线")]
        LineOfSight,
        [InspectorName("敌人自身朝向")]
        TargetFacing
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

    /// <summary>
    /// 目标候选位置策略配置项
    /// </summary>
    [Serializable]
    public class MovementPositionCandidate
    {
        [Tooltip("候选点备注标签 (如: 首选身后 / 备选左侧)")]
        public string label;

        [Tooltip("目标位置模式")]
        public CandidatePositionType targetPositionEnum = CandidatePositionType.EnemyBack;

        [Tooltip("基准朝向")]
        public TargetBaseDirection targetBaseDirection = TargetBaseDirection.LineOfSight;

        [Tooltip("额外半径偏移 (最终距离 = 目标半径 + 自身半径 + 偏移)")]
        public float offsetRadius = 0f;

        [Tooltip("自定义角度(度)")]
        public float angleOffset = 0f;

        [Tooltip("高度偏移")]
        public float heightOffset = 0f;

        public MovementPositionCandidate() { }

        public MovementPositionCandidate(string label, CandidatePositionType posType, TargetBaseDirection baseDir = TargetBaseDirection.LineOfSight, float radius = 0f, float angle = 0f, float height = 0f)
        {
            this.label = label;
            this.targetPositionEnum = posType;
            this.targetBaseDirection = baseDir;
            this.offsetRadius = radius;
            this.angleOffset = angle;
            this.heightOffset = height;
        }

        public MovementPositionCandidate Clone()
        {
            return new MovementPositionCandidate
            {
                label = this.label,
                targetPositionEnum = this.targetPositionEnum,
                targetBaseDirection = this.targetBaseDirection,
                offsetRadius = this.offsetRadius,
                angleOffset = this.angleOffset,
                heightOffset = this.heightOffset
            };
        }
    }

    [Serializable]
    [ClipDefinition(typeof(TransformTrack), "移动")]
    public class MovementClip : ClipBase, ISerializationCallbackReceiver
    {
        [SkillProperty("参考目标")]
        public ReferenceDestination referenceDestination = ReferenceDestination.Fixed;

        // referenceDestination == Fixed
        [SkillProperty("参考坐标系")]
        [ShowIf("referenceDestination", ReferenceDestination.Fixed)]
        public CoordinateSystem referenceCoordinate = CoordinateSystem.Local;

        [SkillProperty("目标位置")]
        [ShowIf("referenceDestination", ReferenceDestination.Fixed)]
        public Vector3 targetPosition;

        // referenceDestination == Target
        [SkillProperty("目标位置模式")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public TargetPositionType targetPositionEnum = TargetPositionType.EnemyFront;

        [SkillProperty("基准朝向")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public TargetBaseDirection targetBaseDirection = TargetBaseDirection.LineOfSight;

        [SkillProperty("额外半径偏移")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public float offsetRadius = 0f;

        [SkillProperty("自定义角度(度)")]
        [ShowIf("targetPositionEnum", TargetPositionType.CustomAngle)]
        public float angleOffset = 0f;

        [Header("通用设置")]
        [SkillProperty("位移方式")]
        public DisplacementType displacementType = DisplacementType.Continuous;

        [SkillProperty("移动曲线")]
        public MovementCurve movementCurve = MovementCurve.Linear;

        [SkillProperty("忽略的碰撞层级")]
        [ShowIf("displacementType", DisplacementType.Continuous)]
        public LayerMask ignoreLayerMask;

        [Header("位置校验与智能寻位")]
        [SkillProperty("启用位置可用性校验")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public bool enablePositionValidation = false;

        [SkillProperty("候选目标位置列表")]
        [ShowIf("enablePositionValidation", true)]
        public MovementPositionCandidate[] candidatePositions = new MovementPositionCandidate[0];

        [SkillProperty("启用径向环形扩散搜索")]
        [ShowIf("enablePositionValidation", true)]
        public bool enableSmartRadialFallback = true;

        [SkillProperty("搜索角度步长(度)")]
        [ShowIf("enablePositionValidation", true)]
        public float fallbackAngleStep = 30f;

        [SkillProperty("最大搜索角度(度)")]
        [ShowIf("enablePositionValidation", true)]
        public float maxFallbackAngle = 180f;

        [SkillProperty("障碍物检测层级")]
        [ShowIf("enablePositionValidation", true)]
        public LayerMask obstacleLayers;

        [SkillProperty("地面检测层级")]
        [ShowIf("enablePositionValidation", true)]
        public LayerMask groundLayers;

        [SkillProperty("地面检测最大距离")]
        [ShowIf("enablePositionValidation", true)]
        public float groundCheckDistance = 3.0f;

        [SkillProperty("必须贴合有效地面")]
        [ShowIf("enablePositionValidation", true)]
        public bool requireGrounded = true;

        [SkillProperty("位移后自动面向目标")]
        [ShowIf("referenceDestination", ReferenceDestination.Target)]
        public bool faceTargetOnArrival = true;

        [SerializeField, HideInInspector]
        private int serializedIgnoreLayerMask;
        [SerializeField, HideInInspector]
        private int serializedObstacleLayers;
        [SerializeField, HideInInspector]
        private int serializedGroundLayers;

        public MovementClip()
        {
            clipName = "Movement Clip";
            duration = 0.5f;
            obstacleLayers = 1; // Default layer (1 << 0)
            groundLayers = 1;   // Default layer (1 << 0)
        }

        public override ClipBase Clone()
        {
            MovementPositionCandidate[] clonedCandidates = null;
            if (this.candidatePositions != null)
            {
                clonedCandidates = new MovementPositionCandidate[this.candidatePositions.Length];
                for (int i = 0; i < this.candidatePositions.Length; i++)
                {
                    clonedCandidates[i] = this.candidatePositions[i]?.Clone();
                }
            }

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
                targetBaseDirection = this.targetBaseDirection,
                offsetRadius = this.offsetRadius,
                angleOffset = this.angleOffset,
                displacementType = this.displacementType,
                movementCurve = this.movementCurve,
                ignoreLayerMask = this.ignoreLayerMask,
                enablePositionValidation = this.enablePositionValidation,
                candidatePositions = clonedCandidates,
                enableSmartRadialFallback = this.enableSmartRadialFallback,
                fallbackAngleStep = this.fallbackAngleStep,
                maxFallbackAngle = this.maxFallbackAngle,
                obstacleLayers = this.obstacleLayers,
                groundLayers = this.groundLayers,
                groundCheckDistance = this.groundCheckDistance,
                requireGrounded = this.requireGrounded,
                faceTargetOnArrival = this.faceTargetOnArrival
            };
        }

        public void OnBeforeSerialize()
        {
            serializedIgnoreLayerMask = ignoreLayerMask.value;
            serializedObstacleLayers = obstacleLayers.value;
            serializedGroundLayers = groundLayers.value;
        }

        public void OnAfterDeserialize()
        {
            ignoreLayerMask.value = serializedIgnoreLayerMask;
            obstacleLayers.value = serializedObstacleLayers;
            groundLayers.value = serializedGroundLayers;
        }
    }
}
