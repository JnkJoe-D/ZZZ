using UnityEngine;
using ATEditor;
using Game.Logic;

namespace Game.Logic
{
    /// <summary>
    /// 技能独立配置
    /// 用于配置技能参数，并关联 SkillTimeline 资产
    /// </summary>
    [CreateAssetMenu(fileName = "SkillConfigAsset", menuName = "Config/Action/Skill Config")]
    public class SkillConfigAsset : ActionConfigAsset
    {
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
