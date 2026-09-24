using System;

namespace Game.Framework
{
    /// <summary>
    /// 条件与触发器的适用实体领域标记。
    /// 用于区分通用规则、角色专属规则以及怪物专属规则。
    /// </summary>
    [Flags]
    public enum ConditionScope
    {
        None = 0,
        /// <summary>
        /// 通用领域：角色与怪物皆可使用（如目标距离、目标锁定、动作流逝时长等）
        /// </summary>
        Common = 1 << 0,
        /// <summary>
        /// 仅角色可用（如按键输入、输入摇杆、招架判定、战斗预警、极限视界、切人等）
        /// </summary>
        Role = 1 << 1,
        /// <summary>
        /// 仅怪物可用（如怪物韧性破防、受击反应、怪物攻击行为阶段等）
        /// </summary>
        Monster = 1 << 2,
        /// <summary>
        /// 所有领域
        /// </summary>
        All = Common | Role | Monster
    }

    /// <summary>
    /// 用于标记多态类型（如 ITransitionCondition 或 IRouteTrigger）的适用实体领域。
    /// 编辑器 SubclassSelectorDrawer 会依据当前配置资产宿主自动过滤下拉选项。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class ConditionScopeAttribute : Attribute
    {
        public ConditionScope Scope { get; }

        public ConditionScopeAttribute(ConditionScope scope)
        {
            Scope = scope;
        }
    }
}
