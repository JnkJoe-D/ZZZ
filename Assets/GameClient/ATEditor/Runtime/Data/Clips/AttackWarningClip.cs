using System;
using UnityEngine;

namespace ATEditor
{
    public enum WarningSignalType
    {
        Yellow_Parryable, // 可招架（弹刀）
        Red_Unparryable   // 不可招架（仅限闪避）
    }

    [Serializable]
    [ClipDefinition(typeof(EventTrack), "攻击预警 (黄/红光)")]
    public class AttackWarningClip : ClipBase
    {
        [Header("Warning Type")]
        [Tooltip("黄光表示可被弹刀/招架，红光表示不可弹刀只能闪避")]
        public WarningSignalType SignalType = WarningSignalType.Yellow_Parryable;

        [Header("Coverage Area (威胁覆盖域)")]
        [Tooltip("用于判定玩家是否已经处于本次攻击危险区内。若在区域内则就地招架；若在外部则瞬移至接刀点。")]
        public HitBoxShape CoverageShape = new HitBoxShape
        {
            shapeType = HitBoxType.Sector,
            radius = 5.0f,
            angle = 120.0f,
            height = 2.5f
        };

        [Tooltip("覆盖区域中心相对怪物的局部坐标偏移")]
        public Vector3 CoverageCenterOffset = Vector3.zero;

        [Header("Clash Position (招架接刀身位)")]
        [Tooltip("当玩家在覆盖域外部需要切入时，目标切入身位（相对于怪物的局部坐标）。可在 SceneView 视口中自由拖拽 Gizmo 手柄微调")]
        public Vector3 ClashPositionOffset = new Vector3(0f, 0f, 1.8f);

        [Tooltip("是否允许就地招架。若勾选且玩家已在覆盖域内，切入时不发生位移，仅瞬间转向面向怪物")]
        public bool AllowInPlaceParry = true;

        [Header("Editor Gizmos")]
        [Tooltip("是否在 Scene 窗口绘制预警覆盖盒与接刀身位标记")]
        public bool ShowGizmos = true;

        [Header("Legacy Compatibility")]
        [Tooltip("检测半径（通常为攻击的最远距离）")]
        public float DetectionRadius = 5.0f;

        [Tooltip("检测角度（扇形夹角，前方为0度）")]
        public float DetectionAngle = 180.0f;

        public override ClipBase Clone()
        {
            return new AttackWarningClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                SignalType = this.SignalType,
                CoverageShape = this.CoverageShape?.Clone() ?? new HitBoxShape { shapeType = HitBoxType.Sector, radius = 5.0f, angle = 120.0f, height = 2.5f },
                CoverageCenterOffset = this.CoverageCenterOffset,
                ClashPositionOffset = this.ClashPositionOffset,
                AllowInPlaceParry = this.AllowInPlaceParry,
                ShowGizmos = this.ShowGizmos,
                DetectionRadius = this.DetectionRadius,
                DetectionAngle = this.DetectionAngle
            };
        }
    }
}
