using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 移动片段智能位置解算器与障碍物/地形可用性校验器
    /// </summary>
    public static class MovementPositionSolver
    {
        private const float DefaultCharacterRadius = 0.5f;
        private const float DefaultCharacterHeight = 2.0f;

        /// <summary>
        /// 解算并校验最佳目标移动位置
        /// </summary>
        public static Vector3 ResolveTargetPosition(
            MovementClip clip,
            ITransformHandler transformHandler,
            Transform ownerTransform,
            Vector3 startPos,
            Vector3 fallbackDir,
            out bool isFound)
        {
            isFound = false;
            if (clip == null) return startPos;

            // 1. 固定位置参考（非目标参考）
            if (clip.referenceDestination == ReferenceDestination.Fixed)
            {
                Vector3 rawPos;
                if (clip.referenceCoordinate == CoordinateSystem.Local)
                {
                    rawPos = ownerTransform != null
                        ? ownerTransform.TransformPoint(clip.targetPosition)
                        : startPos + clip.targetPosition;
                }
                else
                {
                    rawPos = clip.targetPosition;
                }

                if (clip.enablePositionValidation)
                {
                    float selfRadius = transformHandler != null ? transformHandler.GetRadius() : DefaultCharacterRadius;
                    float selfHeight = transformHandler != null ? transformHandler.GetHeight() : DefaultCharacterHeight;
                    Vector3 selfCenter = transformHandler != null ? transformHandler.GetCenter() : new Vector3(0f, 1f, 0f);
                    Collider selfCol = transformHandler != null ? transformHandler.GetCollider() : null;

                    if (ValidatePosition(rawPos, 0f, selfRadius, selfHeight, selfCenter,
                            clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                            clip.requireGrounded, selfCol, null, null, out Vector3 validPos))
                    {
                        isFound = true;
                        return validPos;
                    }
                }

                isFound = true;
                return rawPos;
            }

            // 2. 有目标参考
            Transform target = transformHandler != null ? transformHandler.GetTarget() : null;
            if (target == null)
            {
                // 无目标时降级回退：按角色朝向或输入方向偏移
                Vector3 moveDir = fallbackDir != Vector3.zero ? fallbackDir : (ownerTransform != null ? ownerTransform.forward : Vector3.forward);
                return startPos + moveDir * (clip.offsetRadius > 0 ? clip.offsetRadius : 2.0f);
            }

            float rTarget = transformHandler != null ? transformHandler.GetTargetRadius() : DefaultCharacterRadius;
            float rSelf = transformHandler != null ? transformHandler.GetRadius() : DefaultCharacterRadius;
            float hSelf = transformHandler != null ? transformHandler.GetHeight() : DefaultCharacterHeight;
            Vector3 cSelf = transformHandler != null ? transformHandler.GetCenter() : new Vector3(0f, 1f, 0f);
            Collider selfCollider = transformHandler != null ? transformHandler.GetCollider() : null;
            Collider targetCollider = transformHandler != null ? transformHandler.GetTargetCollider() : null;
            Vector3 inputDir = transformHandler != null ? transformHandler.GetInputDirection(true) : Vector3.zero;

            Vector3 targetPos = target.position;

            float targetOffset = GetAnchorOffset(clip.targetAnchor, rTarget);
            float selfOffset = GetAnchorOffset(clip.selfAnchor, rSelf);
            float minSafeDist = rTarget + rSelf + 0.001f;

            // 如果未开启位置校验，直接按配置计算
            if (!clip.enablePositionValidation)
            {
                if (clip.targetPositionEnum == TargetPositionType.CandidateList)
                {
                    if (clip.candidatePositions != null && clip.candidatePositions.Length > 0 && clip.candidatePositions[0] != null)
                    {
                        var first = clip.candidatePositions[0];
                        Vector3 dir = CalculateOffsetDirection(first.targetPositionEnum, first.targetBaseDirection, first.angleOffset, target, startPos, inputDir);
                        
                        float candTargetOffset = GetAnchorOffset(first.targetAnchor, rTarget);
                        float candSelfOffset = GetAnchorOffset(first.selfAnchor, rSelf);
                        float desiredDist = candTargetOffset + candSelfOffset + first.offsetRadius;
                        float finalDist = Mathf.Max(desiredDist, minSafeDist);

                        isFound = true;
                        return targetPos + dir * finalDist + Vector3.up * first.heightOffset;
                    }
                }

                Vector3 defaultDir = CalculateOffsetDirection(clip.targetPositionEnum, clip.targetBaseDirection, clip.angleOffset, target, startPos, inputDir);
                float defaultDesiredDist = targetOffset + selfOffset + clip.offsetRadius;
                float defaultFinalDist = Mathf.Max(defaultDesiredDist, minSafeDist);

                isFound = true;
                return targetPos + defaultDir * defaultFinalDist;
            }

            // --- 智能多阶段校验流水线 ---

            // 模式 A: 多级候选目标位置列表 (CandidateList)
            if (clip.targetPositionEnum == TargetPositionType.CandidateList)
            {
                if (clip.candidatePositions != null && clip.candidatePositions.Length > 0)
                {
                    for (int i = 0; i < clip.candidatePositions.Length; i++)
                    {
                        var candidate = clip.candidatePositions[i];
                        if (candidate == null) continue;

                        Vector3 candDir = CalculateOffsetDirection(candidate.targetPositionEnum, candidate.targetBaseDirection, candidate.angleOffset, target, startPos, inputDir);
                        
                        float candTargetOffset = GetAnchorOffset(candidate.targetAnchor, rTarget);
                        float candSelfOffset = GetAnchorOffset(candidate.selfAnchor, rSelf);
                        float desiredDist = candTargetOffset + candSelfOffset + candidate.offsetRadius;
                        float candDist = Mathf.Max(desiredDist, minSafeDist);
                        
                        Vector3 rawCandidatePos = targetPos + candDir * candDist;

                        if (ValidatePosition(rawCandidatePos, candidate.heightOffset, rSelf, hSelf, cSelf,
                                clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                                clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validPos))
                        {
                            isFound = true;
                            return validPos;
                        }
                    }
                }

                // 候选列表全部受阻，若开启了径向扩散搜索，则以首选候选点（或身后）为基准向两侧扩散搜索
                if (clip.enableSmartRadialFallback)
                {
                    TargetBaseDirection baseDir = TargetBaseDirection.LineOfSight;
                    float baseAngle = 180f; // 默认身后
                    float distance = rTarget + rSelf + clip.offsetRadius;

                    if (clip.candidatePositions != null && clip.candidatePositions.Length > 0 && clip.candidatePositions[0] != null)
                    {
                        var firstCand = clip.candidatePositions[0];
                        baseDir = firstCand.targetBaseDirection;
                        Vector3 baseFwd = GetBaseDirectionVector(baseDir, target, startPos);
                        baseAngle = GetTypeBaseAngle(firstCand.targetPositionEnum, inputDir, baseFwd) + firstCand.angleOffset;
                        
                        float candTargetOffset = GetAnchorOffset(firstCand.targetAnchor, rTarget);
                        float candSelfOffset = GetAnchorOffset(firstCand.selfAnchor, rSelf);
                        float desiredDist = candTargetOffset + candSelfOffset + firstCand.offsetRadius;
                        distance = Mathf.Max(desiredDist, minSafeDist);
                    }

                    Vector3 baseForward = GetBaseDirectionVector(baseDir, target, startPos);
                    float step = Mathf.Max(5f, clip.fallbackAngleStep);
                    float maxAngle = Mathf.Clamp(clip.maxFallbackAngle, 10f, 180f);

                    for (float offset = step; offset <= maxAngle + 0.01f; offset += step)
                    {
                        // 对称测试左右两侧角度 (先 +offset 后 -offset)
                        float angleA = baseAngle + offset;
                        Vector3 dirA = Quaternion.AngleAxis(angleA, Vector3.up) * baseForward;
                        Vector3 rawPosA = targetPos + dirA * distance;

                        if (ValidatePosition(rawPosA, 0f, rSelf, hSelf, cSelf,
                                clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                                clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validPosA))
                        {
                            isFound = true;
                            return validPosA;
                        }

                        float angleB = baseAngle - offset;
                        Vector3 dirB = Quaternion.AngleAxis(angleB, Vector3.up) * baseForward;
                        Vector3 rawPosB = targetPos + dirB * distance;

                        if (ValidatePosition(rawPosB, 0f, rSelf, hSelf, cSelf,
                                clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                                clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validPosB))
                        {
                            isFound = true;
                            return validPosB;
                        }
                    }
                }

                // 候选列表与扩散搜索均无可用合法位置：不进行任何移动，保持原地
                isFound = false;
                return startPos;
            }
            else // 模式 B: 单一目标位置模式 (EnemyFront, EnemyBack, CustomAngle 等)
            {
                Vector3 mainDir = CalculateOffsetDirection(clip.targetPositionEnum, clip.targetBaseDirection, clip.angleOffset, target, startPos, inputDir);
                
                float desiredDist = targetOffset + selfOffset + clip.offsetRadius;
                float mainDist = Mathf.Max(desiredDist, minSafeDist);
                
                Vector3 rawMainPos = targetPos + mainDir * mainDist;

                if (ValidatePosition(rawMainPos, 0f, rSelf, hSelf, cSelf,
                        clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                        clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validMainPos))
                {
                    isFound = true;
                    return validMainPos;
                }

                // 单一目标点受阻，若开启了径向扩散搜索，以当前设定方向向左右两侧圆周扇形扩散搜索
                if (clip.enableSmartRadialFallback)
                {
                    Vector3 baseForward = GetBaseDirectionVector(clip.targetBaseDirection, target, startPos);
                    float baseAngle = GetTypeBaseAngle(clip.targetPositionEnum, inputDir, baseForward) + clip.angleOffset;
                    float step = Mathf.Max(5f, clip.fallbackAngleStep);
                    float maxAngle = Mathf.Clamp(clip.maxFallbackAngle, 10f, 180f);

                    for (float offset = step; offset <= maxAngle + 0.01f; offset += step)
                    {
                        float angleA = baseAngle + offset;
                        Vector3 dirA = Quaternion.AngleAxis(angleA, Vector3.up) * baseForward;
                        Vector3 rawPosA = targetPos + dirA * mainDist;

                        if (ValidatePosition(rawPosA, 0f, rSelf, hSelf, cSelf,
                                clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                                clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validPosA))
                        {
                            isFound = true;
                            return validPosA;
                        }

                        float angleB = baseAngle - offset;
                        Vector3 dirB = Quaternion.AngleAxis(angleB, Vector3.up) * baseForward;
                        Vector3 rawPosB = targetPos + dirB * mainDist;

                        if (ValidatePosition(rawPosB, 0f, rSelf, hSelf, cSelf,
                                clip.obstacleLayers, clip.groundLayers, clip.groundCheckDistance,
                                clip.requireGrounded, selfCollider, targetCollider, target, out Vector3 validPosB))
                        {
                            isFound = true;
                            return validPosB;
                        }
                    }
                }

                // 单一目标受阻且扩散搜索无结果：不进行任何移动，保持原地
                isFound = false;
                return startPos;
            }
        }

        private static float GetAnchorOffset(DistanceAnchor anchor, float radius)
        {
            switch (anchor)
            {
                case DistanceAnchor.Root:
                    return 0f;
                case DistanceAnchor.NearEdge:
                    return radius;
                default:
                    return radius;
            }
        }

        /// <summary>
        /// 计算相对于目标的单位偏移方向 (TargetPositionType)
        /// </summary>
        public static Vector3 CalculateOffsetDirection(
            TargetPositionType posType,
            TargetBaseDirection baseDir,
            float angleOffset,
            Transform target,
            Vector3 ownerPos,
            Vector3 inputDir)
        {
            if (target == null) return Vector3.forward;

            Vector3 baseForward = GetBaseDirectionVector(baseDir, target, ownerPos);
            float baseAngle = GetTypeBaseAngle(posType, inputDir, baseForward);
            float finalAngle = baseAngle + angleOffset;

            return (Quaternion.AngleAxis(finalAngle, Vector3.up) * baseForward).normalized;
        }

        /// <summary>
        /// 计算相对于目标的单位偏移方向 (CandidatePositionType)
        /// </summary>
        public static Vector3 CalculateOffsetDirection(
            CandidatePositionType posType,
            TargetBaseDirection baseDir,
            float angleOffset,
            Transform target,
            Vector3 ownerPos,
            Vector3 inputDir)
        {
            if (target == null) return Vector3.forward;

            Vector3 baseForward = GetBaseDirectionVector(baseDir, target, ownerPos);
            float baseAngle = GetTypeBaseAngle(posType, inputDir, baseForward);
            float finalAngle = baseAngle + angleOffset;

            return (Quaternion.AngleAxis(finalAngle, Vector3.up) * baseForward).normalized;
        }

        private static Vector3 GetBaseDirectionVector(TargetBaseDirection baseDir, Transform target, Vector3 ownerPos)
        {
            if (target == null) return Vector3.forward;

            if (baseDir == TargetBaseDirection.LineOfSight)
            {
                Vector3 toOwner = ownerPos - target.position;
                toOwner.y = 0f;
                if (toOwner.sqrMagnitude > 0.0001f)
                {
                    return toOwner.normalized;
                }
                return target.forward;
            }
            else // TargetFacing
            {
                Vector3 targetFwd = target.forward;
                targetFwd.y = 0f;
                return targetFwd.sqrMagnitude > 0.0001f ? targetFwd.normalized : Vector3.forward;
            }
        }

        private static float GetTypeBaseAngle(CandidatePositionType posType, Vector3 inputDir, Vector3 baseForward)
        {
            switch (posType)
            {
                case CandidatePositionType.EnemyFront:
                    return 0f;
                case CandidatePositionType.EnemyBack:
                    return 180f;
                case CandidatePositionType.EnemyLeft:
                    return -90f;
                case CandidatePositionType.EnemyRight:
                    return 90f;
                case CandidatePositionType.CustomAngle:
                    return 0f;
                case CandidatePositionType.InputDirection:
                    if (inputDir.sqrMagnitude > 0.01f && baseForward.sqrMagnitude > 0.01f)
                    {
                        inputDir.y = 0f;
                        return Vector3.SignedAngle(baseForward, inputDir.normalized, Vector3.up);
                    }
                    return 0f;
                default:
                    return 0f;
            }
        }

        private static float GetTypeBaseAngle(TargetPositionType posType, Vector3 inputDir, Vector3 baseForward)
        {
            switch (posType)
            {
                case TargetPositionType.EnemyFront:
                    return 0f;
                case TargetPositionType.EnemyBack:
                    return 180f;
                case TargetPositionType.EnemyLeft:
                    return -90f;
                case TargetPositionType.EnemyRight:
                    return 90f;
                case TargetPositionType.CustomAngle:
                    return 0f;
                case TargetPositionType.InputDirection:
                    if (inputDir.sqrMagnitude > 0.01f && baseForward.sqrMagnitude > 0.01f)
                    {
                        inputDir.y = 0f;
                        return Vector3.SignedAngle(baseForward, inputDir.normalized, Vector3.up);
                    }
                    return 0f;
                case TargetPositionType.CandidateList:
                    return 180f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 校验候选位置的物理可用性（地形贴合、悬崖判定、障碍物胶囊碰撞体侵入、视线阻隔检测）
        /// </summary>
        public static bool ValidatePosition(
            Vector3 rawPos,
            float heightOffset,
            float characterRadius,
            float characterHeight,
            Vector3 characterCenter,
            LayerMask obstacleLayers,
            LayerMask groundLayers,
            float groundCheckDistance,
            bool requireGrounded,
            Collider selfCollider,
            Collider targetCollider,
            Transform target,
            out Vector3 validPos)
        {
            validPos = rawPos;

            // 1. 地面投射与高度对其 (Ground Snapping / Pit Check)
            float rayOriginHeight = 1.0f;
            Vector3 rayOrigin = new Vector3(rawPos.x, rawPos.y + rayOriginHeight, rawPos.z);
            float totalCheckDist = Mathf.Max(1.0f, groundCheckDistance) + rayOriginHeight;

            // 若配置了 groundLayers 则使用，否则默认使用 Default 层 (Layer 0 => bit mask 1)
            LayerMask effectiveGroundMask = groundLayers.value != 0 ? groundLayers : (LayerMask)1;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, totalCheckDist, effectiveGroundMask, QueryTriggerInteraction.Ignore))
            {
                validPos.y = groundHit.point.y + heightOffset;
            }
            else
            {
                if (requireGrounded)
                {
                    // 悬空 / 掉落悬崖外，位置不可用
                    return false;
                }
            }

            // 2. 障碍物碰撞胶囊体重叠检测 (Capsule Overlap Clearance)
            if (obstacleLayers.value != 0)
            {
                float safeRadius = Mathf.Max(0.1f, characterRadius * 0.90f);
                float halfHeight = Mathf.Max(characterHeight * 0.5f, safeRadius);
                
                Vector3 centerWorld = validPos + characterCenter;
                Vector3 pointBottom = centerWorld - Vector3.up * (halfHeight - safeRadius);
                Vector3 pointTop = centerWorld + Vector3.up * (halfHeight - safeRadius);

                Collider[] overlaps = Physics.OverlapCapsule(pointBottom, pointTop, safeRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
                if (overlaps != null && overlaps.Length > 0)
                {
                    for (int i = 0; i < overlaps.Length; i++)
                    {
                        var col = overlaps[i];
                        if (col == null || col.isTrigger) continue;

                        // 忽略角色自身及其子物体的碰撞体
                        if (selfCollider != null && (col == selfCollider || col.transform.IsChildOf(selfCollider.transform)))
                        {
                            continue;
                        }

                        // 忽略锁定目标及其子物体的碰撞体
                        if (targetCollider != null && (col == targetCollider || col.transform.IsChildOf(targetCollider.transform)))
                        {
                            continue;
                        }

                        // 检测到地形/墙体/其他障碍物阻挡
                        return false;
                    }
                }
            }

            // 3. 目标与候选点之间的视线与阻挡贯穿检测 (LOS / Wall Penetration Check)
            if (target != null && obstacleLayers.value != 0)
            {
                Vector3 targetEye = target.position + Vector3.up * 0.8f;
                Vector3 candidateEye = validPos + Vector3.up * 0.8f;
                Vector3 toCandidate = candidateEye - targetEye;
                float dist = toCandidate.magnitude;

                if (dist > 0.1f)
                {
                    if (Physics.Raycast(targetEye, toCandidate.normalized, out RaycastHit wallHit, dist, obstacleLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (selfCollider == null || (!wallHit.collider.transform.IsChildOf(selfCollider.transform) && wallHit.collider != selfCollider))
                        {
                            if (targetCollider == null || (!wallHit.collider.transform.IsChildOf(targetCollider.transform) && wallHit.collider != targetCollider))
                            {
                                // 目标与候选点之间存在厚墙或实体阻隔，穿透不可达
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
