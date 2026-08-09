using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 纯程序化脚部自适应接地 IK 模块 (Generic Skeleton Procedural Foot Grounder)
    /// 专为通用型骨骼设计，无需依赖 Unity Animation Rigging 或手动 K 动画曲线。
    /// 
    /// 核心能力：
    /// 1. 自动智能检索通用骨骼层级 (Pelvis / Thigh / Calf / Foot)
    /// 2. 自动测量角色自然脚踝高度 (Auto Measured Ankle Height)，彻底解决脚陷地面问题
    /// 3. 固定鞋底固有基准面贴合 (Rigid Sole Plane Alignment)，彻底解决动画瞬时姿态/单触点导致的鞋底翘起和旋转畸变
    /// 4. 脚部世界接触点定点锁定 (Planted Foot World Lock)，彻底消除待机时脚底左右滑移和摇摆
    /// 5. 动态骨盆平滑自适应下沉 (Pelvis Dynamic Spring Offset)
    /// 6. 解析式两骨骼余弦定理 IK 解算 (Analytic Two-Bone IK Solver)
    /// 7. 纯程序化离地高度/垂直速度启发式动态权重判定 (Procedural Stance/Swing Weight)
    /// </summary>
    [DisallowMultipleComponent]
    public class FootIKModule : MonoBehaviour
    {
        #region Serialized Bone References
        [Header("通用骨骼引用 (若为空将在初始化时自动检索)")]
        [SerializeField] private Transform _pelvisBone;
        [SerializeField] private Transform _leftThighBone;
        [SerializeField] private Transform _leftCalfBone;
        [SerializeField] private Transform _leftFootBone;
        [SerializeField] private Transform _rightThighBone;
        [SerializeField] private Transform _rightCalfBone;
        [SerializeField] private Transform _rightFootBone;
        #endregion

        #region Raycast & Grounding Settings
        [Header("地面检测配置")]
        [Tooltip("地面碰撞层级")]
        [SerializeField] private LayerMask _groundLayer = ~0;

        [Tooltip("射线检测总长度 (米)，建议 1.2m 以上以覆盖陡坡高度差")]
        [SerializeField] private float _raycastDistance = 1.2f;

        [Tooltip("射线起点相对于脚踝骨骼的向上偏移量 (米)，建议 0.65m 以上以穿透高位斜坡地表")]
        [SerializeField] private float _raycastUpOffset = 0.65f;

        [Tooltip("手动微调鞋底高度偏移 (米，正数垫高，负数降低，默认为 0)")]
        [SerializeField] private float _footHeightOffset = 0.0f;

        [Tooltip("检测脚掌旋转对齐的最大坡度角 (度)")]
        [SerializeField] private float _maxGroundAngle = 45f;
        #endregion

        #region Foot Locking (Anti-Sway)
        [Header("待机脚部定点锁定 (防左右晃动与滑步)")]
        [Tooltip("是否开启脚底世界坐标定点锁定 (仅在 Idle 待机且无位移时生效，技能/移动时自动释放)")]
        [SerializeField] private bool _enableFootPlantLock = true;

        [Tooltip("脚部锚点最大容差距离 (米)，角色移动或迈步超过此距离时自动释放锚点")]
        [SerializeField] private float _maxPlantDistance = 0.25f;
        #endregion

        #region Skill & Action Adaptation
        [Header("技能与发力动作适配 (Skill & Action Adaptation)")]
        [Tooltip("是否在角色释放技能/攻击/受击时保持脚部接地 IK (关闭则在技能期间平滑回退至纯动画)")]
        [SerializeField] private bool _enableIKInSkill = true;

        [Tooltip("技能期间 Foot IK 权重乘数 (建议 0.7~0.85，兼顾斜坡贴地与发力动作张力)")]
        [Range(0f, 1f)]
        [SerializeField] private float _skillIKWeight = 0.8f;

        [Tooltip("保留动画原始偏航与发力扭转 (Preserve Animation Twist)：脚掌贴坡时 100% 跟随小腿与动画发力旋转，彻底消除麻花扭曲")]
        [SerializeField] private bool _preserveAnimationTwist = true;
        #endregion

        #region Predictive Trajectory Settings
        [Header("前向轨迹预测与落地自适应 (Predictive Foot Placement)")]
        [Tooltip("是否启用前向速度外推预测 (大幅优化奔跑/上下台阶与斜坡的贴合顺滑度)")]
        [SerializeField] private bool _enablePrediction = true;

        [Tooltip("前向预测时间窗口 (秒)，建议 0.04s ~ 0.08s (约 2~5 帧)")]
        [Range(0.01f, 0.15f)]
        [SerializeField] private float _predictionTime = 0.06f;

        [Tooltip("预测速度指数平滑速率 (防止动画采样微小抖动)")]
        [SerializeField] private float _velocitySmoothSpeed = 25f;

        [Tooltip("落地法线与高度提前自适应比例 (0~1)，脚掌在触地前提前偏转对齐前方坡度")]
        [Range(0f, 1f)]
        [SerializeField] private float _landingAnticipation = 0.7f;
        #endregion

        #region Procedural Weight Heuristics (Slope-Aware)
        [Header("斜坡自适应抬腿权重 (Slope-Aware Procedural Weight)")]
        [Tooltip("脚踝相对下方地面接触点的垂直距离低于此值时判定为踩地支撑 (米)")]
        [SerializeField] private float _groundDistanceThreshold = 0.10f;

        [Tooltip("脚踝相对下方地面接触点的垂直距离高于此值时 IK 权重完全归零 (米)")]
        [SerializeField] private float _maxLiftOffDistance = 0.30f;

        [Tooltip("脚向上离开地面的垂直速度超过此阈值时加速释放 IK (米/秒)")]
        [SerializeField] private float _liftVelocityThreshold = 0.30f;
        #endregion

        #region Suspension & Smoothing
        [Header("启用控制")]
        [Tooltip("是否启用 Foot IK (暂时全局禁用)")]
        [SerializeField] private bool _enableIK = false;

        [Header("平滑与悬挂阻尼")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterWeight = 1.0f;

        [Tooltip("斜坡骨盆下沉平衡比例 (0~1)：0=骨盆不动纯靠双腿屈膝抬升与伸展，0.5=骨盆下沉50%落差且高位腿屈膝抬升50%落差(推荐)，1=骨盆全额下沉")]
        [Range(0f, 1f)]
        [SerializeField] private float _pelvisSlopeBalance = 0.5f;

        [Tooltip("骨盆高度调整的平滑阻尼速度")]
        [SerializeField] private float _pelvisDamping = 16f;

        [Tooltip("脚部位置追踪的平滑阻尼速度")]
        [SerializeField] private float _footPositionDamping = 35f;

        [Tooltip("脚底旋转对齐的平滑阻尼速度")]
        [SerializeField] private float _footRotationDamping = 20f;
        #endregion

        #region Debug Settings
        [Header("调试可视化")]
        [SerializeField] private bool _drawGizmos = false;
        #endregion

        #region Runtime State
        private CharacterEntity _entity;
        private Coroutine _fadeCoroutine;

        // 自动测量的脚踝自然高度（脚踝到地面的垂直距离）
        private float _leftAnkleRestHeight = 0.12f;
        private float _rightAnkleRestHeight = 0.12f;

        // 固定的鞋底固有基准局部旋转（相对于角色根节点），保证鞋底无论在什么帧踩地都保持水平贴合
        private Quaternion _leftFootRestLocalRot = Quaternion.identity;
        private Quaternion _rightFootRestLocalRot = Quaternion.identity;

        private float _currentPelvisOffset;
        private float _lastLeftFootY;
        private float _lastRightFootY;

        // 脚底世界位置与前向外推速度追踪
        private Vector3 _lastLeftFootWorldPos;
        private Vector3 _lastRightFootWorldPos;
        private Vector3 _smoothLeftFootVel;
        private Vector3 _smoothRightFootVel;

        // 调试可视化数据 (预测落点)
        private Vector3 _debugLeftPredictPos;
        private Vector3 _debugRightPredictPos;
        private Vector3 _debugLeftPredictHitPos;
        private Vector3 _debugRightPredictHitPos;
        private bool _debugLeftPredictHit;
        private bool _debugRightPredictHit;

        // 脚底世界锚点 (Planted Anchors)
        private bool _isLeftPlanted;
        private bool _isRightPlanted;
        private Vector3 _leftPlantPos;
        private Vector3 _rightPlantPos;
        private Quaternion _leftPlantRot;
        private Quaternion _rightPlantRot;

        private Vector3 _curLeftTargetPos;
        private Vector3 _curRightTargetPos;
        private Quaternion _curLeftTargetRot;
        private Quaternion _curRightTargetRot;

        private readonly RaycastHit[] _rayHits = new RaycastHit[8];

        public bool EnableIK
        {
            get => _enableIK;
            set
            {
                _enableIK = value;
                if (!_enableIK)
                {
                    ResetRuntimeState();
                }
            }
        }
        public float CurrentWeight => _masterWeight;
        public bool HasBones => _pelvisBone != null && _leftFootBone != null && _rightFootBone != null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureGroundLayerMask();
            AutoDiscoverBonesIfMissing();
            MeasureAnkleRestBaselines();
        }

        private void Start()
        {
            InitializeRuntimeState();
        }

        private void OnDisable()
        {
            ResetRuntimeState();
        }

        private void LateUpdate()
        {
            if (!_enableIK || _masterWeight <= 0.001f || _pelvisBone == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            EvaluateFootIK(deltaTime);
        }
        #endregion

        #region Initialization
        public void Init(CharacterEntity entity)
        {
            _entity = entity;
            _enableIK = true;
            _masterWeight = 1.0f;
            EnsureGroundLayerMask();
            AutoDiscoverBonesIfMissing();
            MeasureAnkleRestBaselines();
            InitializeRuntimeState();
        }

        private void EnsureGroundLayerMask()
        {
            if (_groundLayer.value == 0 || _groundLayer.value == ~0)
            {
                int excludeMask = LayerMask.GetMask("Ignore Raycast", "UI", "CharHit", "Character", "LocalRole", "Camera", "CharIgnore", "Water");
                if (excludeMask != 0)
                {
                    _groundLayer = ~excludeMask;
                }
                else
                {
                    _groundLayer = LayerMask.GetMask("Default", "Ground", "Wall");
                    if (_groundLayer.value == 0) _groundLayer = ~0;
                }
            }
        }

        /// <summary>
        /// 自动测量模型在自然姿态下的脚踝离地高度与固有鞋底基准面旋转，
        /// 确保无论动画当前处于何种角度，踩地时鞋底都严丝合缝平贴地面。
        /// </summary>
        private void MeasureAnkleRestBaselines()
        {
            Transform rootTransform = _entity != null ? _entity.transform : transform;
            float rootY = rootTransform.position.y;
            Quaternion rootRotInv = Quaternion.Inverse(rootTransform.rotation);

            if (_leftFootBone != null)
            {
                float measured = _leftFootBone.position.y - rootY;
                _leftAnkleRestHeight = (measured > 0.03f && measured < 0.35f) ? measured : 0.12f;
                _leftFootRestLocalRot = rootRotInv * _leftFootBone.rotation;
            }

            if (_rightFootBone != null)
            {
                float measured = _rightFootBone.position.y - rootY;
                _rightAnkleRestHeight = (measured > 0.03f && measured < 0.35f) ? measured : 0.12f;
                _rightFootRestLocalRot = rootRotInv * _rightFootBone.rotation;
            }
        }

        private void InitializeRuntimeState()
        {
            if (_leftFootBone != null)
            {
                _curLeftTargetPos = _leftFootBone.position;
                _curLeftTargetRot = _leftFootBone.rotation;
                _lastLeftFootY = _leftFootBone.position.y;
                _lastLeftFootWorldPos = _leftFootBone.position;
                _smoothLeftFootVel = Vector3.zero;
                _leftPlantPos = _leftFootBone.position;
                _leftPlantRot = _leftFootBone.rotation;
            }

            if (_rightFootBone != null)
            {
                _curRightTargetPos = _rightFootBone.position;
                _curRightTargetRot = _rightFootBone.rotation;
                _lastRightFootY = _rightFootBone.position.y;
                _lastRightFootWorldPos = _rightFootBone.position;
                _smoothRightFootVel = Vector3.zero;
                _rightPlantPos = _rightFootBone.position;
                _rightPlantRot = _rightFootBone.rotation;
            }

            _isLeftPlanted = false;
            _isRightPlanted = false;
            _currentPelvisOffset = 0f;
        }

        private void ResetRuntimeState()
        {
            _currentPelvisOffset = 0f;
            _isLeftPlanted = false;
            _isRightPlanted = false;
            _smoothLeftFootVel = Vector3.zero;
            _smoothRightFootVel = Vector3.zero;
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }
        #endregion

        #region Core IK Pipeline
        private void EvaluateFootIK(float deltaTime)
        {
            Transform rootTransform = _entity != null ? _entity.transform : transform;

            // 0. 实时提取脚底世界瞬态速度并进行指数平滑滤波 (用于前向速度外推预测)
            if (_leftFootBone != null)
            {
                Vector3 rawVel = (_leftFootBone.position - _lastLeftFootWorldPos) / Mathf.Max(0.0001f, deltaTime);
                _lastLeftFootWorldPos = _leftFootBone.position;
                _smoothLeftFootVel = Vector3.Lerp(_smoothLeftFootVel, rawVel, deltaTime * _velocitySmoothSpeed);
            }
            if (_rightFootBone != null)
            {
                Vector3 rawVel = (_rightFootBone.position - _lastRightFootWorldPos) / Mathf.Max(0.0001f, deltaTime);
                _lastRightFootWorldPos = _rightFootBone.position;
                _smoothRightFootVel = Vector3.Lerp(_smoothRightFootVel, rawVel, deltaTime * _velocitySmoothSpeed);
            }

            // 1. 角色状态感知与安全检测
            bool isGrounded = true;
            bool isMoving = false;
            bool isSkillOrAction = false;

            if (_entity != null)
            {
                if (_entity.MovementController != null)
                {
                    isGrounded = _entity.MovementController.IsGrounded;
                    isMoving = _entity.MovementController.Velocity.sqrMagnitude > 0.05f;
                }

                // 智能感知是否处于技能/发力/闪避/受击状态
                if (_entity.StateMachine != null && _entity.StateMachine.CurrentState != null)
                {
                    System.Type stateType = _entity.StateMachine.CurrentState.GetType();
                    if (stateType != typeof(CharacterGroundState))
                    {
                        isSkillOrAction = true;
                    }
                }

                if (_entity.RuntimeData != null && _entity.RuntimeData.TargetGroundSubState == ActionState.Skill)
                {
                    isSkillOrAction = true;
                }
            }

            // 空中状态安全释放
            if (!isGrounded)
            {
                _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, deltaTime * _pelvisDamping);
                _pelvisBone.position += new Vector3(0f, _currentPelvisOffset * _masterWeight, 0f);
                _isLeftPlanted = false;
                _isRightPlanted = false;
                if (_leftFootBone != null)
                {
                    _curLeftTargetPos = _leftFootBone.position;
                    _curLeftTargetRot = _leftFootBone.rotation;
                }
                if (_rightFootBone != null)
                {
                    _curRightTargetPos = _rightFootBone.position;
                    _curRightTargetRot = _rightFootBone.rotation;
                }
                return;
            }

            // 技能状态权重自适应系数 (技能中平滑过渡或按配置倍率生效)
            float stateMultiplier = 1.0f;
            if (isSkillOrAction)
            {
                stateMultiplier = _enableIKInSkill ? _skillIKWeight : 0f;
                if (stateMultiplier <= 0.001f)
                {
                    _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, deltaTime * _pelvisDamping);
                    _pelvisBone.position += new Vector3(0f, _currentPelvisOffset * _masterWeight, 0f);
                    _isLeftPlanted = false;
                    _isRightPlanted = false;
                    return;
                }
            }

            // 2. 前向速度外推预测 + 双射线融合检测 (获取当前与预测地面接触点)
            bool leftHit = EvaluateFootGroundWithPrediction(
                _leftFootBone,
                _smoothLeftFootVel,
                rootTransform,
                _leftAnkleRestHeight,
                out Vector3 leftGroundHitPos,
                out Vector3 leftGroundNormal,
                out _debugLeftPredictPos,
                out _debugLeftPredictHitPos,
                out _debugLeftPredictHit
            );

            bool rightHit = EvaluateFootGroundWithPrediction(
                _rightFootBone,
                _smoothRightFootVel,
                rootTransform,
                _rightAnkleRestHeight,
                out Vector3 rightGroundHitPos,
                out Vector3 rightGroundNormal,
                out _debugRightPredictPos,
                out _debugRightPredictHitPos,
                out _debugRightPredictHit
            );

            // 3. 斜坡自适应动态权重计算 (基于脚踝相对下方地面的真实距离，确保斜坡高位脚绝对有 100% 支撑权重)
            float leftWeight = CalculateProceduralFootWeight(_leftFootBone, leftGroundHitPos, leftHit, ref _lastLeftFootY, deltaTime);
            float rightWeight = CalculateProceduralFootWeight(_rightFootBone, rightGroundHitPos, rightHit, ref _lastRightFootY, deltaTime);

            // 4. 骨盆自适应下沉与斜坡落差平衡
            // lowDelta < 0 (低位脚悬空需要骨盆下沉够地)，highDelta > 0 (高位脚在斜坡高处需要屈膝抬升)
            float leftDelta = (leftHit && leftWeight > 0.01f) ? (leftGroundHitPos.y - _leftFootBone.position.y) : 0f;
            float rightDelta = (rightHit && rightWeight > 0.01f) ? (rightGroundHitPos.y - _rightFootBone.position.y) : 0f;

            float minDelta = Mathf.Min(leftDelta, rightDelta);
            float targetPelvisOffset = 0f;
            if (minDelta < 0f)
            {
                // 骨盆按平衡比例适度下沉，为高位腿腾出充足的屈膝抬升空间
                targetPelvisOffset = minDelta * _pelvisSlopeBalance;
            }

            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisOffset, deltaTime * _pelvisDamping);
            _pelvisBone.position += new Vector3(0f, _currentPelvisOffset * _masterWeight * stateMultiplier, 0f);

            // 只有在严格待机（非技能、非发力、非移动）时才允许定点锁定，彻底避免发力动作脚底被钉死拉扯
            bool allowPlantLock = _enableFootPlantLock && !isMoving && !isSkillOrAction;

            // 5. 左脚：贴坡旋转 (100% 保留小腿扭转与动画 Yaw) + 两骨骼余弦定理 IK 解算
            if (_leftThighBone != null && _leftCalfBone != null && _leftFootBone != null)
            {
                float finalLeftWeight = leftWeight * _masterWeight * stateMultiplier;

                if (leftHit && finalLeftWeight > 0.001f)
                {
                    // 贴坡旋转：保留动画当前帧脚踝与小腿的自然发力偏航角，仅叠加坡度倾角
                    Quaternion slopeAlignedRot = AlignFootRotationToSlope(_leftFootBone.rotation, leftGroundNormal);

                    Vector3 desiredTargetPos;
                    Quaternion desiredTargetRot;

                    if (allowPlantLock)
                    {
                        float distToPlant = Vector3.Distance(_leftFootBone.position, _leftPlantPos);
                        if (!_isLeftPlanted || distToPlant > _maxPlantDistance || Mathf.Abs(_leftPlantPos.y - leftGroundHitPos.y) > 0.03f)
                        {
                            _leftPlantPos = leftGroundHitPos;
                            _leftPlantRot = slopeAlignedRot;
                            _isLeftPlanted = true;
                        }
                        else
                        {
                            // 垂直高度始终随地面最新检测更新，防止被卡死在低处
                            _leftPlantPos.y = leftGroundHitPos.y;
                        }

                        desiredTargetPos = _leftPlantPos;
                        desiredTargetRot = _leftPlantRot;
                    }
                    else
                    {
                        _isLeftPlanted = false;
                        desiredTargetPos = leftGroundHitPos;
                        desiredTargetRot = slopeAlignedRot;
                    }

                    _curLeftTargetPos = Vector3.Lerp(_curLeftTargetPos, desiredTargetPos, deltaTime * _footPositionDamping);
                    // 地面防穿模硬钳制：脚部目标高度绝对不允许低于地面接触点！
                    _curLeftTargetPos.y = Mathf.Max(_curLeftTargetPos.y, leftGroundHitPos.y);

                    SolveTwoBoneIK(_leftThighBone, _leftCalfBone, _leftFootBone, _curLeftTargetPos, finalLeftWeight, rootTransform);

                    _curLeftTargetRot = Quaternion.Slerp(_curLeftTargetRot, desiredTargetRot, deltaTime * _footRotationDamping);
                    _leftFootBone.rotation = Quaternion.Slerp(_leftFootBone.rotation, _curLeftTargetRot, finalLeftWeight);
                }
                else
                {
                    _isLeftPlanted = false;
                    _curLeftTargetPos = _leftFootBone.position;
                    _curLeftTargetRot = _leftFootBone.rotation;
                }
            }

            // 6. 右脚：贴坡旋转 (100% 保留小腿扭转与动画 Yaw) + 两骨骼余弦定理 IK 解算
            if (_rightThighBone != null && _rightCalfBone != null && _rightFootBone != null)
            {
                float finalRightWeight = rightWeight * _masterWeight * stateMultiplier;

                if (rightHit && finalRightWeight > 0.001f)
                {
                    Quaternion slopeAlignedRot = AlignFootRotationToSlope(_rightFootBone.rotation, rightGroundNormal);

                    Vector3 desiredTargetPos;
                    Quaternion desiredTargetRot;

                    if (allowPlantLock)
                    {
                        float distToPlant = Vector3.Distance(_rightFootBone.position, _rightPlantPos);
                        if (!_isRightPlanted || distToPlant > _maxPlantDistance || Mathf.Abs(_rightPlantPos.y - rightGroundHitPos.y) > 0.03f)
                        {
                            _rightPlantPos = rightGroundHitPos;
                            _rightPlantRot = slopeAlignedRot;
                            _isRightPlanted = true;
                        }
                        else
                        {
                            // 垂直高度始终随地面最新检测更新，防止被卡死在低处
                            _rightPlantPos.y = rightGroundHitPos.y;
                        }

                        desiredTargetPos = _rightPlantPos;
                        desiredTargetRot = _rightPlantRot;
                    }
                    else
                    {
                        _isRightPlanted = false;
                        desiredTargetPos = rightGroundHitPos;
                        desiredTargetRot = slopeAlignedRot;
                    }

                    _curRightTargetPos = Vector3.Lerp(_curRightTargetPos, desiredTargetPos, deltaTime * _footPositionDamping);
                    // 地面防穿模硬钳制：脚部目标高度绝对不允许低于地面接触点！
                    _curRightTargetPos.y = Mathf.Max(_curRightTargetPos.y, rightGroundHitPos.y);

                    SolveTwoBoneIK(_rightThighBone, _rightCalfBone, _rightFootBone, _curRightTargetPos, finalRightWeight, rootTransform);

                    _curRightTargetRot = Quaternion.Slerp(_curRightTargetRot, desiredTargetRot, deltaTime * _footRotationDamping);
                    _rightFootBone.rotation = Quaternion.Slerp(_rightFootBone.rotation, _curRightTargetRot, finalRightWeight);
                }
                else
                {
                    _isRightPlanted = false;
                    _curRightTargetPos = _rightFootBone.position;
                    _curRightTargetRot = _rightFootBone.rotation;
                }
            }
        }

        /// <summary>
        /// 双射线融合地面检测：结合当前足底真实射线与前向速度外推预测射线，
        /// 提前探知前方上坡/台阶地形并在落地前预对齐坡度法线。
        /// </summary>
        private bool EvaluateFootGroundWithPrediction(
            Transform footBone,
            Vector3 smoothVelocity,
            Transform rootTransform,
            float ankleRestHeight,
            out Vector3 finalHitPos,
            out Vector3 finalNormal,
            out Vector3 debugPredictPos,
            out Vector3 debugPredictHitPos,
            out bool debugPredictHit)
        {
            debugPredictPos = footBone != null ? footBone.position : Vector3.zero;
            debugPredictHitPos = Vector3.zero;
            debugPredictHit = false;

            if (footBone == null)
            {
                finalHitPos = Vector3.zero;
                finalNormal = Vector3.up;
                return false;
            }

            Vector3 currentPos = footBone.position;
            bool currentHit = RaycastGroundAtPosition(currentPos, rootTransform, ankleRestHeight, out Vector3 currHitPos, out Vector3 currNormal);

            if (!_enablePrediction)
            {
                finalHitPos = currHitPos;
                finalNormal = currNormal;
                return currentHit;
            }

            // 计算前瞻点 (根据速度与下落趋势)
            Vector3 forwardAndDownVel = smoothVelocity;
            forwardAndDownVel.y = Mathf.Min(0f, forwardAndDownVel.y); // 仅在前向与下落方向外推

            float speedMag = forwardAndDownVel.magnitude;
            if (speedMag > 0.05f)
            {
                debugPredictPos = currentPos + forwardAndDownVel * _predictionTime;
            }
            else
            {
                debugPredictPos = currentPos;
            }

            debugPredictHit = RaycastGroundAtPosition(debugPredictPos, rootTransform, ankleRestHeight, out debugPredictHitPos, out Vector3 predNormal);

            if (currentHit)
            {
                finalHitPos = currHitPos;
                finalNormal = currNormal;

                if (debugPredictHit)
                {
                    // A. 台阶/上坡防穿模提前抬升：如果预测落点高于当前落点 (例如跑上台阶/斜坡)，提前预抬升目标高度
                    if (debugPredictHitPos.y > currHitPos.y + 0.01f)
                    {
                        finalHitPos.y = Mathf.Lerp(currHitPos.y, debugPredictHitPos.y, _landingAnticipation);
                    }

                    // B. 落地姿态预对齐
                    finalNormal = Vector3.Slerp(currNormal, predNormal, _landingAnticipation * 0.5f);
                }
                return true;
            }
            else if (debugPredictHit)
            {
                finalHitPos = debugPredictHitPos;
                finalNormal = predNormal;
                return true;
            }

            finalHitPos = currentPos;
            finalNormal = Vector3.up;
            return false;
        }

        /// <summary>
        /// 斜坡自适应单脚 IK 权重计算：
        /// 基于脚踝相对下方地面接触点的真实距离进行判定，彻底消除斜坡高位脚被误判为离地抬腿的严重 Bug。
        /// </summary>
        private float CalculateProceduralFootWeight(Transform footBone, Vector3 groundHitPos, bool groundHit, ref float lastY, float deltaTime)
        {
            if (footBone == null) return 0f;

            float currentY = footBone.position.y;
            float verticalVelocity = (currentY - lastY) / Mathf.Max(0.0001f, deltaTime);
            lastY = currentY;

            if (!groundHit) return 0f;

            // 1. 计算脚踝当前高度相对地面接触点的高度差 (高于地面为正，穿模为负)
            float heightAboveGround = currentY - groundHitPos.y;

            // 2. 如果脚踝低于或贴近地面 (穿模/触地支撑)，权重绝对是 100% (无论是在平地还是斜坡高位！)
            if (heightAboveGround <= _groundDistanceThreshold)
            {
                return 1f;
            }

            // 3. 如果脚踝高于地面 (正在抬脚)，根据离地距离平滑衰减
            float liftFactor = Mathf.Clamp01((heightAboveGround - _groundDistanceThreshold) / Mathf.Max(0.001f, _maxLiftOffDistance - _groundDistanceThreshold));
            float heightWeight = 1f - liftFactor;

            // 4. 向上离开地面的垂直速度越快，加速释放权重
            if (verticalVelocity > _liftVelocityThreshold)
            {
                float velocityDamp = 1f - Mathf.Clamp01((verticalVelocity - _liftVelocityThreshold) / _liftVelocityThreshold);
                heightWeight *= velocityDamp;
            }

            return Mathf.Clamp01(heightWeight);
        }

        /// <summary>
        /// 针对指定空间点垂直向下发射地面检测射线 (使用 NonAlloc 避免 GC，自动过滤角色自身碰撞体，并根据实际脚踝自然高度计算目标点)
        /// </summary>
        private bool RaycastGroundAtPosition(Vector3 originPos, Transform rootTransform, float ankleRestHeight, out Vector3 groundPos, out Vector3 groundNormal)
        {
            Vector3 rayStart = originPos + Vector3.up * _raycastUpOffset;
            float totalDistance = _raycastDistance + _raycastUpOffset;

            int hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, _rayHits, totalDistance, _groundLayer, QueryTriggerInteraction.Ignore);
            if (hitCount > 0)
            {
                float closestDist = float.MaxValue;
                int bestIdx = -1;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = _rayHits[i];
                    if (hit.collider == null) continue;

                    // 过滤自身或子物体上的碰撞体 (如 CharacterController, CapsuleCollider)
                    if (rootTransform != null && hit.collider.transform.IsChildOf(rootTransform))
                    {
                        continue;
                    }

                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    RaycastHit bestHit = _rayHits[bestIdx];
                    // 目标脚踝高度 = 地面碰撞点 + 自然脚踝离地厚度 + 手动微调偏移
                    groundPos = new Vector3(bestHit.point.x, bestHit.point.y + ankleRestHeight + _footHeightOffset, bestHit.point.z);
                    groundNormal = bestHit.normal;
                    return true;
                }
            }

            groundPos = originPos;
            groundNormal = Vector3.up;
            return false;
        }

        /// <summary>
        /// 斜坡对齐旋转计算：
        /// 当开启 _preserveAnimationTwist 时，将地面坡度倾角叠加在动画原始旋转上，
        /// 100% 保持小腿发力扭转、技能动作中的偏航角 (Yaw) 与动画细节，彻底消除麻花扭曲！
        /// </summary>
        private Quaternion AlignFootRotationToSlope(Quaternion animFootRot, Vector3 groundNormal)
        {
            if (Vector3.Angle(Vector3.up, groundNormal) > _maxGroundAngle)
            {
                groundNormal = Vector3.up;
            }

            if (_preserveAnimationTwist)
            {
                // 计算地面法线相对世界 Up 的倾斜旋转差 (Pitch / Roll 贴坡)
                Quaternion slopeTilt = Quaternion.FromToRotation(Vector3.up, groundNormal);

                // 将坡度倾角叠加在动画原始旋转上：保留小腿旋转、技能发力扭转与脚踝偏航
                return slopeTilt * animFootRot;
            }
            else
            {
                // 备用模式：全局对齐
                Transform rootTransform = _entity != null ? _entity.transform : transform;
                Vector3 groundForward = Vector3.ProjectOnPlane(rootTransform.forward, groundNormal).normalized;
                if (groundForward.sqrMagnitude < 0.001f)
                {
                    groundForward = rootTransform.forward;
                }
                Quaternion slopeBasis = Quaternion.LookRotation(groundForward, groundNormal);
                return slopeBasis * _leftFootRestLocalRot;
            }
        }

        /// <summary>
        /// 解析式两骨骼 IK 解算器 (基于极向向量 Pole Vector 与余弦定理精确闭式求解)
        /// 保证无论骨骼原始轴向为何种格式，膝关节均严格向前屈膝，高位脚 100% 抬升至斜坡高处！
        /// </summary>
        private static void SolveTwoBoneIK(
            Transform root,
            Transform mid,
            Transform tip,
            Vector3 targetPos,
            float weight,
            Transform characterRoot)
        {
            if (weight <= 0.0001f || root == null || mid == null || tip == null)
            {
                return;
            }

            Vector3 a = root.position; // 大腿根部
            Vector3 b = mid.position;  // 膝关节
            Vector3 c = tip.position;  // 脚踝

            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ac = c - a;
            Vector3 at = targetPos - a;

            float lab = ab.magnitude;
            float lbc = bc.magnitude;
            float lat = at.magnitude;
            float lac = ac.magnitude;

            if (lab < 0.001f || lbc < 0.001f || lat < 0.001f || lac < 0.001f)
            {
                return;
            }

            // 限制目标距离在两骨骼极限长度之间 (略留 0.001 裕量避免奇异点)
            lat = Mathf.Clamp(lat, 0.01f, lab + lbc - 0.001f);

            // 1. 严格计算膝盖向前弯曲方向 (Pole Direction) 与弯曲平面法线 (Hinge Normal)
            Vector3 charFwd = characterRoot != null ? characterRoot.forward : root.forward;

            // 膝盖极向向量：优先取动画当前膝盖在垂直于 at 平面上的投影；若伸直则取角色正前方
            Vector3 kneeDir = Vector3.ProjectOnPlane(b - a, at);
            if (kneeDir.sqrMagnitude < 0.0001f)
            {
                kneeDir = Vector3.ProjectOnPlane(charFwd, at);
                if (kneeDir.sqrMagnitude < 0.0001f)
                {
                    kneeDir = charFwd;
                }
            }
            kneeDir.Normalize();

            // 弯曲铰链轴：at × kneeDir。根据右手定则，绕此轴将大腿向 kneeDir(前) 弯折
            Vector3 bendNormal = Vector3.Cross(at, kneeDir).normalized;
            if (bendNormal.sqrMagnitude < 0.0001f)
            {
                bendNormal = characterRoot != null ? -characterRoot.right : -root.right;
            }

            // 2. 余弦定理求解大腿(Root)目标夹角与当前夹角
            float cosAngleA_target = Mathf.Clamp((lab * lab + lat * lat - lbc * lbc) / (2f * lab * lat), -1f, 1f);
            float angleA_target = Mathf.Acos(cosAngleA_target) * Mathf.Rad2Deg;

            // 3. 求解大腿目标朝向向量 targetThighDir 并旋转 Root
            // 将 at 向量绕弯曲法线旋转 angleA_target 度，得到大腿骨骼 ab 的世界目标朝向
            Vector3 targetThighDir = (Quaternion.AngleAxis(angleA_target, bendNormal) * at).normalized;
            Quaternion targetRootRot = Quaternion.FromToRotation(ab, targetThighDir * lab) * root.rotation;
            root.rotation = Quaternion.Slerp(root.rotation, targetRootRot, weight);

            // 4. 旋转 Mid (小腿)：直接将小腿朝向 targetPos
            Vector3 newB = mid.position;
            Vector3 curMidToTip = tip.position - newB;
            Vector3 targetMidToTip = targetPos - newB;

            if (curMidToTip.sqrMagnitude > 0.0001f && targetMidToTip.sqrMagnitude > 0.0001f)
            {
                Quaternion targetMidRot = Quaternion.FromToRotation(curMidToTip, targetMidToTip) * mid.rotation;
                mid.rotation = Quaternion.Slerp(mid.rotation, targetMidRot, weight);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// 瞬间设置主 IK 权重 (0~1)
        /// </summary>
        public void SetWeightImmediate(float weight)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _masterWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// 平滑渐变主 IK 权重
        /// </summary>
        public void FadeWeight(float targetWeight, float duration)
        {
            targetWeight = Mathf.Clamp01(targetWeight);

            if (duration <= 0f)
            {
                SetWeightImmediate(targetWeight);
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeWeightRoutine(targetWeight, duration));
        }

        /// <summary>
        /// 平滑开启 IK
        /// </summary>
        public void EnableFootIK(float duration = 0.15f)
        {
            _enableIK = true;
            FadeWeight(1f, duration);
        }

        /// <summary>
        /// 平滑关闭 IK
        /// </summary>
        public void DisableFootIK(float duration = 0.1f)
        {
            FadeWeight(0f, duration);
        }

        private IEnumerator FadeWeightRoutine(float target, float duration)
        {
            float startWeight = _masterWeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _masterWeight = Mathf.Lerp(startWeight, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _masterWeight = target;
            _fadeCoroutine = null;
        }
        #endregion

        #region Bone Auto-Discovery
        /// <summary>
        /// 自动在层级中智能匹配通用骨骼
        /// 优先精确匹配项目标准命名 (如 "Bip001 Pelvis"、"Bip001 L Thigh"、"Bip001 L Calf"、"Bip001 L Foot" 等)，
        /// 并自动排除 Twist、Fix、Adv、Nub、Toe、Bip002 等辅助节点。
        /// </summary>
        public void AutoDiscoverBonesIfMissing()
        {
            var transforms = GetComponentsInChildren<Transform>(true);

            // 通用排除关键字列表：防止误匹配衣服挂点、辅骨、扭转骨、脚趾、副骨骼
            string[] commonExcludes = { "fix", "cloth", "ribbon", "adv", "twis", "twist", "nub", "toe", "bip002", "bip003", "skn", "ik", "target", "pole" };

            // 1. 骨盆 (Pelvis)
            if (_pelvisBone == null)
            {
                _pelvisBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 Pelvis", "Pelvis", "Hips", "root_pelvis" },
                    fuzzyKeywords: new[] { "pelvis", "hips" },
                    excludeKeywords: commonExcludes);
            }

            // 2. 左腿 (Left Leg: Thigh / Calf / Foot)
            if (_leftThighBone == null)
            {
                _leftThighBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 L Thigh", "L Thigh", "LeftUpLeg", "Thigh.L", "L_Thigh" },
                    fuzzyKeywords: new[] { "bip001 l thigh", "l thigh", "l_thigh", "leftupleg", "thigh.l" },
                    excludeKeywords: commonExcludes);
            }

            if (_leftCalfBone == null)
            {
                _leftCalfBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 L Calf", "L Calf", "LeftLeg", "Calf.L", "L_Calf" },
                    fuzzyKeywords: new[] { "bip001 l calf", "l calf", "l_calf", "leftleg", "calf.l", "l_knee" },
                    excludeKeywords: commonExcludes);
            }

            if (_leftFootBone == null)
            {
                _leftFootBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 L Foot", "L Foot", "LeftFoot", "Foot.L", "L_Foot" },
                    fuzzyKeywords: new[] { "bip001 l foot", "l foot", "l_foot", "leftfoot", "foot.l", "l_ankle" },
                    excludeKeywords: commonExcludes);
            }

            // 3. 右腿 (Right Leg: Thigh / Calf / Foot)
            if (_rightThighBone == null)
            {
                _rightThighBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 R Thigh", "R Thigh", "RightUpLeg", "Thigh.R", "R_Thigh" },
                    fuzzyKeywords: new[] { "bip001 r thigh", "r thigh", "r_thigh", "rightupleg", "thigh.r" },
                    excludeKeywords: commonExcludes);
            }

            if (_rightCalfBone == null)
            {
                _rightCalfBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 R Calf", "R Calf", "RightLeg", "Calf.R", "R_Calf" },
                    fuzzyKeywords: new[] { "bip001 r calf", "r calf", "r_calf", "rightleg", "calf.r", "r_knee" },
                    excludeKeywords: commonExcludes);
            }

            if (_rightFootBone == null)
            {
                _rightFootBone = FindBone(transforms,
                    exactNames: new[] { "Bip001 R Foot", "R Foot", "RightFoot", "Foot.R", "R_Foot" },
                    fuzzyKeywords: new[] { "bip001 r foot", "r foot", "r_foot", "rightfoot", "foot.r", "r_ankle" },
                    excludeKeywords: commonExcludes);
            }
        }

        private static Transform FindBone(Transform[] allTransforms, string[] exactNames, string[] fuzzyKeywords, string[] excludeKeywords = null)
        {
            // 第一阶段：优先精准全字匹配 (Case-Insensitive)
            if (exactNames != null)
            {
                foreach (var t in allTransforms)
                {
                    foreach (var exact in exactNames)
                    {
                        if (string.Equals(t.name, exact, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return t;
                        }
                    }
                }
            }

            // 第二阶段：严格排除辅助骨骼后的模糊匹配
            if (fuzzyKeywords != null)
            {
                foreach (var t in allTransforms)
                {
                    string nameLower = t.name.ToLowerInvariant();

                    bool excluded = false;
                    if (excludeKeywords != null)
                    {
                        foreach (var exc in excludeKeywords)
                        {
                            if (nameLower.Contains(exc))
                            {
                                excluded = true;
                                break;
                            }
                        }
                    }
                    if (excluded) continue;

                    foreach (var kw in fuzzyKeywords)
                    {
                        if (nameLower.Contains(kw))
                        {
                            return t;
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;

            // 1. 左脚可视化 (绿色真实射线，黄色预测向量与落点)
            if (_leftFootBone != null)
            {
                Gizmos.color = Color.green;
                Vector3 start = _leftFootBone.position + Vector3.up * _raycastUpOffset;
                Gizmos.DrawLine(start, start + Vector3.down * (_raycastDistance + _raycastUpOffset));
                Gizmos.DrawWireSphere(_curLeftTargetPos, 0.04f);

                if (_enablePrediction && _debugLeftPredictPos != _leftFootBone.position)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(_leftFootBone.position, _debugLeftPredictPos);
                    Gizmos.DrawWireSphere(_debugLeftPredictPos, 0.025f);
                    if (_debugLeftPredictHit)
                    {
                        Gizmos.color = new Color(1f, 0.6f, 0f, 0.85f);
                        Gizmos.DrawWireCube(_debugLeftPredictHitPos, new Vector3(0.08f, 0.02f, 0.16f));
                    }
                }

                if (_isLeftPlanted)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(_leftPlantPos, new Vector3(0.08f, 0.02f, 0.16f));
                }
            }

            // 2. 右脚可视化 (蓝色真实射线，青色预测向量与落点)
            if (_rightFootBone != null)
            {
                Gizmos.color = Color.blue;
                Vector3 start = _rightFootBone.position + Vector3.up * _raycastUpOffset;
                Gizmos.DrawLine(start, start + Vector3.down * (_raycastDistance + _raycastUpOffset));
                Gizmos.DrawWireSphere(_curRightTargetPos, 0.04f);

                if (_enablePrediction && _debugRightPredictPos != _rightFootBone.position)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_rightFootBone.position, _debugRightPredictPos);
                    Gizmos.DrawWireSphere(_debugRightPredictPos, 0.025f);
                    if (_debugRightPredictHit)
                    {
                        Gizmos.color = new Color(0f, 0.8f, 1f, 0.85f);
                        Gizmos.DrawWireCube(_debugRightPredictHitPos, new Vector3(0.08f, 0.02f, 0.16f));
                    }
                }

                if (_isRightPlanted)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(_rightPlantPos, new Vector3(0.08f, 0.02f, 0.16f));
                }
            }
        }
        #endregion
    }
}
