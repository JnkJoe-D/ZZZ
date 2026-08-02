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

        [Tooltip("射线检测总长度 (米)")]
        [SerializeField] private float _raycastDistance = 0.9f;

        [Tooltip("射线起点相对于脚踝骨骼的向上偏移量 (米)")]
        [SerializeField] private float _raycastUpOffset = 0.45f;

        [Tooltip("手动微调鞋底高度偏移 (米，正数垫高，负数降低，默认为 0)")]
        [SerializeField] private float _footHeightOffset = 0.0f;

        [Tooltip("检测脚掌旋转对齐的最大坡度角 (度)")]
        [SerializeField] private float _maxGroundAngle = 45f;
        #endregion

        #region Foot Locking (Anti-Sway)
        [Header("脚部接地锁定 (防左右晃动与滑步)")]
        [Tooltip("是否开启脚底世界坐标定点锁定 (待机时完全钉死脚掌)")]
        [SerializeField] private bool _enableFootPlantLock = true;

        [Tooltip("脚部锚点最大容差距离 (米)，角色移动或迈步超过此距离时自动释放锚点")]
        [SerializeField] private float _maxPlantDistance = 0.25f;
        #endregion

        #region Procedural Weight Heuristics
        [Header("程序化抬腿自适应 (无需动画曲线)")]
        [Tooltip("脚踝相对角色根节点的垂直高度低于此值时判定为踩地支撑 (米)")]
        [SerializeField] private float _groundHeightThreshold = 0.12f;

        [Tooltip("脚踝相对角色根节点的垂直高度高于此值时 IK 权重完全归零 (米)")]
        [SerializeField] private float _maxLiftHeight = 0.25f;

        [Tooltip("脚向上抬起的垂直速度超过此阈值时加速释放 IK (米/秒)")]
        [SerializeField] private float _liftVelocityThreshold = 0.25f;
        #endregion

        #region Suspension & Smoothing
        [Header("启用控制")]
        [Tooltip("是否启用 Foot IK (当前已全局禁用)")]
        [SerializeField] private bool _enableIK = false;

        [Header("平滑与悬挂阻尼")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterWeight = 0f;

        [Tooltip("骨盆高度调整的平滑阻尼速度")]
        [SerializeField] private float _pelvisDamping = 12f;

        [Tooltip("脚部位置追踪的平滑阻尼速度")]
        [SerializeField] private float _footPositionDamping = 25f;

        [Tooltip("脚底旋转对齐的平滑阻尼速度")]
        [SerializeField] private float _footRotationDamping = 18f;
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

        public float CurrentWeight => _masterWeight;
        public bool HasBones => _pelvisBone != null && _leftFootBone != null && _rightFootBone != null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
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
            AutoDiscoverBonesIfMissing();
            MeasureAnkleRestBaselines();
            InitializeRuntimeState();
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
                _leftAnkleRestHeight = measured > 0.03f ? measured : 0.12f;
                _leftFootRestLocalRot = rootRotInv * _leftFootBone.rotation;
            }

            if (_rightFootBone != null)
            {
                float measured = _rightFootBone.position.y - rootY;
                _rightAnkleRestHeight = measured > 0.03f ? measured : 0.12f;
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
                _leftPlantPos = _leftFootBone.position;
                _leftPlantRot = _leftFootBone.rotation;
            }

            if (_rightFootBone != null)
            {
                _curRightTargetPos = _rightFootBone.position;
                _curRightTargetRot = _rightFootBone.rotation;
                _lastRightFootY = _rightFootBone.position.y;
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
            Vector3 rootPos = rootTransform.position;

            // 1. 程序化计算左右脚动态抬腿/踩地权重 (0~1)
            float leftWeight = CalculateProceduralFootWeight(_leftFootBone, rootPos, ref _lastLeftFootY, deltaTime);
            float rightWeight = CalculateProceduralFootWeight(_rightFootBone, rootPos, ref _lastRightFootY, deltaTime);

            // 2. 物理射线检测地面高度与法线 (自动基于测量的脚踝高度补偿，绝不陷地)
            bool leftHit = RaycastGround(_leftFootBone, rootTransform, _leftAnkleRestHeight, out Vector3 leftGroundHitPos, out Vector3 leftGroundNormal);
            bool rightHit = RaycastGround(_rightFootBone, rootTransform, _rightAnkleRestHeight, out Vector3 rightGroundHitPos, out Vector3 rightGroundNormal);

            // 3. 计算骨盆自适应下沉 (根据触地脚的地面落差)
            float leftDelta = (leftHit && leftWeight > 0.01f) ? (leftGroundHitPos.y - _leftFootBone.position.y) : 0f;
            float rightDelta = (rightHit && rightWeight > 0.01f) ? (rightGroundHitPos.y - _rightFootBone.position.y) : 0f;

            float targetPelvisOffset = Mathf.Min(leftDelta, rightDelta);
            if (targetPelvisOffset > 0f)
            {
                targetPelvisOffset = 0f; // 骨盆只在单脚下陷时下沉，不主动上抬
            }

            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisOffset, deltaTime * _pelvisDamping);
            _pelvisBone.position += new Vector3(0f, _currentPelvisOffset * _masterWeight, 0f);

            // 4. 左脚：固定鞋底基准面旋转 + 两骨骼 IK 解算 + 定点锚定
            if (_leftThighBone != null && _leftCalfBone != null && _leftFootBone != null)
            {
                float finalLeftWeight = leftWeight * _masterWeight;

                if (leftHit && finalLeftWeight > 0.001f)
                {
                    // 严格基于鞋底固定基准面与地面法线计算平坦鞋底旋转，消除动画瞬态翘脚
                    Quaternion flatSoleRot = CalculateFlatSoleRotation(_leftFootRestLocalRot, leftGroundNormal, rootTransform);

                    Vector3 desiredTargetPos;
                    Quaternion desiredTargetRot;

                    if (_enableFootPlantLock)
                    {
                        float distToPlant = Vector3.Distance(_leftFootBone.position, _leftPlantPos);
                        if (!_isLeftPlanted || distToPlant > _maxPlantDistance)
                        {
                            _leftPlantPos = leftGroundHitPos;
                            _leftPlantRot = flatSoleRot;
                            _isLeftPlanted = true;
                        }

                        desiredTargetPos = _leftPlantPos;
                        desiredTargetRot = _leftPlantRot;
                    }
                    else
                    {
                        desiredTargetPos = leftGroundHitPos;
                        desiredTargetRot = flatSoleRot;
                    }

                    _curLeftTargetPos = Vector3.Lerp(_curLeftTargetPos, desiredTargetPos, deltaTime * _footPositionDamping);
                    SolveTwoBoneIK(_leftThighBone, _leftCalfBone, _leftFootBone, _curLeftTargetPos, finalLeftWeight);

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

            // 5. 右脚：固定鞋底基准面旋转 + 两骨骼 IK 解算 + 定点锚定
            if (_rightThighBone != null && _rightCalfBone != null && _rightFootBone != null)
            {
                float finalRightWeight = rightWeight * _masterWeight;

                if (rightHit && finalRightWeight > 0.001f)
                {
                    // 严格基于鞋底固定基准面与地面法线计算平坦鞋底旋转，消除动画瞬态翘脚
                    Quaternion flatSoleRot = CalculateFlatSoleRotation(_rightFootRestLocalRot, rightGroundNormal, rootTransform);

                    Vector3 desiredTargetPos;
                    Quaternion desiredTargetRot;

                    if (_enableFootPlantLock)
                    {
                        float distToPlant = Vector3.Distance(_rightFootBone.position, _rightPlantPos);
                        if (!_isRightPlanted || distToPlant > _maxPlantDistance)
                        {
                            _rightPlantPos = rightGroundHitPos;
                            _rightPlantRot = flatSoleRot;
                            _isRightPlanted = true;
                        }

                        desiredTargetPos = _rightPlantPos;
                        desiredTargetRot = _rightPlantRot;
                    }
                    else
                    {
                        desiredTargetPos = rightGroundHitPos;
                        desiredTargetRot = flatSoleRot;
                    }

                    _curRightTargetPos = Vector3.Lerp(_curRightTargetPos, desiredTargetPos, deltaTime * _footPositionDamping);
                    SolveTwoBoneIK(_rightThighBone, _rightCalfBone, _rightFootBone, _curRightTargetPos, finalRightWeight);

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
        /// 纯程序化单脚 IK 权重启发式计算：
        /// 综合利用脚踝局部垂直高度（Height）和垂直速度（Vertical Velocity）推导 Stance/Swing 相位。
        /// </summary>
        private float CalculateProceduralFootWeight(Transform footBone, Vector3 rootPos, ref float lastY, float deltaTime)
        {
            if (footBone == null) return 0f;

            // A. 计算脚踝相对角色根节点的局部垂直高度
            float localY = footBone.position.y - rootPos.y;

            // B. 高度衰减：在 groundHeightThreshold 以内权重为 1，超过 maxLiftHeight 权重平滑归零
            float heightWeight = 1f - Mathf.Clamp01((localY - _groundHeightThreshold) / Mathf.Max(0.001f, _maxLiftHeight - _groundHeightThreshold));

            // C. 垂直速度判定：脚向上抬起离开地面时快速释放权重
            float currentY = footBone.position.y;
            float verticalVelocity = (currentY - lastY) / Mathf.Max(0.0001f, deltaTime);
            lastY = currentY;

            if (verticalVelocity > _liftVelocityThreshold)
            {
                float velocityDamp = 1f - Mathf.Clamp01((verticalVelocity - _liftVelocityThreshold) / _liftVelocityThreshold);
                heightWeight *= velocityDamp;
            }

            return Mathf.Clamp01(heightWeight);
        }

        /// <summary>
        /// 物理地面向下射线检测 (使用 NonAlloc 避免 GC，自动过滤角色自身碰撞体，并根据实际脚踝自然高度计算目标点)
        /// </summary>
        private bool RaycastGround(Transform footBone, Transform rootTransform, float ankleRestHeight, out Vector3 groundPos, out Vector3 groundNormal)
        {
            if (footBone == null)
            {
                groundPos = Vector3.zero;
                groundNormal = Vector3.up;
                return false;
            }

            Vector3 rayStart = footBone.position + Vector3.up * _raycastUpOffset;
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

            groundPos = footBone.position;
            groundNormal = Vector3.up;
            return false;
        }

        /// <summary>
        /// 基于固定鞋底固有基准面计算贴地平坦旋转：
        /// 彻底摒弃取动画单帧瞬态旋转的方式，将鞋底固有水平基准面投射到地面法线与角色前向构建的坡度坐标系中。
        /// 无论动画在当前帧是脚跟还是脚尖先着地，踩在地上时鞋底都会 100% 平整贴合地表！
        /// </summary>
        private Quaternion CalculateFlatSoleRotation(Quaternion restLocalRot, Vector3 groundNormal, Transform rootTransform)
        {
            if (Vector3.Angle(Vector3.up, groundNormal) > _maxGroundAngle)
            {
                groundNormal = Vector3.up;
            }

            // 1. 计算贴坡的前向方向 (将角色朝向投影到坡度法线平面)
            Vector3 groundForward = Vector3.ProjectOnPlane(rootTransform.forward, groundNormal).normalized;
            if (groundForward.sqrMagnitude < 0.001f)
            {
                groundForward = rootTransform.forward;
            }

            // 2. 构建贴坡基底 (Up 严格垂直于坡面法线，Forward 顺应角色朝向)
            Quaternion slopeBasis = Quaternion.LookRotation(groundForward, groundNormal);

            // 3. 将模型固有的鞋底基准面叠加到贴坡基底
            return slopeBasis * restLocalRot;
        }

        /// <summary>
        /// 解析式两骨骼 IK 解算器 (余弦定理单次直接求解)
        /// </summary>
        private static void SolveTwoBoneIK(Transform root, Transform mid, Transform tip, Vector3 targetPos, float weight)
        {
            Vector3 a = root.position;
            Vector3 b = mid.position;
            Vector3 c = tip.position;

            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ac = c - a;
            Vector3 at = targetPos - a;

            float lab = ab.magnitude;
            float lbc = bc.magnitude;
            float lat = at.magnitude;
            float lac = ac.magnitude;

            if (lab < 0.0001f || lbc < 0.0001f || lat < 0.0001f)
            {
                return;
            }

            // 限制目标距离在两骨骼极限长度之间
            lat = Mathf.Clamp(lat, 0.001f, lab + lbc - 0.001f);

            // 1. 求解膝关节自然弯曲平面法线 (Bend Normal)
            Vector3 bendNormal = Vector3.Cross(ab, bc);
            if (bendNormal.sqrMagnitude < 0.0001f)
            {
                bendNormal = root.right;
            }
            else
            {
                bendNormal.Normalize();
            }

            // 2. 余弦定理求目标大腿夹角与原始大腿夹角
            float cosAngleA_target = Mathf.Clamp((lab * lab + lat * lat - lbc * lbc) / (2f * lab * lat), -1f, 1f);
            float angleA_target = Mathf.Acos(cosAngleA_target) * Mathf.Rad2Deg;

            float cosAngleA_orig = Mathf.Clamp((lab * lab + lac * lac - lbc * lbc) / (2f * lab * lac), -1f, 1f);
            float angleA_orig = Mathf.Acos(cosAngleA_orig) * Mathf.Rad2Deg;

            // 3. 旋转 Root (大腿)
            // 第一步：在弯曲平面内调整大腿屈伸角
            Quaternion flexRot = Quaternion.AngleAxis(angleA_target - angleA_orig, bendNormal);
            // 第二步：将肢体整体朝向从 ac 对准 at
            Quaternion alignRot = Quaternion.FromToRotation(flexRot * ac, at);
            Quaternion targetRootRot = alignRot * flexRot * root.rotation;
            root.rotation = Quaternion.Slerp(root.rotation, targetRootRot, weight);

            // 4. 旋转 Mid (小腿) 对准 Target
            Vector3 newB = mid.position;
            Vector3 newC = tip.position;
            Vector3 curMidToTip = newC - newB;
            Vector3 targetMidToTip = targetPos - newB;

            if (curMidToTip.sqrMagnitude > 0.0001f && targetMidToTip.sqrMagnitude > 0.0001f)
            {
                Quaternion midAlignRot = Quaternion.FromToRotation(curMidToTip, targetMidToTip);
                Quaternion targetMidRot = midAlignRot * mid.rotation;
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

            if (_leftFootBone != null)
            {
                Gizmos.color = Color.green;
                Vector3 start = _leftFootBone.position + Vector3.up * _raycastUpOffset;
                Gizmos.DrawLine(start, start + Vector3.down * (_raycastDistance + _raycastUpOffset));
                Gizmos.DrawWireSphere(_curLeftTargetPos, 0.04f);
                if (_isLeftPlanted)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(_leftPlantPos, new Vector3(0.08f, 0.02f, 0.16f));
                }
            }

            if (_rightFootBone != null)
            {
                Gizmos.color = Color.blue;
                Vector3 start = _rightFootBone.position + Vector3.up * _raycastUpOffset;
                Gizmos.DrawLine(start, start + Vector3.down * (_raycastDistance + _raycastUpOffset));
                Gizmos.DrawWireSphere(_curRightTargetPos, 0.04f);
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
