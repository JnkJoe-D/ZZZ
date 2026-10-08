using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 投射物/生成物运动轨迹类型
    /// </summary>
    public enum ProjectileMoveMode
    {
        Static,             // 原地静止 (陷阱/地雷/原地法阵)
        StraightLine,       // 直线运动 (匀速/直线加减速)
        TargetTracking,     // 目标追踪 / 寻的 (跟踪导弹/飞剑)
        Parabola,           // 物理抛物线 (手雷/投掷物)
        CustomCurve         // 轨迹曲线驱动
    }

    /// <summary>
    /// 生成物移动与动力学参数块
    /// </summary>
    [Serializable]
    public class ProjectileMovementConfig
    {
        [ActionProperty("运动模式")]
        public ProjectileMoveMode moveMode = ProjectileMoveMode.StraightLine;

        [ActionProperty("初始线速度 (m/s)")]
        [ATShowIf("moveMode", ProjectileMoveMode.Static, false)]
        public float initialSpeed = 15f;

        [ActionProperty("加速度 (m/s²)")]
        [ATShowIf("moveMode", ProjectileMoveMode.StraightLine)]
        public float acceleration = 0f;

        [ActionProperty("最大线速度 (m/s)")]
        [ATShowIf("moveMode", ProjectileMoveMode.StraightLine)]
        public float maxSpeed = 30f;

        [ActionProperty("追踪角速度 (度/秒)")]
        [ATShowIf("moveMode", ProjectileMoveMode.TargetTracking)]
        public float turnSpeed = 180f;

        [ActionProperty("重力系数")]
        [ATShowIf("moveMode", ProjectileMoveMode.Parabola)]
        public float gravityScale = 1.0f;

        [ActionProperty("朝向随速度对齐")]
        public bool orientToVelocity = true;

        [ActionProperty("最大位移距离 (米)")]
        [Tooltip("<= 0 表示不限制距离")]
        public float maxDistance = 50f;

        public ProjectileMovementConfig Clone()
        {
            return new ProjectileMovementConfig
            {
                moveMode = this.moveMode,
                initialSpeed = this.initialSpeed,
                acceleration = this.acceleration,
                maxSpeed = this.maxSpeed,
                turnSpeed = this.turnSpeed,
                gravityScale = this.gravityScale,
                orientToVelocity = this.orientToVelocity,
                maxDistance = this.maxDistance
            };
        }
    }
}
