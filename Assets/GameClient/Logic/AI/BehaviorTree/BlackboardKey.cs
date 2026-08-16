namespace Game.Logic.AI.BehaviorTree
{
    public enum BlackboardKey
    {
        HasTarget,
        Distance,
        Target,
        SelectedAttackAction,
        Context,
        
        // 基于距离的抽象状态，供节点判断
        IsDistanceWithinAttackRange,
        IsDistanceGreaterThanAttackRange,
        IsDistanceWithinPursuitRadius,
        IsDistanceGreaterThanPursuitRadius,
        
        // 基于时间的抽象状态
        IsAttackCDReady
    }
}
