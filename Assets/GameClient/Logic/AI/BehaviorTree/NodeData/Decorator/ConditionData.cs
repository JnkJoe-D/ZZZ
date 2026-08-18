using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 条件节点数据。
    /// 对应 NPBehave.Condition —— 基于一个 Func<bool> 条件决定执行还是失败。
    /// </summary>
    [NodeColor("#55AA88")]
    public class ConditionData : DecoratorData
    {
        [Tooltip("条件变化时的中断策略。")]
        public NPBehave.Stops stopsOnChange = NPBehave.Stops.NONE;

        [Tooltip("检查间隔（秒）。0 表示每帧检查。")]
        public float checkInterval = 0.0f;

        [Tooltip("检查间隔的随机浮动范围（秒）。")]
        public float checkVariance = 0.0f;
    }
}
