using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "CharacterConfigAsset", menuName = "Config/Role/Character Config")]
    public class CharacterConfigAsset : GameConfigAsset
    {
        [Header("Base Info")]
        public int ID;
        public string Name;
        public GameObject Prefab;

        [Header("Ground Check")]
        public float GroundRadius = 0.3f;
        public float GroundHeight = 1.8f;
        public float GroundOffset = 0.1f;
        public LayerMask GroundLayer;

        [System.NonSerialized]
        protected List<ActionConfigAsset> _cachedActionConfigs;

        [Header("Hit Reaction")]
        public HitReactionConfig hitReactionConfig;


        public virtual IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (_cachedActionConfigs != null)
            {
                return _cachedActionConfigs;
            }

            HashSet<ActionConfigAsset> collectedActions = new();
            CollectActionConfigs(collectedActions);

            _cachedActionConfigs = new List<ActionConfigAsset>(collectedActions);
            return _cachedActionConfigs;
        }

        protected virtual void CollectActionConfigs(HashSet<ActionConfigAsset> collectedActions)
        {
            if (hitReactionConfig != null)
            {
                foreach (ActionConfigAsset hitActionConfig in hitReactionConfig.GetAllActionConfigs())
                {
                    CollectActionRecursive(hitActionConfig, collectedActions);
                }
            }
        }

        protected static void CollectActionRecursive(ActionConfigAsset action, HashSet<ActionConfigAsset> collectedActions)
        {
            if (action == null || !collectedActions.Add(action))
            {
                return;
            }

            if (action.CompleteAction != null)
            {
                CollectActionRecursive(action.CompleteAction, collectedActions);
            }

            List<ActionRoute> effectiveRoutes = new();
            action.CollectEffectiveRoutes(effectiveRoutes);
            foreach (ActionRoute route in effectiveRoutes)
            {
                if (route?.ExecuteAction != null)
                {
                    CollectActionRecursive(route.ExecuteAction, collectedActions);
                }
            }
        }
    }
}
