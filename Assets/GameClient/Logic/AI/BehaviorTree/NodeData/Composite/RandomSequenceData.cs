namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 随机序列节点数据。
    /// 对应 NPBehave.RandomSequence —— 随机顺序执行子节点，直到某个失败为止。
    /// 注意：中断规则仍基于编辑器中的原始顺序。
    /// </summary>
    [NodeColor("#66CC88")]
    public class RandomSequenceData : CompositeData
    {
    }
}
