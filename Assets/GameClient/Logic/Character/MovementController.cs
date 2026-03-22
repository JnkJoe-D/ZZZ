using System.Collections.Generic;
using Game.AI;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        [Header("阻挡检测")]
        [SerializeField] private float _blockCheckRadius = 0.3f;
        [SerializeField] private float _blockCheckHeight = 1.8f;
        [SerializeField] private LayerMask _blockCheckLayers;
        [SerializeField] private Color _gizmoColor = new Color(1f, 0f, 0f, 0.35f);
        [SerializeField] private Color _constraintBoxGizmoColor = new Color(0.2f, 0.8f, 1f, 0.2f);

        [Header("根运动")]
        [SerializeField] private float _verticalRootMotionThreshold = 0.0001f;

        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private ISkillMotionWindowHandler _motionWindowHandler;
        private float _verticalVelocity;

        [Header("基础移动")]
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

            if (_blockCheckLayers.value == 0)
            {
                _blockCheckLayers = LayerMask.GetMask("Default", "Ground", "CharHit", "Charcter", "Character");
            }
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

            /* 如果动画自己提供了明显的 Y 位移，就优先信任动画；否则继续叠加重力。 */ bool hasVerticalRootMotion = Mathf.Abs(deltaPosition.y) > _verticalRootMotionThreshold;
            if (_cc != null && _cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else if (!hasVerticalRootMotion)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
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
            Vector3 verticalDelta = Vector3.up * deltaPosition.y;

            if (TryApplyMotionWindow(horizontalDelta, verticalDelta))
            {
                return;
            }

            /* 没有窗口生效时，按默认 root motion 直接落地。 */ if (_cc != null && _cc.enabled)
            {
                _cc.Move(horizontalDelta + verticalDelta);
            }
            else
            {
                transform.position += horizontalDelta + verticalDelta;
            }
        }

        private bool TryApplyMotionWindow(Vector3 horizontalDelta, Vector3 verticalDelta)
        {
            /* 只有动作播放期间才允许窗口接管根运动，避免异常残留状态影响 locomotion。 */ if (_entity?.ActionPlayer != null && !_entity.ActionPlayer.IsPlaying)
            {
                return false;
            }

            if (_motionWindowHandler == null ||
                !_motionWindowHandler.TryGetActiveWindow(out MotionWindowRuntimeData runtimeData) ||
                runtimeData?.Clip == null)
            {
                return false;
            }

            Vector3 filteredHorizontalDelta = ApplyMotionWindowConstraintBox(runtimeData, horizontalDelta);
            if (_cc != null && _cc.enabled)
            {
                Vector3 currentRootPosition = transform.position;
                Vector3 resolvedRootPosition = currentRootPosition + filteredHorizontalDelta;
                {
                    /* MotionWindow 生效时，水平位移以我们自己的解算结果为准，避免 CharacterController 把角色沿碰撞切线挤偏。 */
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
            }
            else
            {
                transform.position += filteredHorizontalDelta + verticalDelta;
            }

            return true;
        }

        private Vector3 BuildMotionWindowHorizontalDelta(MotionWindowRuntimeData runtimeData, Vector3 horizontalDelta)
        {
            MotionWindowClip clip = runtimeData.Clip;
            if (clip == null)
            {
                return horizontalDelta;
            }

            if (clip.trajectoryMode == MotionTrajectoryMode.Authored)
            {
                /* 完整保留作者轨迹，只在策略要求时再做碰撞过滤。 */ Vector3 authoredDelta = ApplyMotionWindowCharacterCollision(runtimeData, horizontalDelta);
                return ApplyMotionWindowWorldCollision(runtimeData, authoredDelta);
            }

            Vector3 referenceForward = ResolveMotionWindowForward(runtimeData);
            if (referenceForward.sqrMagnitude <= 0.0001f)
            {
                return horizontalDelta;
            }

            referenceForward.Normalize();
            Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
            if (referenceRight.sqrMagnitude <= 0.0001f)
            {
                referenceRight = transform.right;
                referenceRight.y = 0f;
            }

            if (referenceRight.sqrMagnitude > 0.0001f)
            {
                referenceRight.Normalize();
            }

            /* 把动画位移拆成“前向推进”和“横向表演”两部分。 */ float authoredForwardDistance = Vector3.Dot(horizontalDelta, referenceForward) * clip.forwardScale;
            float authoredLateralDistance = 0f;
            if (clip.trajectoryMode == MotionTrajectoryMode.ForwardKeepLateral)
            {
                authoredLateralDistance = Vector3.Dot(horizontalDelta, referenceRight) * clip.lateralScale;
            }

            /* 这类技能只收前向距离，横向保留交给策略决定。 */ float filteredForwardDistance = ClampMotionWindowForwardDistance(runtimeData, referenceForward, authoredForwardDistance);
            Vector3 candidateDelta = referenceForward * filteredForwardDistance + referenceRight * authoredLateralDistance;
            return ApplyMotionWindowWorldCollision(runtimeData, candidateDelta);
        }

        private Vector3 ResolveMotionWindowForward(MotionWindowRuntimeData runtimeData)
        {
            if (runtimeData != null)
            {
                Vector3 forward = runtimeData.ReferenceForward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    return forward.normalized;
                }
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private float ClampMotionWindowForwardDistance(MotionWindowRuntimeData runtimeData, Vector3 referenceForward, float desiredDistance)
        {
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null || clip.characterCollisionMode == MotionCharacterCollisionMode.IgnoreAllCharacters)
            {
                return desiredDistance;
            }

            float moveDistance = Mathf.Abs(desiredDistance);
            if (moveDistance <= 0.0001f)
            {
                return desiredDistance;
            }

            LayerMask layers = ResolveMotionWindowLayerMask(clip.characterBlockLayers, true);
            if (layers.value == 0)
            {
                return desiredDistance;
            }

            Transform ignoredRoot = null;
            if (clip.characterCollisionMode == MotionCharacterCollisionMode.IgnorePrimaryTarget)
            {
                ignoredRoot = runtimeData.PrimaryTarget != null ? runtimeData.PrimaryTarget.root : null;
            }

            Vector3 moveDirection = desiredDistance >= 0f ? referenceForward : -referenceForward;
            if (!TryFindBlockingHit(moveDirection, moveDistance, layers, ignoredRoot, out RaycastHit hit))
            {
                return desiredDistance;
            }

            float stopOffset = clip.characterCollisionMode == MotionCharacterCollisionMode.BlockAll
                ? Mathf.Max(clip.stopDistance, 0f)
                : 0f;
            float allowedDistance = Mathf.Max(0f, hit.distance - Mathf.Max(GetSkinWidth(), 0.01f) - stopOffset);
            return Mathf.Sign(desiredDistance) * Mathf.Min(moveDistance, allowedDistance);
        }

        private Vector3 ApplyMotionWindowCharacterCollision(MotionWindowRuntimeData runtimeData, Vector3 candidateDelta)
        {
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null || clip.characterCollisionMode == MotionCharacterCollisionMode.IgnoreAllCharacters)
            {
                return candidateDelta;
            }

            float moveDistance = candidateDelta.magnitude;
            if (moveDistance <= 0.0001f)
            {
                return candidateDelta;
            }

            LayerMask layers = ResolveMotionWindowLayerMask(clip.characterBlockLayers, true);
            if (layers.value == 0)
            {
                return candidateDelta;
            }

            Transform ignoredRoot = null;
            if (clip.characterCollisionMode == MotionCharacterCollisionMode.IgnorePrimaryTarget)
            {
                ignoredRoot = runtimeData.PrimaryTarget != null ? runtimeData.PrimaryTarget.root : null;
            }

            Vector3 moveDirection = candidateDelta / moveDistance;
            if (!TryFindBlockingHit(moveDirection, moveDistance, layers, ignoredRoot, out RaycastHit hit))
            {
                return candidateDelta;
            }

            float stopOffset = clip.characterCollisionMode == MotionCharacterCollisionMode.BlockAll
                ? Mathf.Max(clip.stopDistance, 0f)
                : 0f;
            float allowedDistance = Mathf.Max(0f, hit.distance - Mathf.Max(GetSkinWidth(), 0.01f) - stopOffset);
            return moveDirection * Mathf.Min(moveDistance, allowedDistance);
        }

        private Vector3 ApplyMotionWindowWorldCollision(MotionWindowRuntimeData runtimeData, Vector3 candidateDelta)
        {
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null || clip.worldCollisionMode == MotionWorldCollisionMode.Ignore)
            {
                return candidateDelta;
            }

            float moveDistance = candidateDelta.magnitude;
            if (moveDistance <= 0.0001f)
            {
                return candidateDelta;
            }

            LayerMask layers = ResolveMotionWindowLayerMask(clip.worldBlockLayers, false);
            if (layers.value == 0)
            {
                return candidateDelta;
            }

            Vector3 moveDirection = candidateDelta / moveDistance;
            if (!TryFindBlockingHit(moveDirection, moveDistance, layers, null, out RaycastHit hit))
            {
                return candidateDelta;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - Mathf.Max(GetSkinWidth(), 0.01f));
            float traveledDistance = Mathf.Min(moveDistance, allowedDistance);
            Vector3 traveledDelta = moveDirection * traveledDistance;
            if (clip.worldCollisionMode == MotionWorldCollisionMode.Block || traveledDistance >= moveDistance - 0.0001f)
            {
                return traveledDelta;
            }

            /* Slide 模式先走到撞击点，再把剩余位移投影到障碍切线上。 */ Vector3 remainingDelta = candidateDelta - traveledDelta;
            Vector3 slideDelta = Vector3.ProjectOnPlane(remainingDelta, hit.normal);
            slideDelta.y = 0f;
            float slideDistance = slideDelta.magnitude;
            if (slideDistance <= 0.0001f)
            {
                return traveledDelta;
            }

            Vector3 slideDirection = slideDelta / slideDistance;
            if (TryFindBlockingHit(slideDirection, slideDistance, layers, null, out RaycastHit slideHit))
            {
                float slideAllowedDistance = Mathf.Max(0f, slideHit.distance - Mathf.Max(GetSkinWidth(), 0.01f));
                slideDelta = slideDirection * Mathf.Min(slideDistance, slideAllowedDistance);
            }

            return traveledDelta + slideDelta;
        }

        private Vector3 ApplyMotionWindowConstraintBox(MotionWindowRuntimeData runtimeData, Vector3 candidateDelta)
        {
            if (!MotionConstraintBoxUtility.TryGetConstraintBox(runtimeData, transform, out MotionConstraintBoxData boxData))
            {
                return candidateDelta;
            }

            MotionWindowClip clip = runtimeData?.Clip;
            Vector3 currentRootPosition = transform.position;
            Vector3 candidateRootPosition = currentRootPosition + candidateDelta;
            Vector3 centerOffsetWorld = GetCapsuleCenterOffsetWorld();
            float horizontalRadius = GetCapsuleRadius();
            if (clip != null && clip.constraintBoxMode == MotionConstraintBoxMode.Block)
            {
                if (MotionConstraintBoxUtility.TryRestrictRootPositionToBox(
                    boxData,
                    currentRootPosition,
                    candidateRootPosition,
                    centerOffsetWorld,
                    horizontalRadius,
                    out Vector3 restrictedRootPosition))
                {
                    Vector3 restrictedDelta = restrictedRootPosition - currentRootPosition;
                    restrictedDelta.y = 0f;
                    return restrictedDelta;
                }

                if (!MotionConstraintBoxUtility.IsRootPositionInsideBox(
                        boxData,
                        currentRootPosition,
                        centerOffsetWorld,
                        horizontalRadius))
                {
                    return candidateDelta;
                }
            }

            Vector3 clampedRootPosition = MotionConstraintBoxUtility.ClampRootPositionToBox(
                boxData,
                candidateRootPosition,
                centerOffsetWorld,
                horizontalRadius);

            Vector3 clampedDelta = clampedRootPosition - currentRootPosition;
            clampedDelta.y = 0f;
            return clampedDelta;
        }

        private List<Collider> BeginMotionWindowCollisionBypass(MotionWindowRuntimeData runtimeData, Vector3 horizontalDelta)
        {
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null || _cc == null || !_cc.enabled)
            {
                return null;
            }

            if (clip.characterCollisionMode == MotionCharacterCollisionMode.BlockAll)
            {
                return null;
            }

            HashSet<Collider> collidersToIgnore = new HashSet<Collider>();
            if (clip.characterCollisionMode == MotionCharacterCollisionMode.IgnorePrimaryTarget)
            {
                CollectTargetRootColliders(runtimeData.PrimaryTarget, collidersToIgnore);
            }
            else if (clip.characterCollisionMode == MotionCharacterCollisionMode.IgnoreAllCharacters)
            {
                LayerMask layers = ResolveMotionWindowLayerMask(clip.characterBlockLayers, true);
                CollectCharacterCollidersAlongDelta(horizontalDelta, layers, collidersToIgnore);
            }

            if (collidersToIgnore.Count == 0)
            {
                return null;
            }

            List<Collider> ignoredColliders = new List<Collider>(collidersToIgnore.Count);
            foreach (Collider collider in collidersToIgnore)
            {
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (collider.transform.root == transform.root)
                {
                    continue;
                }

                Physics.IgnoreCollision(_cc, collider, true);
                ignoredColliders.Add(collider);
            }

            return ignoredColliders;
        }

        private void EndMotionWindowCollisionBypass(List<Collider> ignoredColliders)
        {
            if (_cc == null || ignoredColliders == null)
            {
                return;
            }

            for (int i = 0; i < ignoredColliders.Count; i++)
            {
                Collider collider = ignoredColliders[i];
                if (collider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(_cc, collider, false);
            }
        }

        private void CollectTargetRootColliders(Transform target, HashSet<Collider> results)
        {
            if (target == null || results == null)
            {
                return;
            }

            Transform root = target.root != null ? target.root : target;
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                results.Add(collider);
            }
        }

        private void CollectCharacterCollidersAlongDelta(Vector3 horizontalDelta, LayerMask layers, HashSet<Collider> results)
        {
            if (results == null || layers.value == 0 || _cc == null)
            {
                return;
            }

            GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom);
            float radius = Mathf.Max(_blockCheckRadius, 0.01f);
            Collider[] overlaps = Physics.OverlapCapsule(top, bottom, radius, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider collider = overlaps[i];
                if (collider == null || collider.transform.root == transform.root)
                {
                    continue;
                }

                results.Add(collider);
            }

            float moveDistance = horizontalDelta.magnitude;
            if (moveDistance <= 0.0001f)
            {
                return;
            }

            Vector3 moveDirection = horizontalDelta / moveDistance;
            float castDistance = moveDistance + Mathf.Max(GetSkinWidth(), 0.01f);
            RaycastHit[] hits = Physics.CapsuleCastAll(
                top,
                bottom,
                radius,
                moveDirection,
                castDistance,
                layers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.transform.root == transform.root)
                {
                    continue;
                }

                results.Add(collider);
            }
        }

        private LayerMask ResolveMotionWindowLayerMask(LayerMask clipLayers, bool isCharacterMask)
        {
            if (clipLayers.value != 0)
            {
                return clipLayers;
            }

            return isCharacterMask
                ? LayerMask.GetMask("CharHit", "Charcter", "Character")
                : LayerMask.GetMask("Default", "Ground");
        }

        public void ApplyMotionWindowEndPlacement(MotionWindowRuntimeData runtimeData)
        {
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null)
            {
                return;
            }

            if (clip.enableConstraintBox)
            {
                return;
            }

            bool shouldSnapToTarget =
                clip.endPlacementMode == MotionEndPlacementMode.SnapFrontOfTarget ||
                clip.endPlacementMode == MotionEndPlacementMode.SnapBehindTarget;
            if (!clip.projectEndPositionToReferenceLine && !shouldSnapToTarget)
            {
                return;
            }

            Vector3 referenceForward = ResolveMotionWindowForward(runtimeData);
            Vector3 currentHorizontalPosition = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 desiredHorizontalPosition = currentHorizontalPosition;
            if (shouldSnapToTarget)
            {
                if (runtimeData.PrimaryTarget == null)
                {
                    return;
                }

                Vector3 targetPosition = Vector3.ProjectOnPlane(runtimeData.PrimaryTarget.position, Vector3.up);
                float offset = clip.endPlacementMode == MotionEndPlacementMode.SnapBehindTarget
                    ? Mathf.Max(clip.passThroughOffset, 0f)
                    : -Mathf.Max(clip.stopDistance, 0f);
                desiredHorizontalPosition = targetPosition + referenceForward * offset;
            }

            if (clip.projectEndPositionToReferenceLine)
            {
                Vector3 lineOrigin = Vector3.ProjectOnPlane(runtimeData.StartPosition, Vector3.up);
                float projectedDistance = Mathf.Max(0f, Vector3.Dot(desiredHorizontalPosition - lineOrigin, referenceForward));
                if (runtimeData.PrimaryTarget != null)
                {
                    float targetDistance = Mathf.Max(0f, Vector3.Dot(
                        Vector3.ProjectOnPlane(runtimeData.PrimaryTarget.position, Vector3.up) - lineOrigin,
                        referenceForward));
                    projectedDistance = Mathf.Min(projectedDistance, targetDistance);
                }

                desiredHorizontalPosition = lineOrigin + referenceForward * projectedDistance;
            }

            Vector3 desiredDelta = desiredHorizontalPosition - currentHorizontalPosition;
            Vector3 filteredDelta = ApplyMotionWindowWorldCollision(runtimeData, desiredDelta);
            Vector3 finalHorizontalPosition = currentHorizontalPosition + filteredDelta;
            if (clip.projectEndPositionToReferenceLine)
            {
                Vector3 lineOrigin = Vector3.ProjectOnPlane(runtimeData.StartPosition, Vector3.up);
                float projectedDistance = Mathf.Max(0f, Vector3.Dot(finalHorizontalPosition - lineOrigin, referenceForward));
                if (runtimeData.PrimaryTarget != null)
                {
                    float targetDistance = Mathf.Max(0f, Vector3.Dot(
                        Vector3.ProjectOnPlane(runtimeData.PrimaryTarget.position, Vector3.up) - lineOrigin,
                        referenceForward));
                    projectedDistance = Mathf.Min(projectedDistance, targetDistance);
                }

                finalHorizontalPosition = lineOrigin + referenceForward * projectedDistance;
            }

            if (clip.enableConstraintBox &&
                MotionConstraintBoxUtility.TryBuildRuntimeConstraintBox(runtimeData, transform, out MotionConstraintBoxData boxData))
            {
                Vector3 rootPosition = new Vector3(finalHorizontalPosition.x, transform.position.y, finalHorizontalPosition.z);
                Vector3 clampedRootPosition = MotionConstraintBoxUtility.ClampRootPositionToBox(
                    boxData,
                    rootPosition,
                    GetCapsuleCenterOffsetWorld(),
                    GetCapsuleRadius());
                finalHorizontalPosition = Vector3.ProjectOnPlane(clampedRootPosition, Vector3.up);
            }

            transform.position = new Vector3(finalHorizontalPosition.x, transform.position.y, finalHorizontalPosition.z);
        }

        private void ApplyRootRotation(Quaternion deltaRotation)
        {
            if (deltaRotation == Quaternion.identity)
            {
                return;
            }

            transform.rotation *= deltaRotation;
        }

        private bool TryFindBlockingHit(Vector3 moveDirection, float moveDistance, LayerMask layers, Transform ignoredRoot, out RaycastHit bestHit)
        {
            bestHit = default;
            if (_cc == null || moveDistance <= 0f || layers.value == 0)
            {
                return false;
            }

            GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom);
            float castDistance = moveDistance + Mathf.Max(GetSkinWidth(), 0.01f);
            RaycastHit[] hits = Physics.CapsuleCastAll(
                top,
                bottom,
                Mathf.Max(_blockCheckRadius, 0.01f),
                moveDirection,
                castDistance,
                layers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.transform.root == transform.root)
                {
                    continue;
                }

                if (ignoredRoot != null && hit.collider.transform.root == ignoredRoot)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                }
            }

            return bestDistance < float.MaxValue;
        }

        private void GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom)
        {
            const float liftOffset = 0.1f;
            Vector3 center = transform.TransformPoint(_cc != null ? _cc.center : new Vector3(0f, _blockCheckHeight * 0.5f, 0f));
            float halfHeight = Mathf.Max(_blockCheckHeight * 0.5f - _blockCheckRadius, 0f);

            top = center + Vector3.up * halfHeight;
            bottom = center - Vector3.up * (halfHeight - liftOffset);
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
            return _cc != null ? Mathf.Max(_cc.radius, 0.01f) : 0.3f;
        }

        private float GetSkinWidth()
        {
            return _cc != null ? _cc.skinWidth : 0.01f;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_cc != null)
            {
                Gizmos.color = _gizmoColor;
                GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom);
                DrawCapsule(top, bottom, _blockCheckRadius);
            }

            if (_motionWindowHandler != null &&
                _motionWindowHandler.TryGetActiveWindow(out MotionWindowRuntimeData runtimeData) &&
                runtimeData?.Clip != null &&
                MotionConstraintBoxUtility.TryGetConstraintBox(runtimeData, transform, out MotionConstraintBoxData boxData))
            {
                DrawConstraintBox(boxData, runtimeData.Clip.debugColor.a > 0f ? runtimeData.Clip.debugColor : _constraintBoxGizmoColor);
            }
        }

        private void DrawCapsule(Vector3 top, Vector3 bottom, float radius)
        {
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawLine(top + Vector3.left * radius, bottom + Vector3.left * radius);
            Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
            Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
            Gizmos.DrawLine(top + Vector3.back * radius, bottom + Vector3.back * radius);
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
