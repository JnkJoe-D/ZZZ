using System;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
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

    [Serializable]
    public sealed class TimeSinceActionStartCondition : ITransitionCondition
    {
        [Tooltip("Seconds since the current action started.")]
        public float Threshold = 0.2f;

        public ComparisonMode Mode = ComparisonMode.LessThan;

        public bool Check(CharacterEntity actor)
        {
            if (actor?.ActionPlayer == null)
            {
                return false;
            }

            float elapsed = Time.time - actor.ActionPlayer.ActionStartTime;
            return Mode == ComparisonMode.LessThan ? elapsed < Threshold : elapsed >= Threshold;
        }
    }

    /// <summary>
    /// 索敌范围内是否有目标。用于 ActionRoute.ExtraConditions。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("是否有锁定目标 (HasTarget)")]
    public sealed class HasTargetCondition : ITransitionCondition
    {
        [Tooltip("反转结果：勾选后变为'没有锁定目标时为真'。")]
        public bool Inverse;

        public bool Check(CharacterEntity actor)
        {
            // 如果实体没有 TargetFinder 组件，默认找不到
            if (actor?.TargetFinder == null)
            {
                return Inverse;
            }

            // TargetFinder 只要能拿到对象就算有 Target
            bool hasTarget = actor.TargetFinder.GetEnemy() != null;
            
            return Inverse ? !hasTarget : hasTarget;
        }
    }

}
