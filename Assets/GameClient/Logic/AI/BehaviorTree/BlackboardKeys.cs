namespace Game.Logic.AI.BehaviorTree
{
    public enum BBKey
    {
        HasTarget,
        DistanceToTarget,
        AttackCooldownTimer
    }

    public static class BBKeyMapper
    {
        public static string GetString(BBKey key)
        {
            return key.ToString();
        }
    }
}
