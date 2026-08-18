using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 等待直到停止节点数据。
    /// 对应 NPBehave.WaitUntilStopped —— 永远执行，直到被父节点或中断规则停止。
    /// </summary>
    [NodeColor("#AAAAAA")]
    public class WaitUntilStoppedData : TaskData
    {
        [Tooltip("被停止时，返回成功还是失败。")]
        public bool successWhenStopped = false;
    }
}
