using System.Diagnostics;
using cfg;

using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    [RequireComponent(typeof(CharacterEntity))]
    public class CharacterMotor : MonoBehaviour, ICharacterMotor
    {
        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private Transform _visualRoot;



        public float TurnSpeed = 15f;
        public float Gravity => -9.81f;
        public float GravityScale = 1.0f;
        public float PushResistance = 0f;
        public CharacterController CharacterController => _cc;

        public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;

        private float _verticalVelocity = 0f;
        public float VerticalVelocity => _verticalVelocity;

        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }

        public void SetVerticalVelocity(float velocity)
        {
            _verticalVelocity = velocity;
        }

        MotionWindowLocalDeltaFilterMode filterMode = MotionWindowLocalDeltaFilterMode.None;
        private MotionWindowVisualOffsetMode visualOffsetMode = MotionWindowVisualOffsetMode.None;
        private RootMotionCollisionMode _collisionMode = RootMotionCollisionMode.DefaultSlide;
        private LayerMask _obstacleMask = ~0;
        
        [SerializeField] private float _rootMotionSkin = 0.01f;

        private void Awake()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("CharacterVisual"))
                {
                    _visualRoot = child;
                    break;
                }
            }
            
            // 如果没找到对应的 Tag，为了防止报错，可以保留原有的按名字查找作为备选兜底
            if (_visualRoot == null)
            {
                _visualRoot = transform.Find("Visual");
            }
            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc != null)
            {
                _cc.height = 1.6f;
                _cc.radius = 0.3f;
                _cc.center = new Vector3(0f, 0.8f, 0f);
                _cc.skinWidth = 0.015f;
                _cc.minMoveDistance = 0f;
                _cc.excludeLayers = LayerMask.GetMask("Player");
            }

            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = true;
            }
        }

        private void OnDisable()
        {
            ResetVisualOffset();
            ResetVerticalVelocity();
        }

        public void ResetVisualOffset()
        {
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = Vector3.zero;
            }
        }


        public void Init(CharacterEntity entity)

        {
            _entity = entity;
        }

        public void Move(Vector3 moveDelta)
        {
            if (_cc != null && _cc.enabled)
            {
                _cc.Move(moveDelta);
                return;
            }

            transform.position += moveDelta;
        }

        [Header("Root Motion")]
        public bool EnableRootMotion = true;

        private void OnAnimatorMove()
        {
            if (_animator == null)
            {
                return;
            }

            Vector3 deltaPosition = Vector3.zero;
            Quaternion deltaRotation = Quaternion.identity;
            
            if (_animator.applyRootMotion && EnableRootMotion)
            {
                deltaPosition = _animator.deltaPosition;
                deltaRotation = _animator.deltaRotation;
            }

            // --- 标准重力与贴地物理运动学解算 ---
            if (_cc != null && _cc.enabled)
            {
                if (_cc.isGrounded)
                {
                    // 接地且垂直速度向下时，保持持续向下的贴地吸附速度（-2.0f m/s），防止浮空与 isGrounded 判定抖动
                    if (_verticalVelocity < 0f)
                    {
                        _verticalVelocity = -2.0f;
                    }
                }
                else
                {
                    // 自由落体自由下落：v_y = v_0 + g * dt
                    _verticalVelocity += (Gravity * GravityScale) * Time.deltaTime;
                }

                deltaPosition.y += _verticalVelocity * Time.deltaTime;
            }

            ApplyRootMotion(deltaPosition);

            if (_animator.applyRootMotion && deltaRotation != Quaternion.identity)
            {
                transform.rotation *= deltaRotation;
            }
        }

        private void ApplyRootMotion(Vector3 deltaPosition)
        {
            //XZ变化分量
            Vector3 horizontalDelta = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            //Y变化分量
            Vector3 verticalDelta = Vector3.up * deltaPosition.y;
            //XZ转局部变化分量
            Vector3 rawLocalDelta = transform.InverseTransformDirection(horizontalDelta);
            //尝试过滤XZ局部变化分量
            Vector3 filteredLocalDelta = ApplyMotionWindowFilter(rawLocalDelta);
            
            //转回世界变化分量，并执行碰撞约束检测
            Vector3 desiredWorldDelta = transform.TransformDirection(filteredLocalDelta);
            Vector3 allowedWorldDelta = ResolveRootMotionCollision(desiredWorldDelta);

            Vector3 finalDelta = allowedWorldDelta + verticalDelta;
            if (finalDelta.sqrMagnitude > 0.000001f)
            {


                //应用有效变化
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(finalDelta);
                }
                else
                {
                    transform.position += finalDelta;
                }
            }
    
            //尝试应用视觉模型偏移 (依然使用未经约束的 rawLocalDelta，以产生受到阻挡时挤压的视觉效果)
            ApplyVisualOffset(rawLocalDelta);
        }

        private Vector3 ApplyMotionWindowFilter(Vector3 localDelta)
        {
            if (filterMode == MotionWindowLocalDeltaFilterMode.None)
            {
                return localDelta;
            }

            switch (filterMode)
            {
                case MotionWindowLocalDeltaFilterMode.ZeroLocalX:
                    localDelta.x = 0f;
                    break;
                case MotionWindowLocalDeltaFilterMode.ZeroLocalZ:
                    localDelta.z = 0f;
                    break;
                case MotionWindowLocalDeltaFilterMode.ZeroLocalXZ:
                    localDelta.x = 0f;
                    localDelta.z = 0f;
                    break;
            }

            return localDelta;
        }

        private Vector3 ResolveRootMotionCollision(Vector3 desiredWorldDelta)
        {
            if (_collisionMode == RootMotionCollisionMode.DefaultSlide ||
                _collisionMode == RootMotionCollisionMode.IgnorePreCheck)
            {
                return desiredWorldDelta;
            }

            float distance = desiredWorldDelta.magnitude;
            if (distance <= 0.000001f)
                return Vector3.zero;

            Vector3 direction = desiredWorldDelta / distance;

            if (!CapsuleCastMotion(direction, distance, out RaycastHit hit))
                return desiredWorldDelta;

            float allowedDistance = Mathf.Max(0f, hit.distance - _rootMotionSkin);
            return direction * allowedDistance;
        }

        private bool CapsuleCastMotion(Vector3 direction, float distance, out RaycastHit hit)
        {
            if (_cc == null)
            {
                hit = new RaycastHit();
                return false;
            }
            GetCCCapsuleEndpoints(out Vector3 p1, out Vector3 p2, out float radius);
            return Physics.CapsuleCast(p1, p2, radius, direction, out hit, distance,
                _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void GetCCCapsuleEndpoints(out Vector3 p1, out Vector3 p2, out float radius)
        {
            Vector3 center = transform.TransformPoint(_cc.center);
            // 半径自适应：用 CC 的 radius 减去 skinWidth，确保不会因为贴合造成假阳性
            radius = Mathf.Max(0.01f, _cc.radius - _cc.skinWidth);
            float height = Mathf.Max(_cc.height, radius * 2f);
            float halfH = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 up = transform.up;
            p1 = center + up * halfH;
            p2 = center - up * halfH;
        }

        private float _visualRecoverSpeed;
        private bool _isVisualRecoverActive;

        private void ApplyVisualOffset(Vector3 rawLocalDelta)
        {
            if (_visualRoot == null) return;

            Vector3 currentVisualPos = _visualRoot.localPosition;

            // 1. 矫正模式 (Recover Mode)
            if (_isVisualRecoverActive && currentVisualPos.sqrMagnitude > 0.000001f)
            {
                // 计算回正方向向量
                Vector3 dirToOrigin = -currentVisualPos.normalized;

                // 计算原始位移在回正方向上的投能量
                float p = Vector3.Dot(rawLocalDelta, dirToOrigin);

                // 算法逻辑：
                // 如果 p > 0 (朝向原点)，保留该分量；如果 p <= 0 (背离原点)，设为 0（拒绝远离）。
                Vector3 filteredDelta = Mathf.Max(0, p) * dirToOrigin;

                // 附加基础回收速度，确保逻辑最终必归原点
                filteredDelta += dirToOrigin * (_visualRecoverSpeed * Time.deltaTime);

                // 应用最终位移，并防止超调（Overshoot）
                Vector3 nextPos = currentVisualPos + filteredDelta;
                if (Vector3.Dot(-nextPos, dirToOrigin) < 0) // 说明跨过了原点
                {
                    nextPos = Vector3.zero;
                }
                
                _visualRoot.localPosition = nextPos;
                return;
            }

            // 2. 标准模式 (Standard Offset Mode)
            if (visualOffsetMode == MotionWindowVisualOffsetMode.None) return;

            Vector3 visualHorizentalOffsetDalta = Vector3.zero;
            switch(visualOffsetMode)
            {
                case MotionWindowVisualOffsetMode.X:
                    visualHorizentalOffsetDalta.x = rawLocalDelta.x;
                    break;
                case MotionWindowVisualOffsetMode.Z:
                    visualHorizentalOffsetDalta.z = rawLocalDelta.z;
                    break;
                case MotionWindowVisualOffsetMode.XZ:
                    visualHorizentalOffsetDalta.x = rawLocalDelta.x;
                    visualHorizentalOffsetDalta.z = rawLocalDelta.z;
                    break;
            }



            _visualRoot.localPosition += visualHorizentalOffsetDalta;
        }

        public void SetVisualRecover(bool active, float speed = 0f)
        {
            _isVisualRecoverActive = active;
            _visualRecoverSpeed = speed;
        }
        public void SetFilterMode(MotionWindowLocalDeltaFilterMode filterMode)
        {
            this.filterMode = filterMode; 
        }
        public void SetCollisionMode(RootMotionCollisionMode mode)
        {
            _collisionMode = mode;
        }
        public void SetObstacleMask(LayerMask mask)
        {
            _obstacleMask = mask;
        }
        public void SetVisualOffsetMode(MotionWindowVisualOffsetMode visualOffsetMode)
        {
            this.visualOffsetMode = visualOffsetMode;
        }

        public void RotateTo(Vector3 worldDirection, float speed = -1f, Vector3 localOffset = default)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : (speed > 0 ? speed : TurnSpeed);
                Quaternion targetRotation = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(localOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }

        public void RotateToImmediately(Vector3 worldDirection, Vector3 localOffset = default)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(localOffset);
            }
        }

        public void FaceTo(Vector2 inputDir, float speed = -1f, Vector3 localOffset = default)
        {
            Vector3 lookDirection = CalculateWorldDirection(inputDir);
            RotateTo(lookDirection, speed, localOffset);
        }

        public void FaceTo(Vector3 worldDir, float speed = -1f, Vector3 localOffset = default)
        {
            RotateTo(worldDir, speed, localOffset);
        }

        public void FaceToImmediately(Vector2 inputDir, Vector3 localOffset = default)
        {
            Vector3 lookDirection = CalculateWorldDirection(inputDir);
            RotateToImmediately(lookDirection, localOffset);
        }

        public void FaceToImmediately(Vector3 worldDir, Vector3 localOffset = default)
        {
            RotateToImmediately(worldDir, localOffset);
        }

        public void FaceToTarget(Transform target, float speed = -1f, Vector3 localOffset = default)
        {
            if (target == null) return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            RotateTo(direction, speed, localOffset);
        }

        public void FaceToTargetImmediately(Transform target, Vector3 localOffset = default)
        {
            if (target == null) return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            RotateToImmediately(direction, localOffset);
        }

        public Vector3 CalculateWorldDirection(Vector2 inputDir)
        {
            if (_entity is RoleEntity role && role.CameraController != null)
            {
                Vector3 camForward = role.CameraController.GetForward();
                Vector3 camRight = role.CameraController.GetRight();
                return (camForward * inputDir.y + camRight * inputDir.x).normalized;
            }

            return new Vector3(inputDir.x, 0f, inputDir.y).normalized;
        }

        public bool IsGrounded
        {
            get
            {
                if (_cc != null)
                {
                    return _cc.isGrounded;
                }

                return true;
            }
        }
    }
}
