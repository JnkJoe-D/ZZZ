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

        [Header("Movement Feel & Damping (移动手感与阻尼配置)")]
        [Tooltip("松开移动按键后，移动向量平滑归零的阻尼衰减时长（秒）。\n0 表示按键松开瞬时归零（硬性切断）；>0 则平滑过渡归零（推荐 0.06~0.08s），模拟摇杆回弹与身体物理惯性。")]
        [Range(0f, 0.3f)]
        public float MoveInputDecelerationDuration = 0.08f;

        [Tooltip("松开移动按键后，触发 MoveStop（切入刹车/停止动作）的确认容差延迟（秒）。\n在此时间内若重新按下移动方向，将自动撤销停止意图并无缝继续奔跑，防止快速微调方向或小碎步频繁误触发刹车动作（推荐 0.10~0.15s）。")]
        [Range(0f, 0.3f)]
        public float MoveStopDelay = 0.12f;

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
