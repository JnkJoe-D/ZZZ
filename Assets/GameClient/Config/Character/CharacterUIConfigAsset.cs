using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "CharacterUIConfigAsset", menuName = "Config/Role/Character UI Config")]
    public class CharacterUIConfigAsset : ScriptableObject
    {
        public int RoleID;
        
        [Header("角色头像 (Avatar)")]
        public Sprite RoleIconGeneral; // 常规头像 (用于 StatusPanel 左上角)
        public Sprite RoleIconCircle;  // 圆形头像 (用于 连携技/时停/结算)
        
        [Header("专属核心机制 (Core Mechanic)")]
        public GameObject MechanicUIPrefab; // 核心机制 UI 的动态预制体

        [Header("强化特殊技 (EX Special Attack)")]
        public int EXSpecialAttackID; // 强化特殊技的ID，用于查询能量消耗
    }
}
