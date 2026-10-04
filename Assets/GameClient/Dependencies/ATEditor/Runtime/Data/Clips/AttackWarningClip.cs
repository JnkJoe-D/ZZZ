using System;
using UnityEngine;

namespace ATEditor
{
    public enum WarningSignalType
    {
        Yellow_Parryable, // 可招架（弹刀）
        Red_Unparryable   // 不可招架（仅限闪避）
    }

    public enum ParryWeight
    {
        Light = 1, // 轻招架（消耗 1 点支援点数，触发 ParryAid_L）
        Heavy = 2  // 重招架（消耗 2 点支援点数，触发 ParryAid_H）
    }

    [Serializable]
    [ClipDefinition(typeof(CombatTrack), "攻击预警")]
    public class AttackWarningClip : ClipBase
    {
        [Header("Warning Type")]
        [Tooltip("黄光表示可被弹刀/招架，红光表示不可弹刀只能闪避")]
        public WarningSignalType SignalType = WarningSignalType.Yellow_Parryable;

        [Tooltip("招架强度等级：决定防守方消耗 1 点还是 2 点支援点数，并决定进入轻招架还是重招架动作")]
        public ParryWeight ParryWeight = ParryWeight.Heavy;

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

        [Tooltip("贴脸禁区半径（米）。若玩家与怪物距离小于此半径，禁止就地招架，强制瞬移至 ClashPositionOffset，避免模型穿插或打击盒扫空")]
        public float RestrictedInnerRadius = 1.2f;

        [Tooltip("原地招架连线横向容差（米）。玩家偏离怪物与预设接刀点连线的横向距离小于此容差，才允许就地招架。")]
        public float InPlaceLineTolerance = 0.5f;

        [Header("招架分期 (提前预判 vs 临界直接接刀)")]
        [Tooltip("直接切入招架的相对时间偏移（单位：秒）。\n" +
                 "• 若为 0 (默认)：整段预警区间内按键均直接切入招架轻/重 (ParryAid)；\n" +
                 "• 若 > 0：在预警开始后的 [0, Offset) 秒内按键进入提前架势 (ParryAidStart)；在 Offset 之后按键直接切入招架轻/重 (ParryAid)。")]
        public float DirectClashTimeOffset = 0.0f;

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
                ParryWeight = this.ParryWeight,
                CoverageShape = this.CoverageShape?.Clone() ?? new HitBoxShape { shapeType = HitBoxType.Sector, radius = 5.0f, angle = 120.0f, height = 2.5f },
                CoverageCenterOffset = this.CoverageCenterOffset,
                ClashPositionOffset = this.ClashPositionOffset,
                AllowInPlaceParry = this.AllowInPlaceParry,
                RestrictedInnerRadius = this.RestrictedInnerRadius,
                InPlaceLineTolerance = this.InPlaceLineTolerance,
                DirectClashTimeOffset = this.DirectClashTimeOffset,
                ShowGizmos = this.ShowGizmos,
                DetectionRadius = this.DetectionRadius,
                DetectionAngle = this.DetectionAngle
            };
        }
    }
}
