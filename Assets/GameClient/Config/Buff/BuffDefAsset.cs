using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>Buff 叠加行为。</summary>
    public enum StackBehavior
    {
        /// <summary>叠加层数，刷新持续时间。</summary>
        StackAndRefresh = 0,

        /// <summary>叠加层数，不刷新持续时间。</summary>
        StackNoRefresh = 10,

        /// <summary>已达上限时拒绝新施加。</summary>
        Reject = 20,

        /// <summary>替换旧 Buff（移除后重新施加）。</summary>
        Replace = 30,
    }

    /// <summary>
    /// Buff 定义资产（ScriptableObject）。
    /// 策划在 Inspector 中创建，定义一个 Buff 的所有静态参数。
    /// </summary>
    [CreateAssetMenu(fileName = "BuffDefAsset", menuName = "Config/Status/Buff Definition")]
    public class BuffDefAsset : GameConfigAsset
    {
        [Header("标识")]
        public int BuffId;
        public string DisplayName;
        public Sprite Icon;

        [Header("持续时间")]
        [Tooltip("持续时间（秒）。-1 表示永久存在（需要手动移除）。")]
        public float Duration = 10f;

        [Header("叠加")]
        [Tooltip("最大叠加层数。1 表示不可叠加。")]
        [Min(1)]
        public int MaxStack = 1;

        public StackBehavior StackBehavior = StackBehavior.StackAndRefresh;

        [Header("标签")]
        [Tooltip("Buff 标签，用于分类、互斥、批量清除。")]
        public List<string> Tags = new();

        [Header("效果列表")]
        [Tooltip("Buff 效果（多态列表）。施加/移除/Tick 时依次执行。")]
        [SerializeReference, SubclassSelector]
        public List<IBuffEffect> Effects = new();
    }
}
