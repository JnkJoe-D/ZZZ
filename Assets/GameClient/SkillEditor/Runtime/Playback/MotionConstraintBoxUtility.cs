using UnityEngine;

namespace SkillEditor
{
    public struct MotionConstraintBoxData
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector3 Size;
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
            if (clip == null || !clip.enableConstraintBox || owner == null)
            {
                return false;
            }

            // 运行时如果约束盒依赖目标前边界，但当前没有目标，就不启用约束。
            // 这样无目标时会退回到原本的动画表现，而不是被本地盒意外限制。
            if (clip.alignConstraintBoxFrontToTarget && runtimeData.PrimaryTarget == null)
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
            if (clip.alignConstraintBoxFrontToTarget && (runtimeData.PrimaryTargetCollider != null || runtimeData.PrimaryTarget != null))
            {
                Vector3 referencePoint = runtimeData.EnterCapsuleCenter.sqrMagnitude > 0.0001f
                    ? runtimeData.EnterCapsuleCenter
                    : owner.position;
                Vector3 frontBoundary = ResolveForwardBoundaryPoint(runtimeData, referencePoint, forward);
                if (clip.autoFitConstraintBoxDepthToTarget)
                {
                    float frontDistance = Mathf.Max(0f, Vector3.Dot(frontBoundary - currentCapsuleCenter, forward));
                    size.z = Mathf.Max(0.01f, frontDistance + Mathf.Max(clip.constraintBoxBackPadding, 0f));
                }

                anchorCenter = frontBoundary - forward * (size.z * 0.5f);
            }
            else
            {
                anchorCenter = runtimeData.EnterCapsuleCenter.sqrMagnitude > 0.0001f
                    ? runtimeData.EnterCapsuleCenter
                    : runtimeData.StartPosition;
            }

            boxData = new MotionConstraintBoxData
            {
                Center = anchorCenter + rotation * clip.constraintBoxCenterOffset,
                Rotation = rotation,
                Size = size
            };
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
            if (clip.alignConstraintBoxFrontToTarget)
            {
                anchorCenter += rotation * (Vector3.forward * (size.z * 0.5f));
            }

            return new MotionConstraintBoxData
            {
                Center = anchorCenter + rotation * clip.constraintBoxCenterOffset,
                Rotation = rotation,
                Size = size
            };
        }

        public static Vector3 ClampRootPositionToBox(MotionConstraintBoxData boxData, Vector3 candidateRootPosition, Vector3 centerOffsetWorld, float horizontalRadius)
        {
            Quaternion inverseRotation = Quaternion.Inverse(boxData.Rotation);
            Vector3 candidateCenter = candidateRootPosition + centerOffsetWorld;
            Vector3 localCenter = inverseRotation * (candidateCenter - boxData.Center);
            Vector3 extents = boxData.Size * 0.5f;
            float clampedHalfWidth = Mathf.Max(0f, extents.x - horizontalRadius);
            float clampedHalfDepth = Mathf.Max(0f, extents.z - horizontalRadius);

            localCenter.x = Mathf.Clamp(localCenter.x, -clampedHalfWidth, clampedHalfWidth);
            localCenter.z = Mathf.Clamp(localCenter.z, -clampedHalfDepth, clampedHalfDepth);

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
            Collider targetCollider = runtimeData.PrimaryTargetCollider;
            Transform targetRoot = runtimeData.PrimaryTarget != null ? runtimeData.PrimaryTarget.root : null;
            if (targetRoot != null)
            {
                Collider[] colliders = targetRoot.GetComponentsInChildren<Collider>();
                Collider bestCollider = null;
                RaycastHit bestHit = default;
                bool hasHit = false;

                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (collider == null || !collider.enabled || collider.isTrigger)
                    {
                        continue;
                    }

                    Bounds colliderBounds = collider.bounds;
                    float collidercastDistance = colliderBounds.extents.magnitude * 4f + 10f;
                    Vector3 colliderOrigin = referencePoint - forward * collidercastDistance;
                    Ray colliderRay = new Ray(colliderOrigin, forward);
                    if (collider.Raycast(colliderRay, out RaycastHit colliderHit, collidercastDistance * 2f))
                    {
                        if (!hasHit || colliderHit.distance < bestHit.distance)
                        {
                            hasHit = true;
                            bestHit = colliderHit;
                            bestCollider = collider;
                        }
                    }
                }

                if (hasHit)
                {
                    runtimeData.PrimaryTargetCollider = bestCollider;
                    return bestHit.point;
                }

                targetCollider = FindNearestCollider(colliders, referencePoint);
            }

            if (targetCollider == null)
            {
                return referencePoint;
            }

            Bounds targetBounds = targetCollider.bounds;
            float targetCastDistance = targetBounds.extents.magnitude * 4f + 10f;
            Vector3 origin = referencePoint - forward * targetCastDistance;
            Ray targetRay = new Ray(origin, forward);
            if (targetCollider.Raycast(targetRay, out RaycastHit targethit, targetCastDistance * 2f))
            {
                return targethit.point;
            }

            return targetCollider.ClosestPoint(referencePoint);
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
