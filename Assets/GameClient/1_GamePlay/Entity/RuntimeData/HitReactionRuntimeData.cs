using UnityEngine;

namespace Game.GamePlay
{
    public class HitReactionRuntimeData : EntityRuntimeDataBase
    {
        public bool InHitReaction => Get<bool>(nameof(InHitReaction));
        public float CurrentHitStunDuration => Get<float>(nameof(CurrentHitStunDuration));
        public cfg.ZZZ.HitReactionType CurrentReactionType => Get<cfg.ZZZ.HitReactionType>(nameof(CurrentReactionType));
        public ActionConfigAsset ResolvedHitAction => Get<ActionConfigAsset>(nameof(ResolvedHitAction));
        public bool RequireFaceAttacker => Get<bool>(nameof(RequireFaceAttacker));
        public int HitTriggerTimestamp => Get<int>(nameof(HitTriggerTimestamp), -1);
        public int HitSequenceId => Get<int>(nameof(HitSequenceId), 0);

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

        public override void Reset()
        {
            base.Reset();
            ClearHitReactionAxis();
        }
    }
}
