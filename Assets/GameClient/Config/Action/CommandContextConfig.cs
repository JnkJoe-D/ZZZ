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
    }

    [CreateAssetMenu(fileName = "CommandContextConfig", menuName = "Config/Action/Command Context Config")]
    public class CommandContextConfig : ScriptableObject
    {
        [Header("Context Route Groups")]
        public List<CommandContextRouteGroup> ContextRouteGroups = new();

        public List<ContextRoute> GetRoutes(CommandContextType contextType)
        {
            if (ContextRouteGroups == null)
            {
                return null;
            }

            foreach (CommandContextRouteGroup group in ContextRouteGroups)
            {
                if (group != null && group.ContextType == contextType)
                {
                    return group.Routes;
                }
            }

            return null;
        }

        public IEnumerable<ActionConfigAsset> GetAllActions()
        {
            if (ContextRouteGroups == null)
            {
                yield break;
            }

            foreach (CommandContextRouteGroup group in ContextRouteGroups)
            {
                if (group?.Routes == null)
                {
                    continue;
                }

                foreach (ContextRoute route in group.Routes)
                {
                    if (route?.NextAction != null)
                    {
                        yield return route.NextAction;
                    }
                }
            }
        }
    }
}
