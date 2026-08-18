using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 等待节点数据。
    /// 对应 NPBehave.Wait —— 等待指定时间后返回成功。
    /// </summary>
    [NodeColor("#AAAA44")]
    public class WaitData : TaskData
    {
        [Tooltip("等待时间（秒）。-1 表示从黑板读取。")]
        public float seconds = 1.0f;

        [Tooltip("等待时间的随机浮动范围（秒）。")]
        public float randomVariance = 0.05f;

        [Tooltip("如果需要从黑板动态读取等待时间，勾选此项。")]
        public bool readFromBlackboard = false;
        public string blackboardKey;
    }
}
