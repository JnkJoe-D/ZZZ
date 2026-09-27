using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 旋转轴向过滤掩码配置
    /// </summary>
    [Serializable]
    public struct RotationAxisMask
    {
        [Tooltip("过滤 X 轴（Pitch 俯仰）。勾选后消除上下俯仰旋转，通常保持勾选")]
        public bool FilterX;

        [Tooltip("过滤 Y 轴（Yaw 偏航）。勾选后完全禁止水平旋转")]
        public bool FilterY;

        [Tooltip("过滤 Z 轴（Roll 翻滚）。勾选后消除左右倾斜，通常保持勾选")]
        public bool FilterZ;

        public static RotationAxisMask DefaultTurnMask => new RotationAxisMask
        {
            FilterX = true,
            FilterY = false,
            FilterZ = true
        };

        /// <summary>
        /// 是否全轴向过滤（全开）。当全部勾选时，彻底禁止一切根旋转、角度累积以及退出吸附对齐。
        /// </summary>
        public bool IsAllAxesFiltered => FilterX && FilterY && FilterZ;
    }

    /// <summary>
    /// 退出时刻旋转对齐模式
    /// </summary>
    public enum RotationExitAlignMode
    {
        [InspectorName("不进行退出对齐 (None)")]
        None = 0,

        [InspectorName("相对进入朝向固定角度对齐 (SnapToRelativeTarget)")]
        SnapToRelativeTarget = 1,

        [InspectorName("对齐到当前摇杆/按键输入方向 (SnapToInputDirection)")]
        SnapToInputDirection = 2,
    }

    /// <summary>
    /// 动画位移轨道上的根旋转过滤窗口片段
    /// 解决如 TurnBack 等动作中后半段步频晃动污染物理根朝向的问题，
    /// 提供轴向过滤、最大累计角截断、单向锁定与退出精准对齐。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "动画旋转过滤窗口")]
    public class RotationFilterClip : ClipBase
    {
        [ActionProperty("轴向过滤")]
        public RotationAxisMask axisMask = RotationAxisMask.DefaultTurnMask;

        [ActionProperty("启用最大角度限制")]
        public bool enableMaxAngleLimit = true;

        [ActionProperty("最大累计旋转角度")]
        [Range(0f, 360f)]
        public float maxAccumulatedAngle = 180f;

        [ActionProperty("单向单调锁定")]
        public bool lockToSingleDirection = true;

        [ActionProperty("退出时对齐模式")]
        public RotationExitAlignMode exitAlignMode = RotationExitAlignMode.SnapToRelativeTarget;

        [ActionProperty("目标相对偏航角")]
        public float targetRelativeYaw = 180f;

        [ActionProperty("对齐平滑时长")]
        [Range(0f, 0.2f)]
        public float alignBlendDuration = 0.05f;

        public RotationFilterClip()
        {
            clipName = "动画旋转过滤窗口";
            duration = 0.3f;
        }

        public override ClipBase Clone()
        {
            return new RotationFilterClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                axisMask = axisMask,
                enableMaxAngleLimit = enableMaxAngleLimit,
                maxAccumulatedAngle = maxAccumulatedAngle,
                lockToSingleDirection = lockToSingleDirection,
                exitAlignMode = exitAlignMode,
                targetRelativeYaw = targetRelativeYaw,
                alignBlendDuration = alignBlendDuration
            };
        }
    }
}
