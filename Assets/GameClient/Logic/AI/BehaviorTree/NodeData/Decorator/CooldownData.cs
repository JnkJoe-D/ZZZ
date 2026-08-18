using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 冷却节点数据。
    /// 对应 NPBehave.Cooldown —— 确保子节点在冷却时间内不会被重复执行。
    /// </summary>
    [NodeColor("#CC9944")]
    public class CooldownData : DecoratorData
    {
        [Tooltip("冷却时间（秒）。")]
        public float cooldownTime = 1.0f;

        [Tooltip("冷却时间的随机浮动范围（秒）。")]
        public float randomVariation = 0.1f;

        [Tooltip("如果为 true，冷却计时从子节点结束后开始；否则从子节点开始时计时。")]
        public bool startAfterDecoratee = false;

        [Tooltip("如果为 true，子节点失败时重置冷却。")]
        public bool resetOnFailure = false;

        [Tooltip("如果为 true，冷却期间直接返回失败，而非等待冷却结束。")]
        public bool failOnCooldown = false;
    }
}
