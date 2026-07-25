using System.Collections.Generic;
using Game.AI;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "CharacterConfigAsset", menuName = "Config/Role/Character Config")]
    public class CharacterConfigAsset : GameConfigAsset
    {
        [Header("Base Info")]
        public int RoleID;
        public string RoleName;
        public GameObject CharacterPrefab;

        [Header("Ground Check")]
        public float GroundRadius = 0.3f;
        public float GroundHeight = 1.8f;
        public float GroundOffset = 0.1f;
        public LayerMask GroundLayer;

        [Header("Movement Speed Multipliers")]
        [Range(0f, 5f)]
        public float JogMultipier = 1f;

        [Header("Ground Jog")]
        public float JogShortInputThreshold = 0.2f;

        [Range(0f, 5f)]
        public float DashMultipier = 1f;

        [Range(0f, 5f)]
        public float DodgeMultipier = 1f;

        [Header("Attack Speed Multiplier")]
        [Range(0f, 5f)]
        public float AttackMultipier = 1f;

        [Header("Skill Speed Multiplier")]
        [Range(0f, 5f)]
        public float SkillMultipier = 1f;

        [Header("Evade")]
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("Root Action")]
        public ActionConfigAsset ActionRoot;

        [Header("Action Preload List")]
        public List<ActionConfigAsset> ActionProLoadList = new();

        [Header("AI")]
        public BehaviorTreeGraphAsset BehaviorTreeGraph;

        [Header("Hit Reaction")]
        public HitReactionConfig hitReactionConfig;

        [Header("Status")]
        [Tooltip("角色状态配置（属性、初始 Buff、免疫标签）。为空时角色无属性系统。")]
        public StatusProfile StatusProfile;

        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            HashSet<ActionConfigAsset> collectedActions = new();

            CollectActionRecursive(ActionRoot, collectedActions);

            if (ActionProLoadList != null)
            {
                foreach (ActionConfigAsset action in ActionProLoadList)
                {
                    CollectActionRecursive(action, collectedActions);
                }
            }

            if (hitReactionConfig != null)
            {
                foreach (ActionConfigAsset hitActionConfig in hitReactionConfig.GetAllActionConfigs())
                {
                    CollectActionRecursive(hitActionConfig, collectedActions);
                }
            }

            foreach (ActionConfigAsset action in collectedActions)
            {
                yield return action;
            }
        }

        private static void CollectActionRecursive(ActionConfigAsset action, HashSet<ActionConfigAsset> collectedActions)
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
