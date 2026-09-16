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
        IsStunned
    }

    public static class BBKeyMapper
    {
        public static string GetString(BBKey key)
        {
            return key.ToString();
        }
    }
}
