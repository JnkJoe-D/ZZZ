using UnityEngine;
using SkillEditor;

namespace Game.Logic.Skill.Config
{
    public enum SkillCategory
    {
        LightAttack, // 普攻
        HeavyAttack, // 重攻击
        DashAttack, //冲刺普攻
        DodgeCounter, //闪避反击
        SpecialSkill,  // 特殊技
        EnhencedSpecialSkill, //强化特殊技
        ChainSkill, //切人连携技
        AssistSkill, //切人支援技
        Ultimate,      //终结技
        // 注：Dash 已经在 GlobalAnimationConfig 中处理，此处不再列出
    }

    /// <summary>
    /// 技能独立配置
    /// 用于配置技能参数，并关联 SkillTimeline 资产
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Config/Skill/Skill Config")]
    public class SkillConfigSO : ScriptableObject
    {
        [Header("Base Info")]
        public int SkillID;
        public string SkillName;
        public SkillCategory Category;

        [Header("Skill Editor Asset")]
        [Tooltip("技能编辑器产出的时间轴数据")]
        public TextAsset TimelineAsset;

        [Header("Combat Params")]
        public float Cooldown = 0f;       // 冷却时间
        public int MPCost = 0;            // 蓝耗
        public float CastRange = 2f;      // 施法距离 / 索敌距离
        public bool CanBeInterrupted;     // 是否可以被硬直打断

        [Header("Combo Setting (普通攻击套用)")]
        [Tooltip("连击的下一段技能，为空则代表连段结束")]
        public SkillConfigSO NextComboSkill;
        [Tooltip("接收下一招指令的输入窗口期（标准化时间 0~1 或 秒数）")]
        public Vector2 ComboInputWindow = new Vector2(0.4f, 0.8f);

        [Header("Cinematic & Camera (大招特化)")]
        [Tooltip("例如大招播放时，需要实例化的虚拟相机预制体（Timeline等）")]
        public GameObject CinematicCameraPrefab;
    }
}
