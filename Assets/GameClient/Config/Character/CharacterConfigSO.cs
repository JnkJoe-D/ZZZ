using System.Collections.Generic;
using UnityEngine;
using Game.Logic.Action.Config;

namespace Game.Logic.Character.Config
{
    /// <summary>
    /// 角色基础配置
    /// 作为单个角色的数据字典，独立存放角色的各个属性与技能引用
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Config/Role/Character Config")]
    public class CharacterConfigSO : ScriptableObject
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
        public LocomotionConfigSO IdleConfig;
        public LocomotionConfigSO JogStartConfig;
        public LocomotionConfigSO JogConfig;
        public LocomotionConfigSO JogStopConfig;
        public LocomotionConfigSO DashStartConfig;
        public LocomotionConfigSO DashConfig;
        public LocomotionConfigSO DashStopConfig;
        [Header("闪避")]
        public SkillConfigSO[] evadeFront;
        public SkillConfigSO[] evadeBack;
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("普攻")]
        public SkillConfigSO[] lightAttacks;
        [Header("重攻击")]
        public SkillConfigSO[] heavyAttacks;
        [Header("冲刺普攻")]
        public SkillConfigSO dashAttack;
        [Header("闪避反击")]
        public SkillConfigSO[] dodgeCounter;
        [Header("特殊技")]
        public SkillConfigSO specialSkill;
        [Header("强化特殊技")]
        public SkillConfigSO enhancedSpecialSkill;
        [Header("切人连携")]
        public SkillConfigSO chainSkill;
        [Header("切人支援")]
        public SkillConfigSO assistSkill;
        [Header("终结技")]
        public SkillConfigSO Ultimate;
    }
}
