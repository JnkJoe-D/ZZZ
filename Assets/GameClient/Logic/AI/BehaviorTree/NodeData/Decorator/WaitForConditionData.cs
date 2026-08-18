using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 条件等待节点数据。
    /// 对应 NPBehave.WaitForCondition —— 延迟执行子节点直到条件成立。
    /// 注意：具体的条件函数由 Translator 阶段根据上下文注入，这里只存轮询参数。
    /// </summary>
    [NodeColor("#88CC88")]
    public class WaitForConditionData : DecoratorData
    {
        [Tooltip("条件检查间隔（秒）。0 表示每帧检查。")]
        public float checkInterval = 0.0f;

        [Tooltip("检查间隔的随机浮动范围（秒）。")]
        public float randomVariance = 0.0f;
    }
}
