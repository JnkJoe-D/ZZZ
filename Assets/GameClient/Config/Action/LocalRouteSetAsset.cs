using System.Collections.Generic;
using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Action.Config
{
    [CreateAssetMenu(fileName = "LocalRouteSetAsset", menuName = "Config/Action/Local Route Set")]
    public class LocalRouteSetAsset : ScriptableObject
    {
        [Header("Routes")]
        public List<LocalActionRoute> Routes = new();

        public void AppendRoutes(List<LocalActionRoute> results)
        {
            if (results == null || Routes == null)
            {
                return;
            }

            foreach (LocalActionRoute route in Routes)
            {
                if (route != null)
                {
                    results.Add(route);
                }
            }
        }
    }
}
