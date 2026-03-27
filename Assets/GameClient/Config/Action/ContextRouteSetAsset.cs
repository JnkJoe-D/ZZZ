using System.Collections.Generic;
using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Action.Config
{
    [CreateAssetMenu(fileName = "ContextRouteSetAsset", menuName = "Config/Action/Context Route Set")]
    public class ContextRouteSetAsset : ScriptableObject
    {
        [Header("Routes")]
        public List<ContextRoute> Routes = new();

        public void AppendRoutes(List<ContextRoute> results)
        {
            if (results == null || Routes == null)
            {
                return;
            }

            foreach (ContextRoute route in Routes)
            {
                if (route != null)
                {
                    results.Add(route);
                }
            }
        }

        public IEnumerable<ActionConfigAsset> GetAllActions()
        {
            if (Routes == null)
            {
                yield break;
            }

            foreach (ContextRoute route in Routes)
            {
                if (route?.NextAction != null)
                {
                    yield return route.NextAction;
                }
            }
        }
    }
}
