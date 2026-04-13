using System;
using Game.Logic.Action.Config;
using Game.Logic.Character;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    [Serializable]
    public sealed class HasMovementInputCondition : ITransitionCondition
    {
        public bool Expected = true;

        public bool Check(CharacterEntity actor)
        {
            bool hasMovementInput = actor?.InputProvider != null && actor.InputProvider.HasMovementInput();
            return hasMovementInput == Expected;
        }
    }



    public enum ComparisonMode
    {
        LessThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// 检查当前动作已播放的时间是否满足阈值条件。
    /// 用于替代状态机中的短按检测逻辑（如 JogStart 的 0.2s 短按判定）。
    /// </summary>
    [Serializable]
    public sealed class TimeSinceActionStartCondition : ITransitionCondition
    {
        [Tooltip("时间阈值（秒）。")]
        public float Threshold = 0.2f;

        [Tooltip("比较模式：LessThan = 动作开始不到 Threshold 秒，GreaterThanOrEqual = 已超过 Threshold 秒。")]
        public ComparisonMode Mode = ComparisonMode.LessThan;

        public bool Check(CharacterEntity actor)
        {
            if (actor?.ActionPlayer == null) return false;
            float elapsed = Time.time - actor.ActionPlayer.ActionStartTime;
            return Mode == ComparisonMode.LessThan
                ? elapsed < Threshold
                : elapsed >= Threshold;
        }
    }
}

