using cfg;
using Game.AI;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        [SerializeField] private Color _constraintBoxGizmoColor = new Color(0.2f, 0.8f, 1f, 0.2f);
        [SerializeField] private Color _constraintBoxBoundaryPointColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] private Color _constraintBoxFrontFaceColor = new Color(1f, 0.35f, 0.25f, 0.95f);
        [SerializeField] private Color _constraintBoxNearestBoundaryColor = new Color(0.2f, 1f, 0.3f, 0.95f);
        [SerializeField] private Color _constraintBoxFarthestBoundaryColor = new Color(0.9f, 0.2f, 1f, 0.95f);
        [SerializeField] private float _verticalRootMotionThreshold = 0.0001f;
        [SerializeField] private bool _debugConstraintBox;

        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private ISkillMotionWindowHandler _motionWindowHandler;
        private float _verticalVelocity;
        private LayerMask _defaultExcludeLayers;
        private string _lastLoggedSnapClipId;
        private MotionWindowRuntimeData _stickyMotionWindowRuntimeData;

        public float TurnSpeed = 15f;
        public float Gravity => -9.81f;

        private void Awake()
        {
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

            if (_cc != null)
            {
                _defaultExcludeLayers = _cc.excludeLayers;
            }
        }

        private void Update()
        {
            RefreshMotionWindowCollisionOverride();
        }

        private void OnDisable()
        {
            RestoreDefaultExcludeLayers();
            _stickyMotionWindowRuntimeData = null;
        }

        public void Init(CharacterEntity entity)
        {
            _entity = entity;
            _motionWindowHandler = entity?.SkillMotionWindowHandler;
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

            bool hasVerticalRootMotion = Mathf.Abs(deltaPosition.y) > _verticalRootMotionThreshold;
            if (_cc != null && _cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = 0;
            }
            else if (!hasVerticalRootMotion)
            {
                _verticalVelocity += 0;
            }
            else
            {
                _verticalVelocity = 0f;
            }

            Vector3 gravityDelta = hasVerticalRootMotion
                ? Vector3.zero
                : Vector3.up * _verticalVelocity * Time.deltaTime;

            bool hasAnimatorMotion = deltaPosition.sqrMagnitude > 0.000001f || deltaRotation != Quaternion.identity;
            if (!hasAnimatorMotion && gravityDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            ApplyRootMotion(deltaPosition + gravityDelta);

            if (_animator.applyRootMotion)
            {
                ApplyRootRotation(deltaRotation);
            }
        }

        private void ApplyRootMotion(Vector3 deltaPosition)
        {
            Vector3 horizontalDelta = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            horizontalDelta = ApplyHitReactionAxisConstraint(horizontalDelta);
            Vector3 verticalDelta = Vector3.up * deltaPosition.y;

            if (TryApplyMotionWindow(horizontalDelta, verticalDelta))
            {
                return;
            }

            if (_cc != null && _cc.enabled)
            {
                _cc.Move(horizontalDelta + verticalDelta);
            }
            else
            {
                Debug.Log($"ApplyRootMotion1:{horizontalDelta.x}");

                transform.position += horizontalDelta + verticalDelta;
            }
        }

        private bool TryApplyMotionWindow(Vector3 horizontalDelta, Vector3 verticalDelta)
        {
            if (!TryResolveMotionWindowRuntimeData(out MotionWindowRuntimeData runtimeData))
            {
                return false;
            }

            LogConstraintBoxWindowActive(runtimeData);
            Vector3 filteredHorizontalDelta = ApplyMotionWindowConstraintBox(runtimeData, horizontalDelta);
            if (_cc != null && _cc.enabled)
            {
                Vector3 currentRootPosition = transform.position;
                Vector3 resolvedRootPosition = currentRootPosition + filteredHorizontalDelta;
                transform.position = new Vector3(
                    resolvedRootPosition.x,
                    currentRootPosition.y,
                    resolvedRootPosition.z);

                if (verticalDelta.sqrMagnitude > 0.0000001f)
                {
                    _cc.Move(verticalDelta);
                    transform.position = new Vector3(
                        resolvedRootPosition.x,
                        transform.position.y,
                        resolvedRootPosition.z);
                }
            }
            else
            {
                Debug.Log($"ApplyRootMotion2:{filteredHorizontalDelta.x}");

                transform.position += filteredHorizontalDelta + verticalDelta;
            }

            ApplyMotionWindowResolvedPositionGuard(runtimeData);
            return true;
        }

        private bool TryResolveMotionWindowRuntimeData(out MotionWindowRuntimeData runtimeData)
        {
            runtimeData = null;
            if (_motionWindowHandler != null &&
                _motionWindowHandler.TryGetActiveWindow(out MotionWindowRuntimeData activeRuntimeData) &&
                activeRuntimeData?.Clip != null &&
                activeRuntimeData.Clip.constraintMode == MotionWindowConstraintMode.ConstraintBox)
            {
                _stickyMotionWindowRuntimeData = activeRuntimeData;
                runtimeData = activeRuntimeData;
                return true;
            }

            if (CanReuseStickyMotionWindow())
            {
                runtimeData = _stickyMotionWindowRuntimeData;
                return runtimeData?.Clip != null;
            }

            _stickyMotionWindowRuntimeData = null;
            _lastLoggedSnapClipId = null;
            return false;
        }

        private bool CanReuseStickyMotionWindow()
        {
            if (_stickyMotionWindowRuntimeData?.Clip == null || _entity?.RuntimeData == null)
            {
                return false;
            }

            // MotionWindow 的 enter/exit 与 OnAnimatorMove 不是同一时机。
            // 动作首尾有时会出现 1 帧左右的窗口空档，这里在技能/后摇阶段复用最近一次有效窗口，避免漏过滤。
            CommandContextType contextType = _entity.RuntimeData.CurrentCommandContext;
            return contextType == CommandContextType.Skill ||
                   contextType == CommandContextType.Backswing;
        }

        private Vector3 ApplyMotionWindowConstraintBox(MotionWindowRuntimeData runtimeData, Vector3 horizontalDelta)
        {
            if (!MotionConstraintBoxUtility.TryGetConstraintBox(runtimeData, transform, out MotionConstraintBoxData boxData))
            {
                return horizontalDelta;
            }

            Vector3 filteredHorizontalDelta = ApplyMotionWindowLocalDeltaFilter(runtimeData.Clip, boxData, horizontalDelta);
            Vector3 currentRootPosition = transform.position;
            Vector3 candidateRootPosition = currentRootPosition + filteredHorizontalDelta;
            Vector3 centerOffsetWorld = GetCapsuleCenterOffsetWorld();
            float horizontalRadius = GetCapsuleRadius();
            Vector3 clampedRootPosition = candidateRootPosition;
            if (runtimeData.Clip.constraintBoxMode == MotionConstraintBoxMode.Block)
            {
                if (!MotionConstraintBoxUtility.TryRestrictRootPositionToBox(
                        boxData,
                        currentRootPosition,
                        candidateRootPosition,
                        centerOffsetWorld,
                        horizontalRadius,
                        out clampedRootPosition))
                {
                    return ApplyMotionWindowResolvedDeltaGuard(runtimeData.Clip, filteredHorizontalDelta);
                }
            }
            else
            {
                if (!runtimeData.HasAppliedEnterSnap)
                {
                    clampedRootPosition = MotionConstraintBoxUtility.SnapRootPositionToBoxBoundary(
                        boxData,
                        currentRootPosition,
                        currentRootPosition,
                        centerOffsetWorld,
                        horizontalRadius);
                    runtimeData.HasAppliedEnterSnap = true;
                    LogConstraintBoxSnap(runtimeData, boxData, currentRootPosition, clampedRootPosition, centerOffsetWorld, horizontalRadius);
                }
                else
                {
                    return ApplyMotionWindowResolvedDeltaGuard(runtimeData.Clip, filteredHorizontalDelta);
                }

            }

            Vector3 clampedDelta = clampedRootPosition - currentRootPosition;
            clampedDelta.y = 0f;
            return ApplyMotionWindowResolvedDeltaGuard(runtimeData.Clip, clampedDelta);
        }

        private Vector3 ApplyMotionWindowLocalDeltaFilter(
            MotionWindowClip clip,
            MotionConstraintBoxData boxData,
            Vector3 horizontalDelta)
        {
            if (clip == null ||
                clip.localDeltaFilterMode == MotionWindowLocalDeltaFilterMode.None ||
                horizontalDelta.sqrMagnitude <= 0.0000001f)
            {
                return horizontalDelta;
            }

            // 先转到 MotionWindow 的本地坐标里过滤，再回到世界坐标。
            Quaternion inverseRotation = Quaternion.Inverse(boxData.Rotation);
            Vector3 localDelta = inverseRotation * horizontalDelta;
            switch (clip.localDeltaFilterMode)
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

            Vector3 filteredHorizontalDelta = boxData.Rotation * localDelta;
            filteredHorizontalDelta.y = 0f;
            return filteredHorizontalDelta;
        }

        private Vector3 ApplyMotionWindowResolvedDeltaGuard(MotionWindowClip clip, Vector3 resolvedHorizontalDelta)
        {
            if (clip == null)
            {
                return resolvedHorizontalDelta;
            }

            // 最终兜底：做最小验证时，允许直接把世界 X 增量清零。
            if (clip.localDeltaFilterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalX ||
                clip.localDeltaFilterMode == MotionWindowLocalDeltaFilterMode.ZeroLocalXZ)
            {
                if (_debugConstraintBox && Mathf.Abs(resolvedHorizontalDelta.x) > 0.00001f)
                {
                    Debug.LogWarning(
                        "[ConstraintBoxDebug] Final World X Drift\n" +
                        $"clipId={clip.clipId}\n" +
                        $"worldDeltaBeforeGuard={resolvedHorizontalDelta}");
                }

                resolvedHorizontalDelta.x = 0f;
            }

            return resolvedHorizontalDelta;
        }

        private void ApplyMotionWindowResolvedPositionGuard(MotionWindowRuntimeData runtimeData)
        {
            if (runtimeData?.Clip == null)
            {
                return;
            }

            MotionWindowClip clip = runtimeData.Clip;
            if (clip.localDeltaFilterMode == MotionWindowLocalDeltaFilterMode.None)
            {
                return;
            }

            Quaternion referenceRotation = GetMotionWindowReferenceRotation(runtimeData);
            Vector3 currentPosition = transform.position;
            Vector3 localOffset = Quaternion.Inverse(referenceRotation) * (currentPosition - runtimeData.StartPosition);
            Vector3 guardedLocalOffset = localOffset;

            switch (clip.localDeltaFilterMode)
            {
                case MotionWindowLocalDeltaFilterMode.ZeroLocalX:
                    guardedLocalOffset.x = 0f;
                    break;

                case MotionWindowLocalDeltaFilterMode.ZeroLocalZ:
                    guardedLocalOffset.z = 0f;
                    break;

                case MotionWindowLocalDeltaFilterMode.ZeroLocalXZ:
                    guardedLocalOffset.x = 0f;
                    guardedLocalOffset.z = 0f;
                    break;
            }

            if ((guardedLocalOffset - localOffset).sqrMagnitude <= 0.0000001f)
            {
                return;
            }

            if (_debugConstraintBox)
            {
                Debug.LogWarning(
                    "[ConstraintBoxDebug] Final Position Guard\n" +
                    $"clipId={clip.clipId}\n" +
                    $"localOffsetBeforeGuard={localOffset}\n" +
                    $"localOffsetAfterGuard={guardedLocalOffset}");
            }

            Vector3 guardedPosition = runtimeData.StartPosition + referenceRotation * guardedLocalOffset;
            transform.position = new Vector3(guardedPosition.x, currentPosition.y, guardedPosition.z);
        }

        private Quaternion GetMotionWindowReferenceRotation(MotionWindowRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                return Quaternion.identity;
            }

            Vector3 referenceForward = runtimeData.ReferenceForward;
            referenceForward.y = 0f;
            if (referenceForward.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(referenceForward.normalized, Vector3.up);
            }

            Vector3 startForward = runtimeData.StartRotation * Vector3.forward;
            startForward.y = 0f;
            if (startForward.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(startForward.normalized, Vector3.up);
            }

            return Quaternion.identity;
        }

        private Vector3 ApplyHitReactionAxisConstraint(Vector3 horizontalDelta)
        {
            if (_entity?.RuntimeData == null ||
                _entity.RuntimeData.CurrentCommandContext != CommandContextType.HitStun ||
                !_entity.RuntimeData.HasHitReactionAxis)
            {
                return horizontalDelta;
            }

            Vector3 hitAxis = _entity.RuntimeData.CurrentHitReactionAxis;
            hitAxis.y = 0f;
            if (hitAxis.sqrMagnitude <= 0.0001f || horizontalDelta.sqrMagnitude <= 0.0000001f)
            {
                return horizontalDelta;
            }

            // 受击期间把动画水平位移投影到稳定受击轴上，吃掉左右漂移。
            return Vector3.Project(horizontalDelta, hitAxis.normalized);
        }

        private void ApplyRootRotation(Quaternion deltaRotation)
        {
            if (deltaRotation == Quaternion.identity)
            {
                return;
            }

            transform.rotation *= deltaRotation;
        }

        private Vector3 GetCapsuleCenterOffsetWorld()
        {
            if (_cc == null)
            {
                return Vector3.zero;
            }

            return transform.TransformVector(_cc.center);
        }

        private float GetCapsuleRadius()
        {
            return _cc != null
                ? Mathf.Max(_cc.radius + _cc.skinWidth, 0.01f)
                : 0.3f;
        }

        private void RefreshMotionWindowCollisionOverride()
        {
            if (_cc == null)
            {
                return;
            }

            LayerMask targetExcludeLayers = _defaultExcludeLayers;
            if (_motionWindowHandler != null &&
                _motionWindowHandler.TryGetActiveWindow(out MotionWindowRuntimeData runtimeData) &&
                runtimeData?.Clip != null &&
                runtimeData.Clip.constraintMode == MotionWindowConstraintMode.IgnoreCollision)
            {
                targetExcludeLayers |= runtimeData.Clip.ignoreCollisionLayers;
            }

            if (_cc.excludeLayers != targetExcludeLayers)
            {
                _cc.excludeLayers = targetExcludeLayers;
            }
        }

        private void RestoreDefaultExcludeLayers()
        {
            if (_cc != null && _cc.excludeLayers != _defaultExcludeLayers)
            {
                _cc.excludeLayers = _defaultExcludeLayers;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_motionWindowHandler != null &&
                _motionWindowHandler.TryGetActiveWindow(out MotionWindowRuntimeData runtimeData) &&
                runtimeData?.Clip != null &&
                runtimeData.Clip.constraintMode == MotionWindowConstraintMode.ConstraintBox &&
                MotionConstraintBoxUtility.TryGetConstraintBox(runtimeData, transform, out MotionConstraintBoxData boxData))
            {
                DrawConstraintBox(boxData, runtimeData.Clip.debugColor.a > 0f ? runtimeData.Clip.debugColor : _constraintBoxGizmoColor);
            }
        }

        private void DrawConstraintBox(MotionConstraintBoxData boxData, Color color)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = Matrix4x4.TRS(boxData.Center, boxData.Rotation, Vector3.one);
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, boxData.Size);
            Color solidColor = color;
            solidColor.a = Mathf.Min(color.a * 0.35f, 0.2f);
            Gizmos.color = solidColor;
            Gizmos.DrawCube(Vector3.zero, boxData.Size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;

            DrawConstraintBoundaryDebug(boxData);
        }

        private void DrawConstraintBoundaryDebug(MotionConstraintBoxData boxData)
        {
            Vector3 left = boxData.Rotation * Vector3.left;
            Vector3 right = boxData.Rotation * Vector3.right;
            float halfWidth = boxData.Size.x * 0.5f;

            Color previousColor = Gizmos.color;
            Gizmos.color = _constraintBoxFrontFaceColor;
            Gizmos.DrawLine(
                boxData.FrontFaceCenter + left * halfWidth,
                boxData.FrontFaceCenter + right * halfWidth);
            Gizmos.DrawSphere(boxData.FrontFaceCenter, 0.035f);

            if (boxData.HasDebugBoundary)
            {
                Gizmos.color = _constraintBoxNearestBoundaryColor;
                Gizmos.DrawSphere(boxData.SourceNearestBoundaryPoint, 0.035f);

                Gizmos.color = _constraintBoxFarthestBoundaryColor;
                Gizmos.DrawSphere(boxData.SourceFarthestBoundaryPoint, 0.035f);

                Gizmos.color = _constraintBoxBoundaryPointColor;
                Gizmos.DrawSphere(boxData.SourceFrontBoundaryPoint, 0.045f);
                Gizmos.DrawLine(boxData.SourceFrontBoundaryPoint, boxData.FrontFaceCenter);
            }

            Gizmos.color = previousColor;
        }

        private void LogConstraintBoxSnap(
            MotionWindowRuntimeData runtimeData,
            MotionConstraintBoxData boxData,
            Vector3 currentRootPosition,
            Vector3 snappedRootPosition,
            Vector3 centerOffsetWorld,
            float horizontalRadius)
        {
            if (!_debugConstraintBox || runtimeData?.Clip == null)
            {
                return;
            }

            Vector3 targetCenter = Vector3.zero;
            float targetRadius = -1f;
            if (runtimeData.PrimaryTargetCollider is CharacterController targetController)
            {
                targetCenter = targetController.transform.TransformPoint(targetController.center);
                targetRadius = Mathf.Max(targetController.radius + targetController.skinWidth, 0.01f);
            }
            else if (runtimeData.PrimaryTargetCollider != null)
            {
                targetCenter = runtimeData.PrimaryTargetCollider.bounds.center;
            }
            else if (runtimeData.PrimaryTarget != null)
            {
                targetCenter = runtimeData.PrimaryTarget.position;
            }

            Debug.LogWarning(
                "[ConstraintBoxDebug] SnapToInside\n" +
                $"clipId={runtimeData.Clip.clipId}\n" +
                $"frontSource={runtimeData.Clip.ResolveFrontBoundarySource()}\n" +
                $"currentRoot={currentRootPosition}\n" +
                $"snappedRoot={snappedRootPosition}\n" +
                $"ownerCenter={currentRootPosition + centerOffsetWorld}\n" +
                $"ownerRadius={horizontalRadius}\n" +
                $"referenceForward={runtimeData.ReferenceForward}\n" +
                $"target={runtimeData.PrimaryTarget}\n" +
                $"targetCollider={runtimeData.PrimaryTargetCollider}\n" +
                $"targetCenter={targetCenter}\n" +
                $"targetRadius={targetRadius}\n" +
                $"sourceFrontBoundary={boxData.SourceFrontBoundaryPoint}\n" +
                $"frontFaceCenter={boxData.FrontFaceCenter}\n" +
                $"rearFaceCenter={boxData.RearFaceCenter}\n" +
                $"boxCenter={boxData.Center}\n" +
                $"boxSize={boxData.Size}",
                this);
        }

        private void LogConstraintBoxWindowActive(MotionWindowRuntimeData runtimeData)
        {
            if (!_debugConstraintBox || runtimeData?.Clip == null)
            {
                return;
            }

            if (runtimeData.Clip.constraintBoxMode != MotionConstraintBoxMode.SnapToInside)
            {
                return;
            }

            if (_lastLoggedSnapClipId == runtimeData.Clip.clipId)
            {
                return;
            }

            _lastLoggedSnapClipId = runtimeData.Clip.clipId;
            Debug.LogWarning(
                "[ConstraintBoxDebug] Active Snap Window\n" +
                $"clipId={runtimeData.Clip.clipId}\n" +
                $"frontSource={runtimeData.Clip.ResolveFrontBoundarySource()}\n" +
                $"target={runtimeData.PrimaryTarget}\n" +
                $"targetCollider={runtimeData.PrimaryTargetCollider}\n" +
                $"referenceForward={runtimeData.ReferenceForward}\n" +
                $"hasAppliedEnterSnap={runtimeData.HasAppliedEnterSnap}",
                this);
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
