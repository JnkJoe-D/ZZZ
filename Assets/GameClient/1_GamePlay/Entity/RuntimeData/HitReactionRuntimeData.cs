using UnityEngine;

namespace Game.GamePlay
{
    public class HitReactionRuntimeData : IEntityRuntimeData
    {
        public float CurrentHitStunDuration { get; set; }
        public cfg.ZZZ.HitReactionType CurrentReactionType { get; set; }
        public ActionConfigAsset ResolvedHitAction { get; set; }
        public bool RequireFaceAttacker { get; set; }
        public int HitTriggerTimestamp { get; set; } = -1;
        public Vector3 CurrentHitReactionAxis { get; private set; }
        public bool HasHitReactionAxis { get; private set; }

        public bool InHitReaction { get; set; }
        public int HitSequenceId { get; set; }

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
            ResolvedHitAction = null;
            RequireFaceAttacker = false;
            InHitReaction = false;
            HitSequenceId = 0;
            ClearHitReactionAxis();
        }
    }
}
