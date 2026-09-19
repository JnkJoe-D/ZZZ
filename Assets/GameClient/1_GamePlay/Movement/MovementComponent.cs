using System.Diagnostics;
using cfg;
using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementComponent : MonoBehaviour, IMovementComponent
    {
        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private IVisualOffsetPresenter _visualOffsetPresenter;
        private IVisualOffsetPresenter VisualOffsetPresenter => 
            _visualOffsetPresenter ??= GetComponent<IVisualOffsetPresenter>();

        public CharacterEntity OwnerEntity => _entity;

        public float TurnSpeed = 15f;
        float IMovementComponent.TurnSpeed
        {
            get => TurnSpeed;
            set => TurnSpeed = value;
        }

        public float Gravity => -9.81f;
        public float GravityScale = 1.0f;
        public float PushResistance = 0f;
        public CharacterController CharacterController => _cc;

        public float CharacterRadius
        {
            get
            {
                if (_cc != null)
                {
                    return _cc.radius + _cc.skinWidth;
                }
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    return capsule.radius;
                }
                return 0.5f;
            }
        }

        public float CharacterHeight
        {
            get
            {
                if (_cc != null)
                {
                    return _cc.height;
                }
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    return capsule.height;
                }
                return 2.0f;
            }
        }

        public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;

        private float _verticalVelocity;
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
        private RootMotionCollisionMode _collisionMode = RootMotionCollisionMode.DefaultSlide;
        private LayerMask _obstacleMask = ~0;
        
        [SerializeField] private float _rootMotionSkin = 0.01f;

        public void OnComponentInit(CharacterEntity owner)
        {
            _entity = owner;
            if (_visualOffsetPresenter == null)
            {
                _visualOffsetPresenter = GetComponent<IVisualOffsetPresenter>();
            }
            if (owner != null && owner.Config != null)
            {
                ApplyCapsuleConfig(owner.Config);
            }
        }

        public void OnComponentSpawn()
        {
            ResetVisualOffset();
            ResetVerticalVelocity();
        }

        public void OnComponentDespawn()
        {
            ResetVisualOffset();
            ResetVerticalVelocity();
        }

        private void Awake()
        {
            _visualOffsetPresenter = GetComponent<IVisualOffsetPresenter>();

            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc != null)
            {
                ApplyDefaultCapsuleSettings();
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

        private void ApplyDefaultCapsuleSettings()
        {
            if (_cc == null) return;
            _cc.height = 1.6f;
            _cc.radius = 0.3f;
            _cc.center = new Vector3(0f, 0.8f, 0f);
            _cc.skinWidth = 0.015f;
            _cc.minMoveDistance = 0f;
            _cc.excludeLayers = LayerMask.GetMask("Player");
        }

        public void ApplyCapsuleConfig(CharacterConfigAsset config)
        {
            if (_cc == null || config == null) return;
            float height = config.GroundHeight > 0f ? config.GroundHeight : 1.6f;
            float radius = config.GroundRadius > 0f ? config.GroundRadius : 0.3f;
            _cc.height = height;
            _cc.radius = radius;
            _cc.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private void OnDisable()
        {
            ResetVisualOffset();
            ResetVerticalVelocity();
        }

        public void ResetVisualOffset()
        {
            VisualOffsetPresenter?.ResetVisualOffset();
        }

        public void Init(CharacterEntity entity)
        {
            _entity = entity;
            if (entity != null && entity.Config != null)
            {
                ApplyCapsuleConfig(entity.Config);
            }
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

            // --- 标准重力与贴地物理运动学解算（使用实体专属时钟有效流速与引擎原生帧步长） ---
            if (_cc != null && _cc.enabled)
            {
                // OnAnimatorMove 属于 Unity 原生每帧物理驱动，基准步长为 Time.deltaTime
                // 必须乘以宿主实体的私有时钟流速 EffectiveScale，保证慢动作/顿帧下位移与动画严格同步，杜绝太空步
                float effectiveScale = _entity != null && _entity.Clock != null ? _entity.Clock.EffectiveScale : 1.0f;
                float scaledDelta = Time.deltaTime * effectiveScale;

                if (_cc.isGrounded)
                {
                    // 接地且垂直速度向下时，保持持续向下的贴地吸附速度（-2.0f m/s），防止浮空与 isGrounded 判定抖动
                    if (_verticalVelocity < 0f)
                    {
                        _verticalVelocity = -2.0f;
                    }
                }
                else if (effectiveScale > 0f)
                {
                    // 自由落体自由下落：v_y = v_0 + g * dt
                    _verticalVelocity += (Gravity * GravityScale) * scaledDelta;
                }

                deltaPosition.y += _verticalVelocity * scaledDelta;
            }

            ApplyRootMotion(deltaPosition);

            if (_animator.applyRootMotion && deltaRotation != Quaternion.identity)
            {
                transform.rotation *= deltaRotation;
            }
        }

        private void ApplyRootMotion(Vector3 deltaPosition)
        {
            // XZ 变化分量
            Vector3 horizontalDelta = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            // Y 变化分量
            Vector3 verticalDelta = Vector3.up * deltaPosition.y;
            // XZ 转局部变化分量
            Vector3 rawLocalDelta = transform.InverseTransformDirection(horizontalDelta);
            // 尝试过滤 XZ 局部变化分量
            Vector3 filteredLocalDelta = ApplyMotionWindowFilter(rawLocalDelta);
            
            // 转回世界变化分量，并执行碰撞约束检测
            Vector3 desiredWorldDelta = transform.TransformDirection(filteredLocalDelta);
            Vector3 allowedWorldDelta = ResolveRootMotionCollision(desiredWorldDelta);

            Vector3 finalDelta = allowedWorldDelta + verticalDelta;
            if (finalDelta.sqrMagnitude > 0.000001f)
            {
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(finalDelta);
                }
                else
                {
                    transform.position += finalDelta;
                }
            }
    
            // 尝试应用视觉模型偏移 (依然使用未经约束的 rawLocalDelta，以产生受到阻挡时挤压的视觉效果)
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

        private void ApplyVisualOffset(Vector3 rawLocalDelta)
        {
            VisualOffsetPresenter?.ApplyVisualOffset(rawLocalDelta);
        }

        public void SetVisualRecover(bool active, float speed = 0f)
        {
            VisualOffsetPresenter?.SetVisualRecover(active, speed);
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
            VisualOffsetPresenter?.SetVisualOffsetMode(visualOffsetMode);
        }

        public void RotateTo(Vector3 worldDirection, float speed = -1f, Vector3 localOffset = default, float dt = -1f)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : (speed > 0 ? speed : TurnSpeed);
                if (dt <= 0f)
                {
                    dt = _entity != null && _entity.Clock != null ? _entity.Clock.DeltaTime : Time.deltaTime;
                }
                Quaternion targetRotation = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(localOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dt * speed);
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
