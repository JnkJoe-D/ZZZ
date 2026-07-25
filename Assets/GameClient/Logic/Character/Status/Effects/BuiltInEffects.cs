using System;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 修改属性效果。施加时添加修改器，移除时自动撤销。
    /// 用于实现攻击力+20%、最大HP+500 等。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("属性修改效果 (ModifyAttribute)")]
    public class ModifyAttributeEffect : IBuffEffect
    {
        [Tooltip("要修改的目标属性。")]
        public AttributeId TargetAttribute;

        [Tooltip("修改器运算类型。")]
        public ModifierOp Operation = ModifierOp.Flat;

        [Tooltip("修改值。Flat 模式为绝对值；Percent 模式为百分比 (0.2 = +20%)。")]
        public float Value;

        [Tooltip("是否按叠加层数倍增效果。")]
        public bool ScaleWithStack = true;

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            ApplyModifier(buff, target);
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime)
        {
            // 属性修改器是持续性的，不需要每帧处理
        }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack)
        {
            if (!ScaleWithStack) return;

            // 叠加变化时：移除旧修改器，重新施加（值随层数变化）
            RemoveModifier(buff, target);
            ApplyModifier(buff, target);
        }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            RemoveModifier(buff, target);
        }

        private void ApplyModifier(BuffInstance buff, CharacterEntity target)
        {
            var attrSet = target?.StatusModule?.Attributes;
            var instance = attrSet?.Get(TargetAttribute);
            if (instance == null) return;

            float effectiveValue = ScaleWithStack ? Value * buff.CurrentStack : Value;
            var modifier = new AttributeModifier(Operation, effectiveValue, buff.RuntimeId);
            instance.AddModifier(modifier);

            // 如果修改的是 Max 属性，通知 AttributeSet 重新 Clamp 对应 Current
            attrSet.OnMaxAttributeChanged(TargetAttribute);
        }

        private void RemoveModifier(BuffInstance buff, CharacterEntity target)
        {
            var instance = target?.StatusModule?.Attributes?.Get(TargetAttribute);
            instance?.RemoveModifiersBySource(buff.RuntimeId);

            target?.StatusModule?.Attributes?.OnMaxAttributeChanged(TargetAttribute);
        }
    }

    /// <summary>
    /// 持续伤害 (DoT) 效果。每秒对目标造成固定伤害。
    /// 用于实现灼烧、中毒等。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("持续伤害 DoT (DamageOverTime)")]
    public class DamageOverTimeEffect : IBuffEffect
    {
        [Tooltip("每秒伤害量。")]
        public float DamagePerSecond = 10f;

        [Tooltip("伤害的目标属性（通常为 HP）。")]
        public AttributeId TargetAttribute = AttributeId.HP;

        [Tooltip("是否按叠加层数倍增伤害。")]
        public bool ScaleWithStack = true;

        private float _tickAccumulator;

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime)
        {
            _tickAccumulator += deltaTime;

            // 每秒结算一次
            if (_tickAccumulator >= 1f)
            {
                _tickAccumulator -= 1f;

                float damage = ScaleWithStack ? DamagePerSecond * buff.CurrentStack : DamagePerSecond;
                target?.StatusModule?.Attributes?.Modify(TargetAttribute, -damage);
            }
        }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack)
        {
            // 伤害在 Tick 中按层数计算，无需额外处理
        }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }
    }

    /// <summary>
    /// 持续治疗 (HoT) 效果。每秒对目标回复固定生命值。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("持续治疗 HoT (HealOverTime)")]
    public class HealOverTimeEffect : IBuffEffect
    {
        [Tooltip("每秒回复量。")]
        public float HealPerSecond = 10f;

        [Tooltip("回复的目标属性（通常为 HP）。")]
        public AttributeId TargetAttribute = AttributeId.HP;

        public bool ScaleWithStack = true;

        private float _tickAccumulator;

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime)
        {
            _tickAccumulator += deltaTime;

            if (_tickAccumulator >= 1f)
            {
                _tickAccumulator -= 1f;

                float heal = ScaleWithStack ? HealPerSecond * buff.CurrentStack : HealPerSecond;
                target?.StatusModule?.Attributes?.Modify(TargetAttribute, +heal);
            }
        }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }
    }

    /// <summary>
    /// 属性计数器效果。用于实现角色独有仪表（如命中累积计数）。
    /// 此效果本身不自动累积——需要外部逻辑（如 HitImpact）主动调用 Modify。
    /// 它的作用是在 Buff 施加时确保目标属性存在并初始化。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("专属机制计数器 (AttributeCounter)")]
    public class AttributeCounterEffect : IBuffEffect
    {
        [Tooltip("计数器对应的属性 ID。")]
        public AttributeId CounterAttribute;

        [Tooltip("Buff 移除时是否清零计数器。")]
        public bool ResetOnRemove = true;

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            // 确保属性存在（如果 StatusProfile 中已配置则无需额外操作）
            // 这里只做安全检查
            var attrSet = target?.StatusModule?.Attributes;
            if (attrSet == null) return;

            if (!attrSet.Has(CounterAttribute))
            {
                Debug.LogWarning($"[AttributeCounterEffect] 属性 {CounterAttribute} 未在角色 {target.name} 的 StatusProfile 中配置。");
            }
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime) { }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            if (!ResetOnRemove) return;
            target?.StatusModule?.Attributes?.SetValue(CounterAttribute, 0f);
        }
    }
}
