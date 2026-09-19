namespace Game.GamePlay
{
    public enum BBKey
    {
        HasTarget,
        DistanceToTarget,
        AttackCooldownTimer,
        AttackIntervalTimer,
        NextActionEffectiveRange,
        HitTriggerTimestamp,
        InHitReaction,
        IsStunned,
        IsInRange,
        IsSelfControl
    }

    public static class BBKeyMapper
    {
        public static string GetString(BBKey key)
        {
            return key.ToString();
        }
    }
}
