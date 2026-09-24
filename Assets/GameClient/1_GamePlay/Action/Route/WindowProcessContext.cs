using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由窗口 Process 执行时的上下文依赖容器。
    /// 由 ActionController 在每次分发前创建并注入，窗口通过它与外部系统交互，
    /// 避免窗口直接耦合 ActionController 的内部实现。
    /// </summary>
    public sealed class WindowProcessContext
    {
        /// <summary> 当前动作所有有效路由（已展开 RouteSets）</summary>
        public IReadOnlyList<ActionRoute> Routes;

        /// <summary> 路由评估的行为体（可为 null，AutoTransition 等无需 actor 的路由允许 null）</summary>
        public CharacterEntity Actor;

        /// <summary> 技能消耗处理器（用于 ConsumeSkillCost 前置校验）</summary>
        public ISkillCostHandler SkillHandler;

        /// <summary> 路由仲裁器：用于 Submit 提交候选 </summary>
        public RouteArbitrator Arbitrator;

        /// <summary>
        /// 便捷封装：对指定 RouteWindow 和 timing 调用 RouteResolver.TryResolve。
        /// 窗口通过此方法做路由评估，不直接调用 RouteResolver。
        /// </summary>
        public bool TryResolve(
            CharacterCommand cmd,
            ATEditor.RouteWindow activeWindow,
            RouteSingleModifierCheckTiming timing,
            out RouteCandidate candidate)
        {
            return RouteResolver.TryResolve(
                Routes,
                cmd,
                activeWindow,
                Actor,
                SkillHandler,
                timing,
                out candidate);
        }
    }
}