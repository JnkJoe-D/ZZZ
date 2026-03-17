using System.Collections.Generic;
using UnityEngine;
using Game.Logic.Action.Config;
using Game.AI;

namespace Game.Logic.Character.Config
{
    /// <summary>
    /// 角色基础配置
    /// 作为单个角色的数据字典，独立存放角色的各个属性与技能引用
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterConfigAsset", menuName = "Config/Role/Character Config")]
    public class CharacterConfigAsset : ScriptableObject
    {
        [Header("基础信息")]
        public int RoleID;
        public string RoleName;
        public GameObject CharacterPrefab;

        [Header("地面检测")]
        public float GroundRadius = 0.3f; 
        public float GroundHeight = 1.8f; 
        public float GroundOffset = 0.1f; 
        public LayerMask GroundLayer;     

        [Header("基础速度倍率")]
        [Range(0,5f)]
        public float JogMultipier = 1f;
        [Range(0, 5f)]
        public float DashMultipier = 1f;
        [Range(0, 5f)]
        public float DodgeMultipier = 1f;
        [Header("攻击速度倍率")][Range(0,5f)]
        public float AttackMultipier = 1f;
        [Header("技能速度倍率")][Range(0,5f)]
        public float SkillMultipier = 1f;

        // [Header("基础动画 (Legacy)")]
        // public AnimSetEntry AnimationSet;

        [Header("基础移动配置 (Timeline 驱动)")]
        public LocomotionConfigAsset IdleConfig;
        public LocomotionConfigAsset JogStartConfig;
        public LocomotionConfigAsset JogConfig;
        public LocomotionConfigAsset JogStopConfig;
        public LocomotionConfigAsset DashStartConfig;
        public LocomotionConfigAsset DashConfig;
        public LocomotionConfigAsset DashStopConfig;
        [Header("闪避")]
        public SkillConfigAsset[] evadeFront;
        public SkillConfigAsset[] evadeBack;
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("普攻")]
        public SkillConfigAsset[] lightAttacks;
        [Header("重攻击")]
        public SkillConfigAsset[] heavyAttacks;
        [Header("冲刺普攻")]
        public SkillConfigAsset dashAttack;
        [Header("闪避反击")]
        public SkillConfigAsset[] dodgeCounter;
        [Header("特殊技")]
        public SkillConfigAsset specialSkill;
        [Header("特殊技_快速")]
        public SkillConfigAsset specialSkillPerfect;
        [Header("强化特殊技")]
        public SkillConfigAsset enhancedSpecialSkill;
        [Header("强化特殊技_快速")]
        public SkillConfigAsset enhancedSpecialSkillPerfect;
        [Header("切人连携")]
        public SkillConfigAsset chainSkill;
        [Header("切人支援")]
        public SkillConfigAsset assistSkill;
        [Header("终结技")]
        public SkillConfigAsset Ultimate;
        [Header("AI")]
        public BehaviorTreeGraphAsset BehaviorTreeGraph;

        [Header("受击表现")]
        public HitReactionConfig hitReactionConfig;
        /// <summary>
        /// 提取该角色所配置的所有可能被播放的动作，用于集中管理和启动时预加载
        /// </summary>
        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (IdleConfig != null) yield return IdleConfig;
            if (JogStartConfig != null) yield return JogStartConfig;
            if (JogConfig != null) yield return JogConfig;
            if (JogStopConfig != null) yield return JogStopConfig;
            if (DashStartConfig != null) yield return DashStartConfig;
            if (DashConfig != null) yield return DashConfig;
            if (DashStopConfig != null) yield return DashStopConfig;

            if (evadeFront != null) foreach (var c in evadeFront) if (c != null) yield return c;
            if (evadeBack != null) foreach (var c in evadeBack) if (c != null) yield return c;
            
            if (lightAttacks != null) foreach (var c in lightAttacks) if (c != null) yield return c;
            if (heavyAttacks != null) foreach (var c in heavyAttacks) if (c != null) yield return c;
            
            if (dashAttack != null) yield return dashAttack;
            if (dodgeCounter != null) foreach (var c in dodgeCounter) if (c != null) yield return c;
            
            if (specialSkill != null) yield return specialSkill;
            if (specialSkillPerfect != null) yield return specialSkillPerfect;
            if (enhancedSpecialSkillPerfect != null) yield return enhancedSpecialSkillPerfect;
            if (enhancedSpecialSkill != null) yield return enhancedSpecialSkill;
            if (chainSkill != null) yield return chainSkill;
            if (assistSkill != null) yield return assistSkill;
            if (Ultimate != null) yield return Ultimate;
            if (hitReactionConfig != null)
            {
                foreach(var hitActionConfig in hitReactionConfig.GetAllActionConfigs())
                {
                    yield return hitActionConfig;
                }
            }
        }
    }
}
