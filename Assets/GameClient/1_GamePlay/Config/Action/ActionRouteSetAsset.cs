using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
namespace Game.GamePlay
{
    /// <summary>
    /// 可复用的 ActionRoute 集合 ScriptableObject。
    /// 用于在多个动作间共享相同的路由配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ActionRouteSetAsset", menuName = "Config/Action/Action Route Set")]
    public class ActionRouteSetAsset : GameConfigAsset
    {
        [Header("适用实体领域")]
        [Tooltip("指定本通用路由集的适用实体领域。用于在 Inspector 中智能过滤条件和触发器。")]
        public ConditionScope TargetScope = ConditionScope.Role;

        [Header("Routes")]
        public List<ActionRoute> Routes = new();

        /// <summary>
        /// 将此集合中的路由附加到结果列表。
        /// </summary>
        public void AppendRoutes(List<ActionRoute> results)
        {
            if (results == null || Routes == null) return;

            foreach (var route in Routes)
            {
                if (route != null)
                {
                    results.Add(route);
                }
            }
        }
    }
}
