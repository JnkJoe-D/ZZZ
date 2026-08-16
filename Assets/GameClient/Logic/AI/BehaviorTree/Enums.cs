namespace Game.Logic.AI.BehaviorTree
{
    public enum NodeState
    {
        Inactive,
        Active,
        StopRequested
    }

    public enum Stops
    {
        None,
        Self,
        LowerPriority,
        ImmediateRestart,
        Both = ImmediateRestart
    }

    public enum Operator
    {
        IsEqual,
        IsNotEqual,
        IsGreaterOrEqual,
        IsGreater,
        IsSmallerOrEqual,
        IsSmaller,
        AlwaysTrue
    }
}
