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
        
        /// <summary>
        /// 检查目标是否在攻击预警扇形范围内
        /// </summary>
        public bool IsTargetInArea(CharacterEntity target)
        {
            if (target == null || Attacker == null) return false;

            Vector3 dirToTarget = target.transform.position - Attacker.transform.position;
            // 距离检测
            if (dirToTarget.sqrMagnitude > DetectionRadius * DetectionRadius) 
                return false;
            
            // 角度检测
            dirToTarget.y = 0;
            Vector3 attackerForward = Attacker.transform.forward;
            attackerForward.y = 0;

            float angle = Vector3.Angle(attackerForward, dirToTarget);
            if (angle > DetectionAngle * 0.5f) 
                return false;
            
            return true;
        }
    }

    /// <summary>
    /// 战斗预警全局管理器
    /// 存储当前激活的攻击预警（来自怪物的动作时间轴），供玩家切换角色或闪避时进行检测
    /// </summary>
    public static class CombatWarningManager
    {
        private static readonly List<AttackWarningMarker> _activeMarkers = new List<AttackWarningMarker>();

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

        public static void Clear()
        {
            _activeMarkers.Clear();
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
            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker.Attacker == attacker)
                {
                    return marker;
                }
            }
            return null;
        }
    }
}
