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

}
