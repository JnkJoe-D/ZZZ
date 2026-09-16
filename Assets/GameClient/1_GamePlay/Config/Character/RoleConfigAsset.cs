using UnityEngine;

namespace Game.GamePlay
{
    [CreateAssetMenu(fileName = "RoleConfigAsset", menuName = "Config/Role/Role Config")]
    public class RoleConfigAsset : CharacterConfigAsset
    {
        [Header("UI Config")]
        public CharacterUIConfigAsset UIConfig;

        [Header("Ground Jog (Player)")]
        public float JogShortInputThreshold = 0.2f;

        [Header("Evade (Player)")]
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

        [Header("Support / Assist Config (支援能力配置)")]
        public RoleSupportConfig SupportConfig;
    }

    public enum RoleSupportType
    {
        [Tooltip("近战角色：招架支援（分轻重，消耗 1~2 点支援点数）")]
        ParryAid = 0,

        [Tooltip("远程角色：回避支援（无轻重，通常消耗 1 点支援点数）")]
        EvasionAid = 1
    }

    [System.Serializable]
    public class RoleSupportConfig
    {
        [Tooltip("角色支援类型：近战招架 / 远程回避")]
        public RoleSupportType SupportType = RoleSupportType.ParryAid;

        [Header("近战招架动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("轻招架执行动作（招架支援_L），对应 TbSkill 配置（通常消耗 1 点支援点数）")]
        public ActionConfigAsset ParryLightAction;

        [Tooltip("重招架执行动作（招架支援_H），对应 TbSkill 配置（通常消耗 2 点支援点数）")]
        public ActionConfigAsset ParryHeavyAction;

        [Header("远程回避动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("回避支援技能动作，对应 TbSkill 配置（通常消耗 1 点支援点数）")]
        public ActionConfigAsset EvasionAidAction;
    }
}
