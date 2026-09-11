using System.Collections.Generic;
using UnityEngine;
using ATEditor;

namespace Game.Logic
{


    /// <summary>
    /// 攻击预警标记
    /// </summary>
    public class AttackWarningMarker
    {
        public CharacterEntity Attacker;
        public WarningSignalType SignalType;
        public AttackWeight Weight;
        public float DetectionRadius;
        public float DetectionAngle;

        // 方案 C 扩展：覆盖域形状与理想接刀身位
        public HitBoxShape CoverageShape;
        public Vector3 CoverageCenterOffset;
        public Vector3 ClashPositionOffset = new Vector3(0f, 0f, 1.8f);
        public bool AllowInPlaceParry = true;
        
        /// <summary>
        /// 检查目标是否在攻击预警感应范围内（供切换角色/闪避路线条件触发判定）
        /// </summary>
        public bool IsTargetInArea(CharacterEntity target)
        {
            if (target == null || Attacker == null) return false;
            if (!Attacker.gameObject.activeInHierarchy || (Attacker.ActionPlayer != null && !Attacker.ActionPlayer.IsPlaying))
                return false;

            Vector3 dirToTarget = target.transform.position - Attacker.transform.position;
            // 距离检测：严格从配置的 DetectionRadius 读取（若未配置或 <= 0 则回退至默认 10.0f）
            float validRadius = DetectionRadius > 0 ? DetectionRadius : 10.0f;
            if (dirToTarget.sqrMagnitude > validRadius * validRadius)
                return false;

            // 角度检测
            dirToTarget.y = 0;
            Vector3 attackerForward = Attacker.transform.forward;
            attackerForward.y = 0;

            float validAngle = DetectionAngle > 0 ? DetectionAngle : 180.0f;
            float angle = Vector3.Angle(attackerForward, dirToTarget);
            if (angle > validAngle * 0.5f)
                return false;

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

            // 垂直向下射线探测地面
            Vector3 rayStart = rawWorldPos + Vector3.up * 1.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDist + 1.5f, groundLayers))
            {
                rawWorldPos.y = hit.point.y;
            }
            return rawWorldPos;
        }

        public Vector3 GetWorldClashPosition()
        {
            return GetWorldClashPosition(LayerMask.GetMask("Default", "Ground", "Terrain"));
        }
    }

    /// <summary>
    /// 招架对峙确定性拼刀契约（方案 D）
    /// </summary>
    public class ParryClashContract
    {
        public CharacterEntity Attacker;     // 攻击方（怪物）
        public RoleEntity ParryRole;         // 防守招架方（玩家切入角色）
        public AttackWarningMarker Marker;   // 关联的预警数据
        public float ExpireTime;             // 契约超时失效时间点
        public bool IsResolved;              // 是否已至少完成一次拼刀命中

        public bool IsValid => Time.time <= ExpireTime &&
                               Attacker != null &&
                               Attacker.gameObject.activeInHierarchy &&
                               ParryRole != null &&
                               ParryRole.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 战斗预警全局管理器
    /// 存储当前激活的攻击预警（来自怪物的动作时间轴），供玩家切换角色或闪避时进行检测
    /// </summary>
    public static class CombatWarningManager
    {
        private static readonly List<AttackWarningMarker> _activeMarkers = new List<AttackWarningMarker>();
        private static readonly List<ParryClashContract> _activeContracts = new List<ParryClashContract>();

        public static bool Register(AttackWarningMarker marker)
        {
            if (marker != null && !_activeMarkers.Contains(marker))
            {
                _activeMarkers.Add(marker);
                return true;
            }
            return false;
        }

        public static void Unregister(AttackWarningMarker marker)
        {
            if (marker != null)
            {
                _activeMarkers.Remove(marker);
            }
        }

        public static void RegisterContract(ParryClashContract contract)
        {
            if (contract != null && !_activeContracts.Contains(contract))
            {
                _activeContracts.Add(contract);
            }
        }

        public static void UnregisterContract(ParryClashContract contract)
        {
            if (contract != null)
            {
                _activeContracts.Remove(contract);
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
        }

        /// <summary>
        /// 查找对目标有效的预警标记（目标需在范围内且预警类型匹配）
        /// </summary>
        public static AttackWarningMarker GetValidWarning(CharacterEntity target, WarningSignalType type)
        {
            // 倒序遍历，优先响应最新的预警
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
                    return marker;
                }
            }
            return null;
        }

        /// <summary>
        /// 查找对目标有效的任何类型的预警标记（通常用于极限闪避）
        /// </summary>
        public static AttackWarningMarker GetAnyValidWarning(CharacterEntity target)
        {
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
                    return marker;
                }
            }
            return null;
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

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void AutoClearOnDomainReload()
        {
            Clear();
        }
#endif
    }
}
