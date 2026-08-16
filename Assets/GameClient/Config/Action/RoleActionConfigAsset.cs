using System.Collections.Generic;
using UnityEngine;
using ATEditor;
using Game.Logic;

namespace Game.Logic
{
    /// <summary>
    /// 角色独有的动作配置（包含派生和连招路由）
    /// </summary>
    [CreateAssetMenu(fileName = "RoleActionConfigAsset", menuName = "Config/Action/Role Action Config")]
    public class RoleActionConfigAsset : ActionConfigAsset
    {
        // ── 派生与继承 ──────────────────────────────────────────
        [Header("派生与继承")]
        [Tooltip("当前动作对于前置动作派生路由的继承策略。")]
        public RouteInheritMode InheritMode = RouteInheritMode.None;

        [Header("专属派生路由 (可被继承)")]
        [Tooltip("当前动作专属的派生路由列表。如果后续动作设为继承，则会继承此列表中的路由。")]
        public List<ActionRoute> Routes = new();

        [Header("通用路由集 (不被继承)")]
        [Tooltip("通常用于配置闪避、移动等通用动作。后续动作即使设为继承，也不会继承此列表。")]
        public List<ActionRouteSetAsset> RouteSets = new();

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
