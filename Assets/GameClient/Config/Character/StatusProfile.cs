using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性覆盖条目。策划可在 StatusProfile 中覆盖属性的默认初始值和最大值。
    /// </summary>
    [Serializable]
    public class AttributeOverride
    {
        [Tooltip("要覆盖的属性定义。")]
        public AttributeDefAsset Attribute;

        [Tooltip("覆盖初始值。")]
        public float InitialValue;

        [Tooltip("覆盖最大值（仅对有 Max 配对的属性生效）。")]
        public float MaxValue;
    }

    /// <summary>
    /// 角色状态配置（ScriptableObject）。
    /// 定义角色拥有哪些属性、哪些初始 Buff、以及免疫的 Buff 标签。
    /// 挂载在 CharacterConfigAsset 上。
    /// </summary>
    [CreateAssetMenu(fileName = "StatusProfile", menuName = "Config/Status/Status Profile")]
    public class StatusProfile : GameConfigAsset
    {
        [Header("属性配置")]
        [Tooltip("该角色拥有的属性列表。未在此列出的属性不会被创建。")]
        public List<AttributeDefAsset> Attributes = new();

        [Header("属性覆盖")]
        [Tooltip("覆盖属性的默认值（如设置角色特定的 HP 上限）。")]
        public List<AttributeOverride> Overrides = new();

        [Header("初始 Buff")]
        [Tooltip("角色出生时自动施加的 Buff（含角色独有机制 Buff）。")]
        public List<BuffDefAsset> InitialBuffs = new();

        [Header("免疫标签")]
        [Tooltip("免疫的 Buff 标签。带这些标签的 Buff 无法施加到该角色。")]
        public List<string> ImmuneTags = new();
    }
}
