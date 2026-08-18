using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 最大时间限制节点数据。
    /// 对应 NPBehave.TimeMax —— 如果子节点在限定时间内未完成，则失败。
    /// </summary>
    [NodeColor("#CC6644")]
    public class TimeMaxData : DecoratorData
    {
        [Tooltip("时间限制（秒）。")]
        public float limit = 5.0f;

        [Tooltip("时间限制的随机浮动范围（秒）。")]
        public float randomVariation = 0.0f;

        [Tooltip("如果为 true，即使超时也等待子节点完成，但最终仍返回失败。")]
        public bool waitForChildButFailOnLimitReached = false;
    }
}
