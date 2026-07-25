using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性定义资产（ScriptableObject）。
    /// 策划在 Inspector 中创建，定义一个属性的默认参数。
    /// </summary>
    [CreateAssetMenu(fileName = "AttributeDefAsset", menuName = "Config/Status/Attribute Definition")]
    public class AttributeDefAsset : GameConfigAsset
    {
        [Header("标识")]
        [Tooltip("属性枚举 ID，代码层通过此 ID 快速索引。")]
        public AttributeId Id;

        [Tooltip("属性显示名（用于 UI 和调试）。")]
        public string DisplayName;

        [Header("默认值")]
        [Tooltip("默认最大值。对于 HP 类属性表示满血量；对于 Energy 类表示满能量。")]
        public float DefaultMax = 100f;

        [Tooltip("默认初始值。角色出生时的起始值。")]
        public float DefaultInitial = 100f;

        [Header("值域")]
        [Tooltip("允许的最小值。通常为 0。")]
        public float ClampMin = 0f;

        [Tooltip("允许的最大值上限。防止修改器导致数值溢出。")]
        public float ClampMax = 99999f;

        [Header("自然变化")]
        [Tooltip("每秒自然回复量（负值表示自然衰减）。0 表示无自然变化。")]
        public float RegenPerSecond = 0f;
    }
}
