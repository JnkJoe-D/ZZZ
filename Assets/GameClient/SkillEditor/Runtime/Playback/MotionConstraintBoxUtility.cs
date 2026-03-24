using UnityEngine;

namespace SkillEditor
{
    public struct MotionConstraintBoxData
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector3 Size;
        public MotionConstraintBoxLimitMode LimitMode;
        public bool RecoverWhenOutside;
        public bool HasDebugBoundary;
        public Vector3 SourceFrontBoundaryPoint;
        public Vector3 SourceNearestBoundaryPoint;
        public Vector3 SourceFarthestBoundaryPoint;
        public Vector3 FrontFaceCenter;
        public Vector3 RearFaceCenter;
    }

    public static class MotionConstraintBoxUtility
    {
        public static bool TryGetConstraintBox(MotionWindowRuntimeData runtimeData, Transform owner, out MotionConstraintBoxData boxData)
        {
            boxData = default;
            if (runtimeData == null)
            {
                return false;
            }

            if (runtimeData.HasFrozenConstraintBox)
            {
                boxData = runtimeData.FrozenConstraintBox;
                return true;
            }

            return TryBuildRuntimeConstraintBox(runtimeData, owner, out boxData);
        }

        public static bool TryBuildRuntimeConstraintBox(MotionWindowRuntimeData runtimeData, Transform owner, out MotionConstraintBoxData boxData)
        {
            boxData = default;
            MotionWindowClip clip = runtimeData?.Clip;
            if (clip == null || !clip.UsesConstraintBox() || owner == null)
            {
                return false;
            }

            // 运行时如果前边界依赖目标，但当前没有目标，就不启用约束。
            // 这样无目标时会退回到原本的动画表现，而不是被目标语义的盒子意外限制。
            if (clip.ResolveFrontBoundarySource() != MotionConstraintBoxFrontBoundarySource.LocalConfigured &&
                runtimeData.PrimaryTarget == null)
            {
                return false;
            }

            Vector3 forward = runtimeData.ReferenceForward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = owner.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 size = SanitizeSize(clip.constraintBoxSize);
            Vector3 currentCapsuleCenter = ResolveCapsuleCenter(owner.gameObject, owner.position);

            Vector3 anchorCenter;
            Vector3 sourceNearestBoundary = currentCapsuleCenter + forward * (size.z * 0.5f);
            Vector3 sourceFarthestBoundary = sourceNearestBoundary;
            Vector3 sourceFrontBoundary = sourceNearestBoundary;

            if (clip.ResolveFrontBoundarySource() != MotionConstraintBoxFrontBoundarySource.LocalConfigured &&
                (runtimeData.PrimaryTargetCollider != null || runtimeData.PrimaryTarget != null))
            {
                Vector3 referencePoint = runtimeData.EnterCapsuleCenter.sqrMagnitude > 0.0001f
                    ? runtimeData.EnterCapsuleCenter
                    : owner.position;
                ResolveForwardBoundaryPoints(runtimeData, referencePoint, forward, out sourceNearestBoundary, out sourceFarthestBoundary);
                sourceFrontBoundary = ResolveConfiguredFrontBoundary(
                    clip,
                    sourceNearestBoundary,
                    sourceFarthestBoundary,
                    currentCapsuleCenter,
                    forward,
                    ResolveOwnerRadius(owner.gameObject));
                sourceFrontBoundary += forward * clip.constraintBoxFrontBoundaryOffset;

                if (clip.autoFitConstraintBoxDepthToTarget)
                {
                    float ownerRadius = ResolveOwnerRadius(owner.gameObject);
                    float rearDistance = ownerRadius + Mathf.Max(clip.constraintBoxBackPadding, 0f);
                    float frontDistance = Mathf.Max(0f, Vector3.Dot(sourceFrontBoundary - currentCapsuleCenter, forward));
                    size.z = Mathf.Max(clip.minConstraintBoxDepth, frontDistance + rearDistance);
                }

                anchorCenter = sourceFrontBoundary - forward * (size.z * 0.5f);
            }
            else
            {
                anchorCenter = runtimeData.EnterCapsuleCenter.sqrMagnitude > 0.0001f
                    ? runtimeData.EnterCapsuleCenter
                    : runtimeData.StartPosition;
                Vector3 extentsForward = rotation * Vector3.forward * (size.z * 0.5f);
                sourceFrontBoundary = anchorCenter + extentsForward;
                sourceNearestBoundary = sourceFrontBoundary;
                sourceFarthestBoundary = sourceFrontBoundary;
            }

            boxData = new MotionConstraintBoxData
            {
                Center = anchorCenter + rotation * clip.constraintBoxCenterOffset,
                Rotation = rotation,
                Size = size,
                LimitMode = clip.constraintBoxLimitMode,
                RecoverWhenOutside = clip.recoverWhenOutside,
                HasDebugBoundary = true,
                SourceFrontBoundaryPoint = sourceFrontBoundary,
                SourceNearestBoundaryPoint = sourceNearestBoundary,
                SourceFarthestBoundaryPoint = sourceFarthestBoundary
            };
            FillBoxDebugFaces(ref boxData);
            return true;
        }

        public static MotionConstraintBoxData BuildPreviewConstraintBox(MotionWindowClip clip, Vector3 anchorPosition, Quaternion anchorRotation)
        {
            Quaternion rotation = anchorRotation;
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            Vector3 size = SanitizeSize(clip.constraintBoxSize);
            Vector3 anchorCenter = anchorPosition;
            if (clip.ResolveFrontBoundarySource() != MotionConstraintBoxFrontBoundarySource.LocalConfigured)
            {
                anchorCenter += rotation * (Vector3.forward * (size.z * 0.5f));
            }

            return new MotionConstraintBoxData
            {
                Center = anchorCenter + rotation * clip.constraintBoxCenterOffset,
                Rotation = rotation,
                Size = size,
                LimitMode = clip.constraintBoxLimitMode,
                RecoverWhenOutside = clip.recoverWhenOutside,
                HasDebugBoundary = true,
                SourceFrontBoundaryPoint = anchorCenter + rotation * (Vector3.forward * (size.z * 0.5f)),
                SourceNearestBoundaryPoint = anchorCenter + rotation * (Vector3.forward * (size.z * 0.5f)),
                SourceFarthestBoundaryPoint = anchorCenter + rotation * (Vector3.forward * (size.z * 0.5f))
            };
        }

        public static Vector3 SnapRootPositionToBoxBoundary(
            MotionConstraintBoxData boxData,
            Vector3 currentRootPosition,
            Vector3 candidateRootPosition,
            Vector3 centerOffsetWorld,
            float horizontalRadius)
        {
            return ClampRootPositionToBox(boxData, candidateRootPosition, centerOffsetWorld, horizontalRadius);
        }

        public static Vector3 ClampRootPositionToBox(MotionConstraintBoxData boxData, Vector3 candidateRootPosition, Vector3 centerOffsetWorld, float horizontalRadius)
        {
            Quaternion inverseRotation = Quaternion.Inverse(boxData.Rotation);
            Vector3 candidateCenter = candidateRootPosition + centerOffsetWorld;
            Vector3 localCenter = inverseRotation * (candidateCenter - boxData.Center);
            Vector3 extents = boxData.Size * 0.5f;
            float clampedHalfWidth = Mathf.Max(0f, extents.x - horizontalRadius);
            float clampedHalfDepth = Mathf.Max(0f, extents.z - horizontalRadius);

            switch (boxData.LimitMode)
            {
                case MotionConstraintBoxLimitMode.ForwardOnly:
                    localCenter.z = Mathf.Clamp(localCenter.z, -clampedHalfDepth, clampedHalfDepth);
                    break;
                case MotionConstraintBoxLimitMode.LateralOnly:
                    localCenter.x = Mathf.Clamp(localCenter.x, -clampedHalfWidth, clampedHalfWidth);
                    break;
                default:
                    localCenter.x = Mathf.Clamp(localCenter.x, -clampedHalfWidth, clampedHalfWidth);
                    localCenter.z = Mathf.Clamp(localCenter.z, -clampedHalfDepth, clampedHalfDepth);
                    break;
            }

            Vector3 clampedCenter = boxData.Center + boxData.Rotation * localCenter;
            return clampedCenter - centerOffsetWorld;
        }

        public static bool IsRootPositionInsideBox(MotionConstraintBoxData boxData, Vector3 rootPosition, Vector3 centerOffsetWorld, float horizontalRadius)
        {
            Quaternion inverseRotation = Quaternion.Inverse(boxData.Rotation);
            Vector3 capsuleCenter = rootPosition + centerOffsetWorld;
            Vector3 localCenter = inverseRotation * (capsuleCenter - boxData.Center);
            Vector3 extents = boxData.Size * 0.5f;
            float allowedHalfWidth = Mathf.Max(0f, extents.x - horizontalRadius);
            float allowedHalfDepth = Mathf.Max(0f, extents.z - horizontalRadius);

            return Mathf.Abs(localCenter.x) <= allowedHalfWidth + 0.0001f &&
                   Mathf.Abs(localCenter.z) <= allowedHalfDepth + 0.0001f;
        }

        public static bool TryRestrictRootPositionToBox(
            MotionConstraintBoxData boxData,
            Vector3 currentRootPosition,
            Vector3 candidateRootPosition,
            Vector3 centerOffsetWorld,
            float horizontalRadius,
            out Vector3 restrictedRootPosition)
        {
            restrictedRootPosition = candidateRootPosition;

            Quaternion inverseRotation = Quaternion.Inverse(boxData.Rotation);
            Vector3 currentCenter = currentRootPosition + centerOffsetWorld;
            Vector3 candidateCenter = candidateRootPosition + centerOffsetWorld;
            Vector3 currentLocal = inverseRotation * (currentCenter - boxData.Center);
            Vector3 candidateLocal = inverseRotation * (candidateCenter - boxData.Center);

            Vector3 extents = boxData.Size * 0.5f;
            float allowedHalfWidth = Mathf.Max(0f, extents.x - horizontalRadius);
            float allowedHalfDepth = Mathf.Max(0f, extents.z - horizontalRadius);

            bool currentInside = IsInsideBoxXZ(currentLocal, allowedHalfWidth, allowedHalfDepth);
            bool candidateInside = IsInsideBoxXZ(candidateLocal, allowedHalfWidth, allowedHalfDepth);
            if (currentInside)
            {
                if (candidateInside)
                {
                    return false;
                }
            }
            else if (candidateInside)
            {
                return false;
            }

            if (!TryIntersectSegmentWithBoxXZ(currentLocal, candidateLocal, allowedHalfWidth, allowedHalfDepth, out float enterT, out float exitT))
            {
                if (boxData.RecoverWhenOutside)
                {
                    restrictedRootPosition = ClampRootPositionToBox(boxData, candidateRootPosition, centerOffsetWorld, horizontalRadius);
                    return (restrictedRootPosition - candidateRootPosition).sqrMagnitude > 0.0000001f;
                }
                return false;
            }

            float restrictT = currentInside ? exitT : exitT;
            Vector3 hitLocal = Vector3.Lerp(currentLocal, candidateLocal, Mathf.Clamp01(restrictT));
            Vector3 hitCenter = boxData.Center + boxData.Rotation * hitLocal;
            restrictedRootPosition = hitCenter - centerOffsetWorld;
            return true;
        }

        public static Vector3 ResolveCapsuleCenter(GameObject owner, Vector3 fallbackPosition)
        {
            if (owner == null)
            {
                return fallbackPosition;
            }

            CharacterController controller = owner.GetComponent<CharacterController>();
            if (controller == null)
            {
                return fallbackPosition;
            }

            return owner.transform.TransformPoint(controller.center);
        }

        private static Vector3 SanitizeSize(Vector3 size)
        {
            size.x = Mathf.Max(0.01f, Mathf.Abs(size.x));
            size.y = Mathf.Max(0.01f, Mathf.Abs(size.y));
            size.z = Mathf.Max(0.01f, Mathf.Abs(size.z));
            return size;
        }

        private static bool IsInsideBoxXZ(Vector3 localPoint, float halfWidth, float halfDepth)
        {
            return Mathf.Abs(localPoint.x) <= halfWidth + 0.0001f &&
                   Mathf.Abs(localPoint.z) <= halfDepth + 0.0001f;
        }

        private static bool TryIntersectSegmentWithBoxXZ(Vector3 startLocal, Vector3 endLocal, float halfWidth, float halfDepth, out float enterT, out float exitT)
        {
            enterT = 0f;
            exitT = 1f;
            Vector3 delta = endLocal - startLocal;

            if (!ClipAxis(startLocal.x, delta.x, -halfWidth, halfWidth, ref enterT, ref exitT))
            {
                return false;
            }

            if (!ClipAxis(startLocal.z, delta.z, -halfDepth, halfDepth, ref enterT, ref exitT))
            {
                return false;
            }

            return enterT <= exitT && enterT >= 0f && enterT <= 1f;
        }

        private static bool ClipAxis(float start, float delta, float min, float max, ref float enterT, ref float exitT)
        {
            if (Mathf.Abs(delta) <= 0.000001f)
            {
                return start >= min && start <= max;
            }

            float t1 = (min - start) / delta;
            float t2 = (max - start) / delta;
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }

            enterT = Mathf.Max(enterT, t1);
            exitT = Mathf.Min(exitT, t2);
            return enterT <= exitT;
        }

        private static Vector3 ResolveForwardBoundaryPoint(MotionWindowRuntimeData runtimeData, Vector3 referencePoint, Vector3 forward)
        {
            ResolveForwardBoundaryPoints(runtimeData, referencePoint, forward, out Vector3 nearest, out _);
            return nearest;
        }

        private static void ResolveForwardBoundaryPoints(
            MotionWindowRuntimeData runtimeData,
            Vector3 referencePoint,
            Vector3 forward,
            out Vector3 nearestBoundary,
            out Vector3 farthestBoundary)
        {
            nearestBoundary = referencePoint;
            farthestBoundary = referencePoint;
            Collider targetCollider = runtimeData.PrimaryTargetCollider;
            if (targetCollider == null)
            {
                return;
            }

            if (targetCollider is CharacterController controller)
            {
                Vector3 center = controller.transform.TransformPoint(controller.center);
                float radius = Mathf.Max(controller.radius + controller.skinWidth, 0.01f);
                nearestBoundary = center - forward * radius;
                farthestBoundary = center + forward * radius;
                return;
            }

            Bounds bounds = targetCollider.bounds;
            float distance = bounds.extents.magnitude * 4f + 10f;
            Vector3 origin = referencePoint - forward * distance;
            Ray ray = new Ray(origin, forward);
            if (targetCollider.Raycast(ray, out RaycastHit hit, distance * 2f))
            {
                nearestBoundary = hit.point;
            }
            else
            {
                nearestBoundary = targetCollider.ClosestPoint(referencePoint);
            }

            farthestBoundary = bounds.center + forward.normalized * bounds.extents.magnitude;
        }

        private static Vector3 ResolveConfiguredFrontBoundary(
            MotionWindowClip clip,
            Vector3 nearestBoundary,
            Vector3 farthestBoundary,
            Vector3 ownerCenter,
            Vector3 forward,
            float ownerRadius)
        {
            switch (clip.ResolveFrontBoundarySource())
            {
                case MotionConstraintBoxFrontBoundarySource.TargetFarthestSurface:
                    return farthestBoundary;
                case MotionConstraintBoxFrontBoundarySource.TargetBackPlusOwnerDiameter:
                    return farthestBoundary + forward * (ownerRadius * 2f);
                case MotionConstraintBoxFrontBoundarySource.LocalConfigured:
                    return ownerCenter + forward * Mathf.Max(clip.constraintBoxSize.z * 0.5f, ownerRadius);
                default:
                    return nearestBoundary;
            }
        }

        private static float ResolveOwnerRadius(GameObject owner)
        {
            if (owner == null)
            {
                return 0.3f;
            }

            CharacterController controller = owner.GetComponent<CharacterController>();
            if (controller == null)
            {
                return 0.3f;
            }

            return Mathf.Max(controller.radius + controller.skinWidth, 0.01f);
        }

        private static void FillBoxDebugFaces(ref MotionConstraintBoxData boxData)
        {
            Vector3 forward = boxData.Rotation * Vector3.forward;
            float halfDepth = boxData.Size.z * 0.5f;
            boxData.FrontFaceCenter = boxData.Center + forward * halfDepth;
            boxData.RearFaceCenter = boxData.Center - forward * halfDepth;
        }

        private static Collider FindNearestCollider(Collider[] colliders, Vector3 referencePoint)
        {
            Collider bestCollider = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                Vector3 closestPoint = collider.ClosestPoint(referencePoint);
                float sqrDistance = (closestPoint - referencePoint).sqrMagnitude;
                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    bestCollider = collider;
                }
            }

            return bestCollider;
        }
    }
}
