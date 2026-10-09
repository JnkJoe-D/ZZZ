using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public enum TapTimeWindowMode
    {
        [InspectorName("最近滑动时间窗口 (秒)")]
        SlidingWindow = 0,

        [InspectorName("当前动作播放至今 (Action Start)")]
        CurrentActionLifetime = 10,
    }

    /// <summary>
    /// 按键连续点击次数判定条件
    /// 用于检测玩家在指定时间窗口内的点击频次（例如：连击次数 >= 2，或未连续点击 <= 0）
    /// </summary>
    [Serializable]
    [SubclassDisplayName("按键连续点击次数条件")]
    public sealed class KeyTapCountCondition : IKeyInputCondition
    {
        [Tooltip("目标检测按键 (如普攻 BasicAttack)")]
        public HardwareInputType TargetKey = HardwareInputType.BasicAttack;

        [Tooltip("时间窗口模式：SlidingWindow 为最近 N 秒滑动窗口；CurrentActionLifetime 为自当前动作起手以来的累计点击")]
        public TapTimeWindowMode WindowMode = TapTimeWindowMode.SlidingWindow;

        [Tooltip("滑动时间窗口时长 (秒)，仅在 SlidingWindow 模式下生效。例如 0.3s 内点击统计")]
        [ShowIf("WindowMode", TapTimeWindowMode.SlidingWindow)]
        [Range(0.05f, 2.0f)]
        public float WindowDuration = 0.3f;

        [Tooltip("比较运算符")]
        public ConditionCompareOperator Operator = ConditionCompareOperator.LessOrEqual;

        [Tooltip("目标点击次数阈值")]
        public int ThresholdCount = 0;
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            var provider = actor.InputProvider;
            if (provider == null) return false;

            int actualTaps = 0;
            if (WindowMode == TapTimeWindowMode.SlidingWindow)
            {
                actualTaps = provider.GetTapCount(TargetKey, WindowDuration);
            }
            else if (WindowMode == TapTimeWindowMode.CurrentActionLifetime)
            {
                float startTime = actor.ActionPlayer != null ? actor.ActionPlayer.ActionStartTime : 0f;
                actualTaps = provider.GetTapCountSince(TargetKey, startTime);
            }

            bool matched = Compare(actualTaps, Operator, ThresholdCount);
            return matched;
        }

        private static bool Compare(int actual, ConditionCompareOperator op, int target)
        {
            switch (op)
            {
                case ConditionCompareOperator.Equal: return actual == target;
                case ConditionCompareOperator.NotEqual: return actual != target;
                case ConditionCompareOperator.LessThan: return actual < target;
                case ConditionCompareOperator.LessOrEqual: return actual <= target;
                case ConditionCompareOperator.GreaterThan: return actual > target;
                case ConditionCompareOperator.GreaterOrEqual: return actual >= target;
                default: return false;
            }
        }
    }
}
