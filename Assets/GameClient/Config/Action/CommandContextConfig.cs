using System;
using System.Collections.Generic;
using Game.Logic.Action.Combo;
using Game.Logic.Character;
using UnityEngine;

namespace Game.Logic.Action.Config
{
    [Serializable]
    public class CommandContextRouteGroup
    {
        [Header("Context")]
        public CommandContextType ContextType;

        [Header("Routes")]
        public List<ContextRoute> Routes = new();

        [Header("Route Sets")]
        public List<ContextRouteSetAsset> ContextRouteSets = new();

        public void AppendEffectiveRoutes(List<ContextRoute> results)
        {
            if (results == null)
            {
                return;
            }

            if (Routes != null)
            {
                foreach (ContextRoute route in Routes)
                {
                    if (route != null)
                    {
                        results.Add(route);
                    }
                }
            }

            if (ContextRouteSets == null)
            {
                return;
            }

            foreach (ContextRouteSetAsset routeSet in ContextRouteSets)
            {
                routeSet?.AppendRoutes(results);
            }
        }

        public IEnumerable<ActionConfigAsset> GetAllActions()
        {
            if (Routes != null)
            {
                foreach (ContextRoute route in Routes)
                {
                    if (route?.NextAction != null)
                    {
                        yield return route.NextAction;
                    }
                }
            }

            if (ContextRouteSets == null)
            {
                yield break;
            }

            foreach (ContextRouteSetAsset routeSet in ContextRouteSets)
            {
                if (routeSet == null)
                {
                    continue;
                }

                foreach (ActionConfigAsset action in routeSet.GetAllActions())
                {
                    if (action != null)
                    {
                        yield return action;
                    }
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "CommandContextConfig", menuName = "Config/Action/Command Context Config")]
    public class CommandContextConfig : ScriptableObject
    {
        [Header("Context Route Groups")]
        public List<CommandContextRouteGroup> ContextRouteGroups = new();

        public void CollectEffectiveRoutes(CommandContextType contextType, List<ContextRoute> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            if (ContextRouteGroups != null)
            {
                foreach (CommandContextRouteGroup group in ContextRouteGroups)
                {
                    if (group != null && group.ContextType == contextType)
                    {
                        group.AppendEffectiveRoutes(results);
                    }
                }
            }
        }

        public IEnumerable<ActionConfigAsset> GetAllActions()
        {
            if (ContextRouteGroups != null)
            {
                foreach (CommandContextRouteGroup group in ContextRouteGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    foreach (ActionConfigAsset action in group.GetAllActions())
                    {
                        if (action != null)
                        {
                            yield return action;
                        }
                    }
                }
            }
        }
    }
}
