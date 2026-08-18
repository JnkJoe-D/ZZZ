namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 动作节点数据。
    /// 对应 NPBehave.Action —— 执行具体游戏逻辑的叶子节点。
    /// 这里定义为一个基类，实际的动作（如 MoveTo, PlayAnim）将继承此类。
    /// </summary>
    [NodeColor("#44CC44")]
    public class ActionData : TaskData
    {
        // 游戏特定的 Action 数据参数放在其子类中
    }
}
