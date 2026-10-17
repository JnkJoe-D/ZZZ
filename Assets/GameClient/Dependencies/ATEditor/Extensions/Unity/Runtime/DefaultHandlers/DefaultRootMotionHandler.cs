using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 默认运动窗口与根运动约束处理器。
    /// 统一实现 IMotionWindowHandler，负责管理本地位移滤波、碰撞模式与旋转约束窗口。
    /// </summary>
    public sealed class DefaultRootMotionHandler : IMotionWindowHandler, IDisposable
    {
        private struct RotationWindowState
        {
            public RotationFilterClip Clip;
            public Quaternion EnterRotation;
            public float Yaw;
            public float Sign;
        }

        private readonly Transform _owner;
        private readonly ITransformHandler _motor;
        private readonly AnimatorRootMotionBridge _bridge;

        private readonly List<MotionWindowLocalDeltaFilterMode> _deltaFilters = new List<MotionWindowLocalDeltaFilterMode>();
        private readonly List<(RootMotionCollisionMode mode, LayerMask mask)> _collisionModes = new List<(RootMotionCollisionMode, LayerMask)>();
        private readonly List<RotationWindowState> _rotationWindows = new List<RotationWindowState>();

        private RaycastHit[] _hits = new RaycastHit[16];
        private bool _disposed;
        private bool _isSeeking;

        public bool IsSeeking
        {
            get => _isSeeking;
            set { _isSeeking = value; if (value) _alignDuration = 0f; }
        }

        private float _alignTime, _alignDuration;
        private Quaternion _alignStart, _alignTarget;

        public DefaultRootMotionHandler(Transform owner, ITransformHandler motor)
        {
            _owner = owner;
            _motor = motor;

            if (_owner != null)
            {
                var animator = _owner.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    var bridge = animator.GetComponent<AnimatorRootMotionBridge>();
                    if (bridge == null) bridge = animator.gameObject.AddComponent<AnimatorRootMotionBridge>();
                    bridge.Bind(this, anim =>
                    {
                        ApplyRootMotion(anim.deltaPosition, anim.deltaRotation);
                    });
                    _bridge = bridge;
                }
            }
        }

        private void UpdateCaptureState()
        {
            if (_bridge != null)
            {
                _bridge.Capture = _deltaFilters.Count > 0 || _collisionModes.Count > 0 || _rotationWindows.Count > 0;
            }
        }

        #region IMotionWindowHandler 实现

        public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode)
        {
            if (_disposed) return;
            _deltaFilters.Add(filterMode);
            UpdateCaptureState();
        }

        public void DisableLocalDeltaFilter()
        {
            if (_disposed || _deltaFilters.Count == 0) return;
            _deltaFilters.RemoveAt(_deltaFilters.Count - 1);
            UpdateCaptureState();
        }

        public void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask)
        {
            if (_disposed) return;
            _collisionModes.Add((mode, obstacleMask));
            UpdateCaptureState();
        }

        public void DisableCollisionMode()
        {
            if (_disposed || _collisionModes.Count == 0) return;
            _collisionModes.RemoveAt(_collisionModes.Count - 1);
            UpdateCaptureState();
        }

        public void EnableRotationFilter(RotationFilterClip clipData)
        {
            if (clipData == null || _disposed) return;
            _alignDuration = 0f;
            var state = new RotationWindowState
            {
                Clip = clipData,
                EnterRotation = _motor?.GetRotation() ?? (_owner != null ? _owner.rotation : Quaternion.identity)
            };
            _rotationWindows.Add(state);
            UpdateCaptureState();
        }

        public void DisableRotationFilter()
        {
            if (_disposed || _rotationWindows.Count == 0) return;
            int lastIndex = _rotationWindows.Count - 1;
            var window = _rotationWindows[lastIndex];
            _rotationWindows.RemoveAt(lastIndex);
            UpdateCaptureState();

            var clip = window.Clip;
            if (_rotationWindows.Count > 0 || IsSeeking || clip == null || clip.axisMask.FilterY || clip.exitAlignMode == RotationExitAlignMode.None || _motor == null)
                return;

            Quaternion target;
            if (clip.exitAlignMode == RotationExitAlignMode.SnapToInputDirection)
            {
                var direction = _motor.GetInputDirection(true);
                if (direction.sqrMagnitude < 0.000001f) return;
                target = Quaternion.LookRotation(direction, Vector3.up);
            }
            else
            {
                target = window.EnterRotation * Quaternion.Euler(0f, (window.Sign < 0f ? -1f : 1f) * clip.targetRelativeYaw, 0f);
            }

            _alignTarget = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            _alignStart = _motor.GetRotation();
            _alignTime = 0f;
            _alignDuration = Mathf.Max(0f, clip.alignBlendDuration);
            if (_alignDuration == 0f)
            {
                _motor.SetRotation(_alignTarget);
            }
        }

        public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode) { }
        public void DisableVisualOffset() { }
        public void EnableVisualOffsetRecover(float speed) { }
        public void DisableVisualOffsetRecover() { }
        public void EnableVisualRotationOffset(VisualRotationOffsetClip clipData) { }
        public void UpdateVisualRotationOffset(float normalizedTime) { }
        public void DisableVisualRotationOffset() { }

        #endregion

        #region 根运动位移与旋转应用 (供测试或自驱动使用)

        public void Tick(float deltaTime)
        {
            if (_disposed || _alignDuration <= 0f || IsSeeking || _motor == null) return;
            _alignTime += deltaTime;
            _motor.SetRotation(Quaternion.Slerp(_alignStart, _alignTarget, Mathf.Clamp01(_alignTime / _alignDuration)));
            if (_alignTime >= _alignDuration) _alignDuration = 0f;
        }

        public void CompleteAlignment()
        {
            if (_disposed || _alignDuration <= 0f || _motor == null) return;
            _motor.SetRotation(_alignTarget);
            _alignDuration = 0f;
        }

        public void ApplyRootMotion(Vector3 worldDelta, Quaternion localRotationDelta)
        {
            if (_disposed || IsSeeking || _motor == null) return;

            // 位移过滤与碰撞
            if (_deltaFilters.Count > 0 || _collisionModes.Count > 0)
            {
                var filterMode = _deltaFilters.Count > 0 ? _deltaFilters[_deltaFilters.Count - 1] : MotionWindowLocalDeltaFilterMode.None;
                var (collisionMode, obstacleMask) = _collisionModes.Count > 0 ? _collisionModes[_collisionModes.Count - 1] : (RootMotionCollisionMode.DefaultSlide, (LayerMask)0);

                var local = Quaternion.Inverse(_motor.GetRotation()) * worldDelta;
                if (filterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalX || filterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalXZ) local.x = 0f;
                if (filterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalZ || filterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalXZ) local.z = 0f;
                worldDelta = _motor.GetRotation() * local;

                if (collisionMode == RootMotionCollisionMode.StopAtObstacle)
                {
                    worldDelta = ClampToObstacle(worldDelta, obstacleMask);
                }
            }

            if (worldDelta.sqrMagnitude > 0f)
            {
                _motor.Move(worldDelta);
            }

            // 旋转过滤
            if (_rotationWindows.Count > 0)
            {
                int topIdx = _rotationWindows.Count - 1;
                var window = _rotationWindows[topIdx];
                var settings = window.Clip;
                var euler = localRotationDelta.eulerAngles;
                float yaw = settings.axisMask.FilterY ? 0f : Mathf.DeltaAngle(0f, euler.y);
                if (settings.lockToSingleDirection && Mathf.Abs(yaw) > 0.0001f)
                {
                    if (window.Sign == 0f) window.Sign = Mathf.Sign(yaw);
                    else if (Mathf.Sign(yaw) != window.Sign) yaw = 0f;
                }
                if (settings.enableMaxAngleLimit && settings.maxAccumulatedAngle > 0f)
                {
                    yaw = Mathf.Clamp(window.Yaw + yaw, -settings.maxAccumulatedAngle, settings.maxAccumulatedAngle) - window.Yaw;
                }
                window.Yaw += yaw;
                _rotationWindows[topIdx] = window;

                localRotationDelta = Quaternion.Euler(
                    settings.axisMask.FilterX ? 0f : Mathf.DeltaAngle(0f, euler.x),
                    yaw,
                    settings.axisMask.FilterZ ? 0f : Mathf.DeltaAngle(0f, euler.z));
            }

            _motor.SetRotation(_motor.GetRotation() * localRotationDelta);
        }

        private Vector3 ClampToObstacle(Vector3 delta, LayerMask mask)
        {
            if (_owner == null || _motor == null) return delta;
            float distance = delta.magnitude;
            if (distance <= 0.000001f) return Vector3.zero;
            float radius = Mathf.Max(0.01f, _motor.GetRadius());
            float half = Mathf.Max(0f, _motor.GetHeight() * 0.5f - radius);
            var center = _owner.TransformPoint(_motor.GetCenter());
            int count;
            do
            {
                count = Physics.CapsuleCastNonAlloc(center + _owner.up * half, center - _owner.up * half, radius,
                    delta / distance, _hits, distance, mask, QueryTriggerInteraction.Ignore);
                if (count < _hits.Length) break;
                Array.Resize(ref _hits, _hits.Length * 2);
            } while (true);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider != null && !_hits[i].collider.transform.IsChildOf(_owner))
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, _hits[i].distance - 0.01f));
            }
            return delta * (allowed / distance);
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _bridge?.Unbind(this);
            _deltaFilters.Clear();
            _collisionModes.Clear();
            _rotationWindows.Clear();
            _alignDuration = 0f;
        }
    }
}
