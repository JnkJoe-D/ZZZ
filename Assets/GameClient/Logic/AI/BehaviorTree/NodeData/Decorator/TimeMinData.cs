using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 最小时间保证节点数据。
    /// 对应 NPBehave.TimeMin —— 确保子节点至少执行指定时间。
    /// 如果子节点提前完成，装饰器会等到最低时间后再上报结果。
    /// </summary>
    [NodeColor("#CC8866")]
    public class TimeMinData : DecoratorData
    {
        [Tooltip("最小执行时间（秒）。")]
        public float limit = 1.0f;

        [Tooltip("最小时间的随机浮动范围（秒）。")]
        public float randomVariation = 0.0f;

        [Tooltip("如果为 true，子节点失败时也等待最小时间；否则失败时立即上报。")]
        public bool waitOnFailure = false;
    }
}
