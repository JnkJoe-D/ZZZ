using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 宏观战术战略枚举
    /// </summary>
    public enum MonsterStrategy
    {
        Idle,
        Approach,
        Strafe,
        Attack
    }

    /// <summary>
    /// 怪物战术上下文：行为树（宏观）与状态机（微观）之间的纯数据解耦桥梁。
    /// 严格遵循单向信号流：
    /// 1. 行为树向 Context 写入 Strategy / TargetRadius / PendingAttack；
    /// 2. 状态机读取 Context 指引微观执行；
    /// 3. 状态机向 Context 写回 IsInRange 事实；
    /// 4. 行为树下一帧读取事实判定任务结束。
    /// </summary>
    public class MonsterTacticalContext
    {
        // === 行为树写入 -> 状态机读取 ===
        public MonsterStrategy Strategy { get; set; } = MonsterStrategy.Idle;
        public float TargetRadius { get; set; } = 3.5f;
        public ActionConfigAsset PendingAttack { get; set; }

        // === 状态机写入 -> 行为树读取 ===
        public bool IsInRange { get; set; }

        // === 共享静态资产引用（缓存加速，避免重复装箱） ===
        public MonsterLocomotionConfig LocomotionConfig { get; set; }

        /// <summary>
        /// 消费待执行的攻击动作（取走并置空，杜绝重复触发出刀）
        /// </summary>
        public ActionConfigAsset ConsumePendingAttack()
        {
            var attack = PendingAttack;
            PendingAttack = null;
            return attack;
        }

        /// <summary>
        /// 重置战术上下文（受击打断、实体回池或重新激活时调用）
        /// </summary>
        public void Reset()
        {
            Strategy = MonsterStrategy.Idle;
            TargetRadius = 3.5f;
            PendingAttack = null;
            IsInRange = false;
        }
    }
}
