using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 可复用的 ActionRoute 集合 ScriptableObject。
    /// 用于在多个动作间共享相同的路由配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ActionRouteSetAsset", menuName = "Config/Action/Action Route Set")]
    public class ActionRouteSetAsset : ScriptableObject
    {
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
