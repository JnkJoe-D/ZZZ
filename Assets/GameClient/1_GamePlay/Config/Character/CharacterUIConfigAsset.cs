using UnityEngine;
using Game.Framework;
namespace Game.GamePlay
{
    [CreateAssetMenu(fileName = "CharacterUIConfigAsset", menuName = "Config/Role/Character UI Config")]
    public class CharacterUIConfigAsset : GameConfigAsset
    {
        public int RoleID;
        
        [Header("角色头像 (Avatar)")]
        public Sprite RoleIconGeneral; // 常规头像 (用于 StatusPanel 左上角)
        public Sprite RoleIconCircle;  // 圆形头像 (用于 连携技/时停/结算)
        
        [Header("专属核心机制 (Core Mechanic)")]
        public GameObject MechanicUIPrefab; // 核心机制 UI 的动态预制体

        [Header("强化特殊技 (EX Special Attack)")]
        public int EXSpecialAttackID; // 强化特殊技的ID，用于查询能量消耗

        [Header("按键面板图标配置 (SkillKey Icons)")]
        [Tooltip("基础特殊技图标 (未达标时的灰色暗淡图标。若留空则自动使用全局通用保底 SkillBtnBranch2)")]
        public Sprite SpecialAttackNormalIcon;

        [Tooltip("强化特殊技图标 (达标时的彩色发光图标。若留空则自动使用通用保底 SkillBtnBranch；仪玄配置 SkillBtnBranch02，叶瞬光配置 SkillBtnBranch_Zhenzhen)")]
        public Sprite SpecialAttackExIcon;

        [Tooltip("大招就绪专属图标 (喧响值满3000时的金色就绪图标。若留空则使用全局通用 IconRoleSkillKeyUltimate)")]
        public Sprite UltimateReadyIcon;

        [Tooltip("大招蓄力基础图标 (喧响值不足3000时的基础图标。若留空则使用全局通用 SkillQTE)")]
        public Sprite UltimateNormalIcon;
    }
}
