namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 并行节点数据。
    /// 对应 NPBehave.Parallel —— 同时执行所有子节点，根据策略决定成功/失败。
    /// </summary>
    [NodeColor("#CC8844")]
    public class ParallelData : CompositeData
    {
        /// <summary>
        /// 成功策略：ONE = 任一子节点成功即成功；ALL = 所有子节点成功才成功。
        /// </summary>
        public NPBehave.Parallel.Policy successPolicy = NPBehave.Parallel.Policy.ALL;

        /// <summary>
        /// 失败策略：ONE = 任一子节点失败即失败；ALL = 所有子节点失败才失败。
        /// </summary>
        public NPBehave.Parallel.Policy failurePolicy = NPBehave.Parallel.Policy.ONE;
    }
}
