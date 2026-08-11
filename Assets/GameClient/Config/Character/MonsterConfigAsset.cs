using System.Collections.Generic;
using UnityEngine;
using Game.Logic;

namespace Game.Logic
{
    public enum MonsterFaction
    {
        None = 0,
        Ethereal = 10,     // 以太体 (例如: 杜拉汉, 塔纳托斯)
        Machine = 20,      // 机械体 (例如: 重工机甲)
        Humanoid = 30,     // 人形怪 (例如: 帮派成员, 叛军)
        Corrupted = 40,    // 侵蚀体
    }

    public enum MonsterGrade
    {
        Normal = 0,        // 普通小怪 (易受击退，失衡极快)
        Elite = 10,        // 精英怪 (有独立的失衡条，较难打断，失衡可触发连携)
        Boss = 20,         // 首领怪 (多阶段，高霸体，免疫部分控制)
    }

    public enum ElementalType
    {
        Physical = 0,      // 物理
        Fire = 10,         // 火
        Ice = 20,          // 冰
        Electric = 30,     // 电
        Ether = 40,        // 以太
    }

    [CreateAssetMenu(fileName = "MonsterConfigAsset", menuName = "Config/Role/Monster Config")]
    public class MonsterConfigAsset : CharacterConfigAsset
    {
        [Header("怪物身份设定")]
        [Tooltip("怪物阵营分类，决定弱点与抗性倾向。例如：以太体弱以太，机械体弱电，人形弱火。")]
        public MonsterFaction Faction = MonsterFaction.Ethereal;
        
        [Tooltip("怪物阶级，决定霸体机制与连携技触发条件。")]
        public MonsterGrade Grade = MonsterGrade.Normal;

        [Header("属性抗性与弱点")]
        [Tooltip("天然弱点属性（受到该属性伤害增加，且属性异常积蓄极快）")]
        public List<ElementalType> Weaknesses = new List<ElementalType>();

        [Tooltip("天然抗性属性（受到该属性伤害减少，且属性异常积蓄极慢）")]
        public List<ElementalType> Resistances = new List<ElementalType>();

        [Header("感知与行为参数 (供 AI 读取)")]
        [Tooltip("发现敌人的感知半径（索敌范围）")]
        public float PerceptionRadius = 15f;
        
        [Tooltip("追击敌人的最大半径，超出后脱战或返回出生点")]
        public float LoseTargetRadius = 25f;

        [Tooltip("视野夹角（角度，例如 120度表示前方宽广视野）")]
        [Range(0, 360)]
        public float FieldOfView = 120f;

        [Header("战斗与失衡机制 (Daze)")]
        [Tooltip("受击硬直的韧性等级 (Poise)，越高越难被打断基础动作")]
        public int PoiseLevel = 1;

        [Tooltip("基础失衡值上限 (Daze Limit)，达到上限后怪物进入 Stunned 状态")]
        public float MaxDaze = 100f;
    }
}
