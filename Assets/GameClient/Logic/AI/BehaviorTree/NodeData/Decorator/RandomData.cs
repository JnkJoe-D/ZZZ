using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 随机执行节点数据。
    /// 对应 NPBehave.Random —— 以给定概率决定是否执行子节点。
    /// </summary>
    [NodeColor("#AA88CC")]
    public class RandomData : DecoratorData
    {
        [Range(0f, 1f)]
        [Tooltip("执行子节点的概率（0~1）。")]
        public float probability = 0.5f;
    }
}
