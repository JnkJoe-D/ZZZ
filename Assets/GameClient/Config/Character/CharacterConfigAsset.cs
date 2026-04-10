using System.Collections.Generic;
using Game.AI;
using Game.Logic.Action.Config;
using UnityEngine;

namespace Game.Logic.Character.Config
{
    [CreateAssetMenu(fileName = "CharacterConfigAsset", menuName = "Config/Role/Character Config")]
    public class CharacterConfigAsset : ScriptableObject
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
        [Range(0, 5f)]
        public float JogMultipier = 1f;

        [Header("Ground Jog")]
        [Tooltip("短按移动时，Jog 状态被视为短跑的时间阈值。")]
        public float JogShortInputThreshold = 0.2f;

        [Range(0, 5f)]
        public float DashMultipier = 1f;

        [Range(0, 5f)]
        public float DodgeMultipier = 1f;

        [Header("Attack Speed Multiplier")]
        [Range(0, 5f)]
        public float AttackMultipier = 1f;

        [Header("Skill Speed Multiplier")]
        [Range(0, 5f)]
        public float SkillMultipier = 1f;
        [Header("Evade")]
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("根动作")]
        public ActionConfigAsset ActionRoot;
        [Header("动作加载列表")]
        public List<ActionConfigAsset> ActionProLoadList = new List<ActionConfigAsset>();
        [Header("AI")]
        public BehaviorTreeGraphAsset BehaviorTreeGraph;
        [Header("Hit Reaction")]
        public HitReactionConfig hitReactionConfig;

        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (ActionProLoadList != null)
            {
                foreach (ActionConfigAsset action in ActionProLoadList)
                {
                    if (action != null) yield return action;
                }
            }

            if (hitReactionConfig != null)
            {
                foreach (ActionConfigAsset hitActionConfig in hitReactionConfig.GetAllActionConfigs())
                {
                    yield return hitActionConfig;
                }
            }
        }
    }
}
