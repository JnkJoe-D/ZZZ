using System.Diagnostics;
using cfg;
using Game.AI;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private Transform _visualRoot;

        public float TurnSpeed = 15f;
        public float Gravity => -9.81f;

        MotionWindowLocalDeltaFilterMode filterMode = MotionWindowLocalDeltaFilterMode.None;
        private MotionWindowVisualOffsetMode visualOffsetMode = MotionWindowVisualOffsetMode.None;
        private void Awake()
        {
            _visualRoot = transform.Find("Visual");
            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();
                _cc.height = 1.6f;
                _cc.radius = 0.3f;
                _cc.center = new Vector3(0f, 0.88f, 0f);
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

        private void OnAnimatorMove()
        {
            if (_animator == null)
            {
                return;
            }

            Vector3 deltaPosition = Vector3.zero;
            Quaternion deltaRotation = Quaternion.identity;
            if (_animator.applyRootMotion)
            {
                deltaPosition = _animator.deltaPosition;
                deltaRotation = _animator.deltaRotation;
            }

            if (_cc != null && !_cc.isGrounded)
            {
                deltaPosition += Vector3.up * Gravity * Time.deltaTime * Time.deltaTime;
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
            
            //转回世界变化分量
            Vector3 finalDelta = transform.TransformDirection(filteredLocalDelta) + verticalDelta;
            if (finalDelta.sqrMagnitude <= 0.000001f)
            {
                return;
            }
            //应用有效变化
            if (_cc != null && _cc.enabled)
            {
                _cc.Move(finalDelta);
            }
            else
            {
                transform.position += finalDelta;
            }
            //尝试应用视觉模型偏移
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
        public void SetVisualOffsetMode(MotionWindowVisualOffsetMode visualOffsetMode)
        {
            this.visualOffsetMode = visualOffsetMode;
        }
        public void FaceTo(Vector3 inputDir, float speed = -1f)
        {
            Vector3 lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : (speed > 0 ? speed : TurnSpeed);
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }

        public void FaceToImmediately(Vector3 inputDir)
        {
            Vector3 lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.forward = lookDirection.normalized;
            }
        }

        public void FaceToTarget(Transform target, float speed = -1f)
        {
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : (speed > 0 ? speed : TurnSpeed);
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }

        public void FaceToTargetImmediately(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.forward = direction.normalized;
            }
        }

        public Vector3 CalculateWorldDirection(Vector2 inputDir)
        {
            if (_entity.InputProvider is AIInputProvider aiInputProvider &&
                aiInputProvider.TryGetWorldMovementDirection(out Vector3 aiWorldDirection))
            {
                return aiWorldDirection.normalized;
            }

            if (_entity.CameraController != null)
            {
                Vector3 camForward = _entity.CameraController.GetForward();
                Vector3 camRight = _entity.CameraController.GetRight();
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
