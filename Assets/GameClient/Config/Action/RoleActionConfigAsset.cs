using System.Collections.Generic;
using UnityEngine;
using ATEditor;
using Game.Logic;

namespace Game.Logic
{
    /// <summary>
    /// 动作所处的状态机环境。每个 RoleActionConfigAsset 必须显式声明。
    /// </summary>
    public enum ActionState
    {
        Idle = 0,       // GroundState → Idle 子状态
        Jog = 10,       // GroundState → Jog 子状态
        Dash = 20,      // GroundState → Dash 子状态
        Stop = 30,      // GroundState → Stop 子状态
        Skill = 40,     // CharacterSkillState
        Evade = 50,     // CharacterEvadeState
        Hit = 60,       // CharacterHitStunState
        Switch = 70,    // CharacterSwitchState
        Parry = 80,     // CharacterParryState
    }

    /// <summary>
    /// 角色独有的动作配置（包含派生和连招路由）
    /// </summary>
    [CreateAssetMenu(fileName = "RoleActionConfigAsset", menuName = "Config/Action/Role Action Config")]
    public class RoleActionConfigAsset : ActionConfigAsset
    {
        [Header("状态转换")]
        [Tooltip("指定此动作执行时的目标状态机主状态/子状态。")]
        public ActionState EnterState = ActionState.Idle;

        // ── 派生与继承 ──────────────────────────────────────────
        [Header("派生与继承")]
        [Tooltip("当前动作对于前置动作派生路由的继承策略。")]
        public RouteInheritMode InheritMode = RouteInheritMode.None;

        // Routes 和 RouteSets 已迁移至基类 ActionConfigAsset

        /// <summary>
        /// 收集此动作上所有有效的统一路由（展开集合资产）。
        /// </summary>
        public override void CollectEffectiveRoutes(List<ActionRoute> results, RoleEntity actor = null)
        {
            if (results == null) return;
            results.Clear();

            bool shouldInherit = InheritMode != RouteInheritMode.None;
            bool overrideSelf = InheritMode == RouteInheritMode.InheritAndOverrideSelf;
            bool inheritedFirst = InheritMode == RouteInheritMode.InheritPrioritizeInherited;

            List<ActionRoute> selfRoutes = new List<ActionRoute>();
            if (!overrideSelf && Routes != null)
            {
                foreach (var route in Routes)
                {
                    if (route != null) selfRoutes.Add(route);
                }
            }

            List<ActionRoute> inheritedRoutes = new List<ActionRoute>();
            if (shouldInherit && actor?.ActionController != null)
            {
                var history = actor.ActionController.ExecutionHistory;
                if (history != null && history.Count >= 2)
                {
                    var prevAction = history[1].Asset as RoleActionConfigAsset;
                    if (prevAction != null && prevAction.Routes != null)
                    {
                        foreach (var route in prevAction.Routes)
                        {
                            if (route != null) 
                            {
                                // 防止继承来的路由指向自己从而引发死循环
                                if (route.ExecuteType == ExecuteTarget.Action && route.ExecuteAction != null && route.ExecuteAction.ID == this.ID)
                                {
                                    continue;
                                }
                                inheritedRoutes.Add(route);
                            }
                        }
                    }
                }
            }

            // 按优先级顺序添加
            if (inheritedFirst)
            {
                results.AddRange(inheritedRoutes);
                results.AddRange(selfRoutes);
            }
            else
            {
                results.AddRange(selfRoutes);
                results.AddRange(inheritedRoutes);
            }

            // 3. 收集自身的通用路由集
            if (RouteSets != null)
            {
                foreach (var routeSet in RouteSets)
                {
                    routeSet?.AppendRoutes(results);
                }
            }
        }
    }
}
