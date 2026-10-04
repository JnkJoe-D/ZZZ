using System.Collections.Generic;
using UnityEngine;
using ATEditor;
using Game.Framework;

namespace Game.GamePlay
{


    /// <summary>
    /// 攻击预警标记
    /// </summary>
    public class AttackWarningMarker
    {
        public CharacterEntity Attacker;
        public WarningSignalType SignalType;
        public ParryWeight ParryWeight = ParryWeight.Heavy;
        public float DetectionRadius;
        public float DetectionAngle;

        // 方案 C 扩展：覆盖域形状与理想接刀身位
        public HitBoxShape CoverageShape;
        public Vector3 CoverageCenterOffset;
        public Vector3 ClashPositionOffset = new Vector3(0f, 0f, 1.8f);
        public bool AllowInPlaceParry = true;
        public float RestrictedInnerRadius = 1.2f;
        public float InPlaceLineTolerance = 0.5f;
        public float StartTime;
        public float Duration;
        public float EndTime => StartTime + Duration;
        public float DirectClashTimeOffset;
        /// <summary>
        /// 怪物攻击判定帧到来的绝对物理时间戳 (Time.time + Duration)
        /// </summary>
        public float ExpectedHitTime;
        
        /// <summary>
        /// 检查目标是否在攻击预警感应范围内（供切换角色/闪避路线条件触发判定）
        /// </summary>
        public bool IsTargetInArea(CharacterEntity target)
        {
            if (target == null || Attacker == null) return false;
            if (!Attacker.gameObject.activeInHierarchy || (Attacker.ActionPlayer != null && !Attacker.ActionPlayer.IsPlaying))
                return false;

            Vector3 dirToTarget = target.transform.position - Attacker.transform.position;
            // 距离检测：取配置值与战场通用感应半径的较大值，确保远处怪物蓄力闪黄光时玩家招架能够被正确感应
            float validRadius = DetectionRadius > 0 ? Mathf.Max(DetectionRadius, CombatWarningManager.DefaultDetectionRadius) : CombatWarningManager.DefaultDetectionRadius;
            if (dirToTarget.sqrMagnitude > validRadius * validRadius)
                return false;

            // 角度检测：若未特化配置严格狭窄扇形（如 < 180度），默认采用 360 度全方位威胁感知，完美支持背向偷袭接刀
            float validAngle = (DetectionAngle > 0 && DetectionAngle < 180f) ? DetectionAngle : CombatWarningManager.DefaultDetectionAngle;
            if (validAngle < 360.0f)
            {
                dirToTarget.y = 0;
                Vector3 attackerForward = Attacker.transform.forward;
                attackerForward.y = 0;
                float angle = Vector3.Angle(attackerForward, dirToTarget);
                if (angle > validAngle * 0.5f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查指定世界坐标（或角色站位）是否处于该攻击的威胁覆盖域内（用于决定就地招架还是瞬移至接刀点）
        /// </summary>
        public bool IsPositionInCoverage(Vector3 worldPos)
        {
            if (Attacker == null || !Attacker.gameObject.activeInHierarchy || (Attacker.ActionPlayer != null && !Attacker.ActionPlayer.IsPlaying))
                return false;

            if (CoverageShape == null)
            {
                return false;
            }

            // 采样角色中心（脚底向上 0.9m，代表人体躯干中心）
            Vector3 charCenter = worldPos + Vector3.up * 0.9f;
            Vector3 localPos = Attacker.transform.InverseTransformPoint(charCenter) - CoverageCenterOffset;

            // 环形贴脸禁区检查：若距攻击者中心过近，禁止就地招架，强制瞬移至 ClashPosition
            if (RestrictedInnerRadius > 0f)
            {
                float dist2DToAttacker = new Vector2(localPos.x, localPos.z).magnitude;
                if (dist2DToAttacker < RestrictedInnerRadius) return false;
            }

            // 角色世界垂直区间 [worldPos.y, worldPos.y + 1.8f] 转为局部 Y
            float charLocalMinY = Attacker.transform.InverseTransformPoint(worldPos).y - CoverageCenterOffset.y;
            float charLocalMaxY = charLocalMinY + 1.8f;

            switch (CoverageShape.shapeType)
            {
                case HitBoxType.Sector:
                    float sectorMinY = -CoverageShape.height * 0.5f;
                    float sectorMaxY = CoverageShape.height * 0.5f;
                    if (charLocalMaxY < sectorMinY || charLocalMinY > sectorMaxY) return false;

                    float dist2D = new Vector2(localPos.x, localPos.z).magnitude;
                    if (dist2D > CoverageShape.radius) return false;
                    float forwardAngle = Vector2.Angle(Vector2.up, new Vector2(localPos.x, localPos.z));
                    return forwardAngle <= CoverageShape.angle * 0.5f;

                case HitBoxType.Box:
                    Vector3 halfSize = CoverageShape.size * 0.5f;
                    if (Mathf.Abs(localPos.x) > halfSize.x || Mathf.Abs(localPos.z) > halfSize.z) return false;
                    if (charLocalMaxY < -halfSize.y || charLocalMinY > halfSize.y) return false;
                    return true;

                case HitBoxType.Sphere:
                    return localPos.magnitude <= CoverageShape.radius;

                case HitBoxType.Capsule:
                    float radius = CoverageShape.radius;
                    float halfH = Mathf.Max(0f, (CoverageShape.height * 0.5f) - radius);
                    Vector3 ptOnAxis = new Vector3(0, Mathf.Clamp(localPos.y, -halfH, halfH), 0);
                    return (localPos - ptOnAxis).sqrMagnitude <= radius * radius;

                case HitBoxType.Ring:
                    float rMinY = -CoverageShape.height * 0.5f;
                    float rMaxY = CoverageShape.height * 0.5f;
                    if (charLocalMaxY < rMinY || charLocalMinY > rMaxY) return false;
                    float ringDist = new Vector2(localPos.x, localPos.z).magnitude;
                    return ringDist >= CoverageShape.innerRadius && ringDist <= CoverageShape.radius;

                default:
                    return localPos.sqrMagnitude <= CoverageShape.radius * CoverageShape.radius;
            }
        }

        /// <summary>
        /// 计算接刀点的安全世界坐标（支持射线探地贴合）
        /// </summary>
        public Vector3 GetWorldClashPosition(LayerMask groundLayers, float groundCheckDist = 3.0f)
        {
            if (Attacker == null) return Vector3.zero;
            Vector3 rawWorldPos = Attacker.transform.TransformPoint(ClashPositionOffset);

            // 若外部传入的 layer 为空，自动排除角色受击与UI层，寻找真实地面
            if (groundLayers.value == 0)
            {
                int excludeMask = LayerMask.GetMask("Ignore Raycast", "UI", "CharHit", "Character", "LocalRole", "Camera", "CharIgnore", "Water");
                groundLayers = excludeMask != 0 ? (LayerMask)(~excludeMask) : LayerMask.GetMask("Default", "Ground", "Wall");
                if (groundLayers.value == 0) groundLayers = ~0;
            }

            // 垂直向下射线探测地面
            Vector3 rayStart = rawWorldPos + Vector3.up * 1.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDist + 1.5f, groundLayers))
            {
                rawWorldPos.y = hit.point.y;
            }
            else
            {
                // 若探地未击中，以怪物脚底高度为基准平贴地面
                rawWorldPos.y = Attacker.transform.position.y;
            }
            return rawWorldPos;
        }

        public Vector3 GetWorldClashPosition(CharacterEntity entity)
        {
            LayerMask layer = entity?.Config?.GroundLayer ?? LayerMask.GetMask("Ground");
            return GetWorldClashPosition(layer);
        }

        /// <summary>
        /// 检查指定世界坐标是否处于怪物位置与预设接刀点之间的连线上（XZ 平面）
        /// </summary>
        public bool IsOnClashLine(Vector3 worldPos, float tolerance = -1f)
        {
            if (Attacker == null) return false;

            float tol = tolerance > 0f ? tolerance : (InPlaceLineTolerance > 0f ? InPlaceLineTolerance : 0.5f);

            Vector3 attackerWorld = Attacker.transform.position;
            Vector3 clashWorld = Attacker.transform.TransformPoint(ClashPositionOffset);

            Vector2 a = new Vector2(attackerWorld.x, attackerWorld.z);
            Vector2 b = new Vector2(clashWorld.x, clashWorld.z);
            Vector2 p = new Vector2(worldPos.x, worldPos.z);

            Vector2 ab = b - a;
            float lineLen = ab.magnitude;
            if (lineLen < 0.001f) return false;

            Vector2 dir = ab / lineLen;
            Vector2 ap = p - a;

            // 沿连线方向的投影距离
            float projDist = Vector2.Dot(ap, dir);

            // 投影必须在怪物与接刀点之间（端点允许适度容差）
            if (projDist < 0f || projDist > lineLen + tol)
            {
                return false;
            }

            // 垂直于连线的偏离距离
            Vector2 perp = ap - projDist * dir;
            float distToLine = perp.magnitude;

            return distToLine <= tol;
        }

        /// <summary>
        /// 检查是否满足原地招架条件：
        /// 1. 允许就地招架 (AllowInPlaceParry == true)
        /// 2. 处于禁区外 (距攻击者中心 >= RestrictedInnerRadius)
        /// 3. 处于有效覆盖域内 (CoverageShape 内)
        /// 4. 处于怪物位置与预设接刀点之间的连线上 (IsOnClashLine == true)
        /// 必须全部符合才能原地招架，否则判定为瞬移招架。
        /// </summary>
        public bool CanPerformInPlaceParry(Vector3 worldPos, float lineTolerance = -1f)
        {
            if (!AllowInPlaceParry) return false;
            if (Attacker == null || !Attacker.gameObject.activeInHierarchy) return false;

            // 1 & 2: 禁区外 + 有效范围内（IsPositionInCoverage 内部已权威包含贴脸禁区与各种形状的有效域判定）
            if (!IsPositionInCoverage(worldPos)) return false;

            // 3: 怪物位置和预设接刀点之间的连线上
            if (!IsOnClashLine(worldPos, lineTolerance)) return false;

            return true;
        }
    }

    /// <summary>
    /// 招架对峙确定性拼刀契约
    /// </summary>
    public class ParryClashContract
    {
        public CharacterEntity Attacker;     // 攻击方（怪物）
        public CharacterEntity ParryRole;    // 防守招架方（玩家切入角色）
        public AttackWarningMarker Marker;   // 关联的预警数据
        public bool IsResolved;              // 是否已至少完成一次拼刀命中
        public ParryPrecomputedData PrecomputedData; // 契约绑定的预裁决纯数据上下文

        // ─── 双周期时间划分 ───
        public float ContractStartTime;
        public float ContractEndTime;
        public float DirectClashStartTime; // = ContractStartTime + DirectClashTimeOffset

        /// <summary>
        /// 判断当前攻击者所在的时间点是否已进入正式直接招架期
        /// </summary>
        public bool IsInDirectClashPhase(float? specifiedTime = null)
        {
            // 若未配置分期（DirectClashStartTime <= ContractStartTime），整个契约期均视为正式招架期
            if (DirectClashStartTime <= ContractStartTime) return true;

            float curTime;
            if (specifiedTime.HasValue)
            {
                curTime = specifiedTime.Value;
            }
            else
            {
                if (Attacker == null || Attacker.ActionPlayer == null) return true;
                curTime = Attacker.ActionPlayer.CurrentTime;
            }

            return curTime >= DirectClashStartTime;
        }

        public bool IsValid => 
            Attacker != null &&
            Attacker.gameObject.activeInHierarchy &&
            (ParryRole == null || ParryRole.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// 战斗预警全局管理器
    /// 存储当前激活的攻击预警（来自怪物的动作时间轴），供玩家切换角色或闪避时进行检测
    /// </summary>
    public static class CombatWarningManager
    {
        public const float DefaultDetectionRadius = 25.0f;
        public const float DefaultDetectionAngle = 360.0f;

        private static readonly List<AttackWarningMarker> _activeMarkers = new List<AttackWarningMarker>();
        private static readonly List<ParryClashContract> _activeContracts = new List<ParryClashContract>();
        private static readonly Dictionary<CharacterEntity, AttackThreatSession> _activeThreatSessions = new();

        public static AttackThreatSession GetActiveThreatSession(CharacterEntity attacker)
        {
            if (attacker == null) return null;
            if (_activeThreatSessions.TryGetValue(attacker, out var session))
            {
                if (!attacker.gameObject.activeInHierarchy || (attacker.DataModule?.Get<LifecycleRuntimeData>()?.IsDead ?? false))
                {
                    _activeThreatSessions.Remove(attacker);
                    AttackThreatSession.Release(session);
                    return null;
                }
                return session;
            }
            return null;
        }

        public static void CloseThreatSession(CharacterEntity attacker)
        {
            if (attacker == null) return;
            if (_activeThreatSessions.TryGetValue(attacker, out var session))
            {
                session.Close();
                // 保持 session 在字典中（维持 IsClosed = true），供同次挥刀动作后半程物理帧免伤识别
                // 只有当实体开启下一次出招注册新预警、死亡或场景清理时，才真正 Release 回收入池
            }
        }

        public static bool Register(AttackWarningMarker marker)
        {
            if (marker != null && !_activeMarkers.Contains(marker))
            {
                _activeMarkers.Add(marker);
                if (marker.Attacker != null)
                {
                    if (_activeThreatSessions.TryGetValue(marker.Attacker, out var oldSession))
                    {
                        AttackThreatSession.Release(oldSession);
                    }
                    var session = AttackThreatSession.Allocate(marker.Attacker, marker);
                    _activeThreatSessions[marker.Attacker] = session;
                }
                return true;
            }
            return false;
        }

        public static void Unregister(AttackWarningMarker marker)
        {
            if (marker != null)
            {
                _activeMarkers.Remove(marker);
                // 核心纠偏：预警 Clip 离开是正常的生理时钟（在攻击判定帧之前结束），
                // 绝不在此销毁 AttackThreatSession，因为挥刀判定正在进行或正要来临！
            }
        }

        public static void RegisterContract(ParryClashContract contract)
        {
            if (contract != null && !_activeContracts.Contains(contract))
            {
                _activeContracts.Add(contract);
                // 仅针对当前正式确立拼刀契约的单一怪物发起 1 对 1 预裁决（零泛洪计算）
                ParryPreArbitrator.Precompute(contract);
            }
        }

        public static void UnregisterContract(ParryClashContract contract)
        {
            if (contract != null)
            {
                _activeContracts.Remove(contract);
                contract.PrecomputedData?.Reset();
            }
        }

        public static void UnregisterContractsByRole(CharacterEntity role)
        {
            if (role == null) return;
            for (int i = _activeContracts.Count - 1; i >= 0; i--)
            {
                if (_activeContracts[i].ParryRole == role)
                {
                    _activeContracts.RemoveAt(i);
                }
            }
        }

        public static void UnregisterContractsByAttacker(CharacterEntity attacker)
        {
            if (attacker == null) return;
            for (int i = _activeContracts.Count - 1; i >= 0; i--)
            {
                if (_activeContracts[i].Attacker == attacker)
                {
                    _activeContracts.RemoveAt(i);
                }
            }
        }

        public static ParryClashContract GetActiveContract(CharacterEntity attacker)
        {
            if (attacker == null) return null;
            for (int i = _activeContracts.Count - 1; i >= 0; i--)
            {
                var contract = _activeContracts[i];
                if (!contract.IsValid)
                {
                    _activeContracts.RemoveAt(i);
                    continue;
                }
                if (contract.Attacker == attacker)
                {
                    return contract;
                }
            }
            return null;
        }

        public static ParryClashContract GetActiveContractByRole(CharacterEntity role)
        {
            if (role == null) return null;
            for (int i = _activeContracts.Count - 1; i >= 0; i--)
            {
                var contract = _activeContracts[i];
                if (!contract.IsValid)
                {
                    _activeContracts.RemoveAt(i);
                    continue;
                }
                if (contract.ParryRole == role)
                {
                    return contract;
                }
            }
            return null;
        }

        public static void Clear()
        {
            _activeMarkers.Clear();
            _activeContracts.Clear();
            foreach (var kvp in _activeThreatSessions)
            {
                AttackThreatSession.Release(kvp.Value);
            }
            _activeThreatSessions.Clear();
        }

        /// <summary>
        /// 查找对目标有效的预警标记（锁定优先 + 危险自动索敌最近攻击者）
        /// </summary>
        public static AttackWarningMarker GetValidWarning(CharacterEntity target, WarningSignalType type)
        {
            if (target == null) return null;

            // 1. 锁定优先：若当前锁定目标正在出招预警，绝对优先响应（无距离限制，无论多远直接触发切人瞬移接刀）
            Transform lockedTrans = target.TargetFinder?.GetTarget();
            if (lockedTrans != null)
            {
                for (int i = _activeMarkers.Count - 1; i >= 0; i--)
                {
                    var m = _activeMarkers[i];
                    if (m.Attacker != null && m.Attacker.transform == lockedTrans && m.SignalType == type)
                    {
                        if (m.Attacker.gameObject.activeInHierarchy && (m.Attacker.ActionPlayer == null || m.Attacker.ActionPlayer.IsPlaying))
                        {
                            return m;
                        }
                    }
                }
            }

            // 2. 危险自动索敌：寻找感应范围内距离最近的预警攻击者
            AttackWarningMarker closestMarker = null;
            float closestDistSqr = float.MaxValue;

            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker.Attacker == null || !marker.Attacker.gameObject.activeInHierarchy)
                {
                    _activeMarkers.RemoveAt(i);
                    continue;
                }

                if (marker.SignalType == type && marker.IsTargetInArea(target))
                {
                    float distSqr = (marker.Attacker.transform.position - target.transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestMarker = marker;
                    }
                }
            }
            return closestMarker;
        }

        /// <summary>
        /// 查找对目标有效的任何类型的预警标记（锁定优先 + 危险自动索敌最近攻击者）
        /// </summary>
        public static AttackWarningMarker GetAnyValidWarning(CharacterEntity target)
        {
            if (target == null) return null;

            // 1. 锁定优先：若当前锁定目标有预警，绝对优先响应（无距离限制）
            Transform lockedTrans = target.TargetFinder?.GetTarget();
            if (lockedTrans != null)
            {
                for (int i = _activeMarkers.Count - 1; i >= 0; i--)
                {
                    var m = _activeMarkers[i];
                    if (m.Attacker != null && m.Attacker.transform == lockedTrans)
                    {
                        if (m.Attacker.gameObject.activeInHierarchy && (m.Attacker.ActionPlayer == null || m.Attacker.ActionPlayer.IsPlaying))
                        {
                            return m;
                        }
                    }
                }
            }

            // 2. 危险自动索敌
            AttackWarningMarker closestMarker = null;
            float closestDistSqr = float.MaxValue;

            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker.Attacker == null || !marker.Attacker.gameObject.activeInHierarchy)
                {
                    _activeMarkers.RemoveAt(i);
                    continue;
                }

                if (marker.IsTargetInArea(target))
                {
                    float distSqr = (marker.Attacker.transform.position - target.transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestMarker = marker;
                    }
                }
            }
            return closestMarker;
        }

        /// <summary>
        /// 查找某个攻击者发出的有效预警（用于怪物的招架受击判断）
        /// </summary>
        public static AttackWarningMarker GetWarningByAttacker(CharacterEntity attacker)
        {
            if (attacker == null) return null;

            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker.Attacker == null || !marker.Attacker.gameObject.activeInHierarchy)
                {
                    _activeMarkers.RemoveAt(i);
                    continue;
                }

                if (marker.Attacker == attacker)
                {
                    return marker;
                }
            }
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitEventSubscription()
        {
            EventCenter.Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        private static void OnEntityDied(EntityDiedEvent evt)
        {
            if (evt.Victim != null)
            {
                HandleEntityRemoved(evt.Victim);
            }
        }

        /// <summary>
        /// 当实体死亡或被销毁时，主动自闭环清理与其关联的 Marker、Contract 与 ThreatSession
        /// </summary>
        public static void HandleEntityRemoved(CharacterEntity entity)
        {
            if (entity == null) return;

            // 1. 清理该实体作为攻击方的预警标记
            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                if (_activeMarkers[i].Attacker == entity)
                {
                    _activeMarkers.RemoveAt(i);
                }
            }

            // 2. 清理该实体相关的拼刀契约 (无论是攻击方还是防守招架方)
            UnregisterContractsByAttacker(entity);
            UnregisterContractsByRole(entity);

            // 3. 清理该实体对应的威胁会话并回收入池
            if (_activeThreatSessions.TryGetValue(entity, out var session))
            {
                _activeThreatSessions.Remove(entity);
                AttackThreatSession.Release(session);
            }
        }

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void AutoClearOnDomainReload()
        {
            Clear();
        }
#endif
    }
}
