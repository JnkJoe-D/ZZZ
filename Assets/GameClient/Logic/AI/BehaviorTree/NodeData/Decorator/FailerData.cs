namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 失败器节点数据。
    /// 对应 NPBehave.Failer —— 无论子节点结果如何，始终返回失败。
    /// </summary>
    [NodeColor("#CC4444")]
    public class FailerData : DecoratorData
    {
    }
}
