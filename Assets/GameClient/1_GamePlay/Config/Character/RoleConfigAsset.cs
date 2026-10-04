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

        [Tooltip("输入残留缓存的平滑阻尼时长（秒）。用于与当前原生输入对比获取输入变化量与方向，判定180度掉头转向路由（推荐 0.1~0.15s）。\n>0 则平滑滞后追踪，为大幅度转向/掉头检测保留前向输入残留的时间窗口。")]
        [Range(0.01f, 0.5f)]
        public float MoveInputResidualDuration = 0.12f;
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
    public class ParryActionEntry
    {
        [Tooltip("招架反制动作 (如 ParryAid_L / ParryAid_H)")]
        public ActionConfigAsset Action;

        [HitEffectId]
        [Tooltip("招架反制命中效果 ID (配置失衡值、怪物 Parried 受击表现等，支持 Luban 查表)")]
        public int HitEffectId = 1050120;

        [Tooltip("招架成功双方顿帧时长 (秒)")]
        [Range(0.01f, 0.5f)]
        public float HitStopDuration = 0.2f;
    }

    [System.Serializable]
    public class RoleAssistConfig
    {
        [Tooltip("角色支援类型：近战招架 / 远程回避")]
        public RoleAssistType SupportType = RoleAssistType.ParryAid;

        [Header("近战招架起手动作与位移配置 (ParryAidStart)")]
        [Tooltip("招架起手动作配置（ParryAid_Start），切入招架时优先切入此动作")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public ActionConfigAsset ParryStartAction;

        [Tooltip("招架起手完成举刀防御的准备时长（即时期 a 到时期 b 的时间点 T_ready，单位：秒，推荐 0.2s）")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public float ParryReadyDuration = 0.2f;

        [Tooltip("招架起手在时期 a 完整播放时产生的 Z 轴前向根运动总位移（米）。\n切入时将根据剩余播放时间比例动态扣除生成位置，确保举刀时刚好停留在接刀锚点。")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public float ParryRootMotionZOffset = 1.0f;

        [Header("近战轻招架反制配置 (消耗 1 点支援点数)")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public ParryActionEntry ParryLight = new ParryActionEntry();

        [Header("近战重招架反制配置 (消耗 2 点支援点数)")]
        [ShowIf("SupportType", RoleAssistType.ParryAid)]
        public ParryActionEntry ParryHeavy = new ParryActionEntry();

        // 兼容转发：用于外部（如技能点校验）只读获取轻重动作引用
        public ActionConfigAsset ParryLAction => ParryLight?.Action;
        public ActionConfigAsset ParryHAction => ParryHeavy?.Action;

        /// <summary>
        /// 根据招架强度权重获取对应的招架反制配置条目
        /// </summary>
        public ParryActionEntry GetParryEntry(ATEditor.ParryWeight weight)
        {
            return weight == ATEditor.ParryWeight.Heavy ? ParryHeavy : ParryLight;
        }

        [Header("远程回避动作引用 (用于查 TbSkill 消耗/条件)")]
        [Tooltip("回避支援技能动作，对应 TbSkill 配置（通常消耗 1 点支援点数）")]
        [ShowIf("SupportType", RoleAssistType.EvasionAid)]
        public ActionConfigAsset EvasionAidAction;
    }
}
