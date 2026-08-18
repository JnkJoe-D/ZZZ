using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 重复器节点数据。
    /// 对应 NPBehave.Repeater —— 重复执行子节点。
    /// </summary>
    [NodeColor("#8866CC")]
    public class RepeaterData : DecoratorData
    {
        [Tooltip("重复次数。-1 表示无限重复。0 表示不执行。")]
        public int loopCount = -1;
    }
}
