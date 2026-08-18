namespace Game.Logic.AI.BehaviorTree
{
    public enum BBKey
    {
        HasTarget,
        TargetDistance,
        TargetDirection,
        IsDead,
        CurrentHP
    }

    public static class BBKeyMapper
    {
        public static string GetString(BBKey key)
        {
            return key.ToString();
        }
    }
}
