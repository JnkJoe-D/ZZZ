using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    [CreateAssetMenu(fileName = "RoleConfigAsset", menuName = "Config/Role/Role Config")]
    public class RoleConfigAsset : CharacterConfigAsset
    {
        [Header("UI")]
        public CharacterUIConfigAsset UIConfig;
        [Header("输入")]
        public RoleInputConfig InputConfig;

        [Header("闪避")]
        public RoleEvadeConfig EvadeConfig;

        [Header("支援")]
        public RoleAssistConfig AssistConfig;
    }

    public enum RoleAssistType
    {
        [Tooltip("近战角色：招架支援（分轻重，消耗 1~2 点支援点数）")]
        [InspectorName("招架支援")]
        ParryAid = 0,

        [Tooltip("远程角色：回避支援（无轻重，通常消耗 1 点支援点数）")]
        [InspectorName("回避支援")]
        EvasionAid = 1
    }
    [System.Serializable]
    public class RoleInputConfig
    {
        [Header("移动输入")]
        [Tooltip("短按移动按键的时间阈值")]
        public float MoveShortInputThreshold = 0.2f;

        [Header("移动手感与阻尼配置")]
        [Tooltip("松开移动按键后，移动向量平滑归零的阻尼衰减时长（秒）。\n0 表示按键松开瞬时归零（硬性切断）；>0 则平滑过渡归零（推荐 0.06~0.08s），模拟摇杆回弹与身体物理惯性。")]
        [Range(0f, 0.3f)]
        public float MoveInputDecelerationDuration = 0.08f;
    }
    [System.Serializable]
    public class RoleEvadeConfig
    {
        [Header("闪避次数限制")]
        [Tooltip("闪避次数限制，0 表示无限制")]
        public int LimitedTimes = 2;

        [Header("闪避冷却时间")]
        [Tooltip("闪避冷却时间，单位秒")]
        public float CoolDown = 1f;
    }
    [System.Serializable]
    public class RoleAssistConfig
    {
        [Tooltip("角色支援类型：近战招架 / 远程回避")]
        public RoleAssistType SupportType = RoleAssistType.ParryAid;

        [Header("近战招架动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("轻招架执行动作（招架支援_L），对应 TbSkill 配置（通常消耗 1 点支援点数）")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public ActionConfigAsset ParryLAction;
        [Header("近战招架动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("重招架执行动作（招架支援_H），对应 TbSkill 配置（通常消耗 2 点支援点数）")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public ActionConfigAsset ParryHAction;

        [Header("远程回避动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("回避支援技能动作，对应 TbSkill 配置（通常消耗 1 点支援点数）")]
        [ShowIf("SupportType", RoleAssistType.EvasionAid)]
        public ActionConfigAsset EvasionAidAction;
    }
}
