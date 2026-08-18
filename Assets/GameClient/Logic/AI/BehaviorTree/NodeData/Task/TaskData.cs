namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 叶节点（任务）数据基类。
    /// 对应 NPBehave.Task —— 没有子节点的叶子节点。
    /// </summary>
    public abstract class TaskData : NodeData
    {
        // Task 节点无子节点，基类的默认实现已足够
    }
}
