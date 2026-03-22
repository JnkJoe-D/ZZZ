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

        [Header("Base Locomotion Actions")]
        public LocomotionConfigAsset IdleConfig;
        public LocomotionConfigAsset JogStartConfig;
        public LocomotionConfigAsset JogConfig;
        public LocomotionConfigAsset JogStopConfig;
        public LocomotionConfigAsset DashStartConfig;
        public LocomotionConfigAsset DashTurnBackConfig;
        public LocomotionConfigAsset DashConfig;
        public LocomotionConfigAsset DashStopConfig;

        [Header("Evade")]
        public SkillConfigAsset[] evadeFront;
        public SkillConfigAsset[] evadeBack;
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("Light Attacks")]
        public SkillConfigAsset[] lightAttacks;

        [Header("Heavy Attacks")]
        public SkillConfigAsset[] heavyAttacks;

        [Header("Dash Attack")]
        public SkillConfigAsset dashAttack;

        [Header("Dodge Counter")]
        public SkillConfigAsset[] dodgeCounter;

        [Header("Special Skill")]
        public SkillConfigAsset specialSkill;

        [Header("Perfect Special Skill")]
        public SkillConfigAsset specialSkillPerfect;

        [Header("Enhanced Special Skill")]
        public SkillConfigAsset enhancedSpecialSkill;

        [Header("Perfect Enhanced Special Skill")]
        public SkillConfigAsset enhancedSpecialSkillPerfect;

        [Header("Chain Skill")]
        public SkillConfigAsset chainSkill;

        [Header("Assist Skill")]
        public SkillConfigAsset assistSkill;

        [Header("Ultimate")]
        public SkillConfigAsset Ultimate;

        [Header("AI")]
        public BehaviorTreeGraphAsset BehaviorTreeGraph;

        [Header("Command Context Routes")]
        public CommandContextConfig CommandContextConfig;

        [Header("Hit Reaction")]
        public HitReactionConfig hitReactionConfig;

        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (IdleConfig != null) yield return IdleConfig;
            if (JogStartConfig != null) yield return JogStartConfig;
            if (JogConfig != null) yield return JogConfig;
            if (JogStopConfig != null) yield return JogStopConfig;
            if (DashStartConfig != null) yield return DashStartConfig;
            if (DashTurnBackConfig != null) yield return DashTurnBackConfig;
            if (DashConfig != null) yield return DashConfig;
            if (DashStopConfig != null) yield return DashStopConfig;

            if (evadeFront != null)
            {
                foreach (SkillConfigAsset action in evadeFront)
                {
                    if (action != null) yield return action;
                }
            }

            if (evadeBack != null)
            {
                foreach (SkillConfigAsset action in evadeBack)
                {
                    if (action != null) yield return action;
                }
            }

            if (lightAttacks != null)
            {
                foreach (SkillConfigAsset action in lightAttacks)
                {
                    if (action != null) yield return action;
                }
            }

            if (heavyAttacks != null)
            {
                foreach (SkillConfigAsset action in heavyAttacks)
                {
                    if (action != null) yield return action;
                }
            }

            if (dashAttack != null) yield return dashAttack;

            if (dodgeCounter != null)
            {
                foreach (SkillConfigAsset action in dodgeCounter)
                {
                    if (action != null) yield return action;
                }
            }

            if (specialSkill != null) yield return specialSkill;
            if (specialSkillPerfect != null) yield return specialSkillPerfect;
            if (enhancedSpecialSkill != null) yield return enhancedSpecialSkill;
            if (enhancedSpecialSkillPerfect != null) yield return enhancedSpecialSkillPerfect;
            if (chainSkill != null) yield return chainSkill;
            if (assistSkill != null) yield return assistSkill;
            if (Ultimate != null) yield return Ultimate;

            if (CommandContextConfig != null)
            {
                foreach (ActionConfigAsset action in CommandContextConfig.GetAllActions())
                {
                    if (action != null)
                    {
                        yield return action;
                    }
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
