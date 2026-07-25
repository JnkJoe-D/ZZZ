using System;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性阈值条件。用于 ActionRoute.ExtraConditions。
    /// 判断角色某个属性是否满足阈值要求。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("属性数值阈值条件 (AttributeThreshold)")]
    public sealed class AttributeThresholdCondition : ITransitionCondition
    {
        [Tooltip("要检查的属性。")]
        public AttributeId Attribute;

        [Tooltip("比较模式。")]
        public ComparisonMode Mode;

        [Tooltip("阈值（绝对值或百分比，取决于 UsePercent 设置）。")]
        public float Threshold;

        [Tooltip("勾选后以百分比 (0~1) 进行比较。")]
        public bool UsePercent;

        public bool Check(CharacterEntity actor)
        {
            var attrSet = actor?.StatusModule?.Attributes;
            if (attrSet == null || !attrSet.Has(Attribute)) return false;

            float value = UsePercent
                ? attrSet.GetPercent(Attribute)
                : attrSet.GetCurrent(Attribute);

            return Mode == ComparisonMode.LessThan
                ? value < Threshold
                : value >= Threshold;
        }
    }

    /// <summary>
    /// Buff 存在性条件。用于 ActionRoute.ExtraConditions。
    /// 判断角色是否持有指定 Buff。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("持有 Buff 状态条件 (HasBuff)")]
    public sealed class HasBuffCondition : ITransitionCondition
    {
        [Tooltip("要检查的 Buff 定义。")]
        public BuffDefAsset RequiredBuff;

        [Tooltip("反转结果：勾选后变为'不持有该 Buff 时为真'。")]
        public bool Inverse;

        public bool Check(CharacterEntity actor)
        {
            bool has = actor?.StatusModule?.Buffs?.HasBuff(RequiredBuff) ?? false;
            return Inverse ? !has : has;
        }
    }

    /// <summary>
    /// Buff 叠层条件。用于 ActionRoute.ExtraConditions。
    /// 判断角色持有的指定 Buff 叠层数是否满足阈值。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("Buff 叠层数量条件 (BuffStack)")]
    public sealed class BuffStackCondition : ITransitionCondition
    {
        [Tooltip("要检查的 Buff 定义。")]
        public BuffDefAsset TargetBuff;

        [Tooltip("比较模式。")]
        public ComparisonMode Mode;

        [Tooltip("叠层阈值。")]
        public int Threshold = 1;

        public bool Check(CharacterEntity actor)
        {
            int stack = actor?.StatusModule?.Buffs?.GetStack(TargetBuff?.BuffId ?? 0) ?? 0;

            return Mode == ComparisonMode.LessThan
                ? stack < Threshold
                : stack >= Threshold;
        }
    }
}
