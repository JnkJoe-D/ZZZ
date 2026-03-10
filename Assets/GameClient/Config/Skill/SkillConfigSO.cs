using UnityEngine;
using SkillEditor;
using Game.Logic.Action.Combo;

namespace Game.Logic.Action.Config
{
    public enum SkillCategory
    {
        LightAttack =10, // 普攻
        HeavyAttack =20, // 重攻击
        DashAttack =30, //冲刺普攻
        DodgeCounter =40, //闪避反击
        SpecialSkill = 50,  // 特殊技
        EnhencedSpecialSkill = 60, //强化特殊技
        ChainSkill =70, //切人连携技
        AssistSkill= 80, //切人支援技
        Ultimate =100,      //终结技
        // 注：Dash 已经在 GlobalAnimationConfig 中处理，此处不再列出
    }

    /// <summary>
    /// 技能独立配置
    /// 用于配置技能参数，并关联 SkillTimeline 资产
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Config/Action/Skill Config")]
    public class SkillConfigSO : ActionConfigSO
    {
        [Header("Skill Base Info")]
        public SkillCategory Category;

        [Header("Combat Params")]
        public float Cooldown = 0f;       // 冷却时间
        public int MPCost = 0;            // 蓝耗
        public float CastRange = 2f;      // 施法距离 / 索敌距离
        public bool CanBeInterrupted;     // 是否可以被硬直打断

        [Header("Cinematic & Camera (大招特化)")]
        [Tooltip("例如大招播放时，需要实例化的虚拟相机预制体（Timeline等）")]
        public GameObject CinematicCameraPrefab;
    }
}
