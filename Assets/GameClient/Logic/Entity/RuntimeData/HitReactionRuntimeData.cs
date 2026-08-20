using UnityEngine;

namespace Game.Logic
{
    public class HitReactionRuntimeData : IEntityRuntimeData
    {
        public float CurrentHitStunDuration { get; set; }
        public cfg.ZZZ.HitReactionType CurrentReactionType { get; set; }
        public Vector3 CurrentHitReactionAxis { get; private set; }
        public bool HasHitReactionAxis { get; private set; }

        public void SetHitReactionAxis(Vector3 axis)
        {
            axis.y = 0f;
            if (axis.sqrMagnitude <= 0.0001f)
            {
                ClearHitReactionAxis();
                return;
            }
            CurrentHitReactionAxis = axis.normalized;
            HasHitReactionAxis = true;
        }

        public void ClearHitReactionAxis()
        {
            CurrentHitReactionAxis = Vector3.zero;
            HasHitReactionAxis = false;
        }

        public void Reset()
        {
            CurrentHitStunDuration = 0f;
            ClearHitReactionAxis();
        }
    }
}
