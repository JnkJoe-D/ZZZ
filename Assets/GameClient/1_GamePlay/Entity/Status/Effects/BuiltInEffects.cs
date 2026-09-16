using Game.Framework;
using System;
using UnityEngine;
using cfg.ZZZ;

namespace Game.GamePlay
{
    /// <summary>
    /// 修改属性效果。施加时添加修改器，移除时自动撤销。
    /// 由 Luban 的 BuffEffectData (EffectType = ModifyAttribute) 驱动。
    /// 用于实现攻击力+20%、最大HP+500 等。
    /// </summary>
    [BuffEffectBinding(BuffEffectType.ModifyAttribute)]
    public class ModifyAttributeEffect : IBuffEffect
    {
        public AttributeId TargetAttribute { get; }
        public ModifierOp Operation { get; }
        public float Value { get; }
        public bool ScaleWithStack { get; }

        public ModifyAttributeEffect(BuffEffectData cfg, BuffApplyContext ctx = null)
        {
            TargetAttribute = (AttributeId)cfg.AttrId;
            Operation = (ModifierOp)cfg.ParamInt1;
            float multiplier = ctx != null ? ctx.ValueMultiplier : 1f;
            Value = cfg.ParamFloat1 * multiplier;
            ScaleWithStack = cfg.ParamBool1;
        }

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
    /// 持续伤害 (DoT) 效果。
    /// 由 Luban 的 BuffEffectData (EffectType = DamageOverTime) 驱动。
    /// 用于实现灼烧、中毒、感电等。
    /// </summary>
    [BuffEffectBinding(BuffEffectType.DamageOverTime)]
    public class DamageOverTimeEffect : IBuffEffect
    {
        public float DamagePerSecond { get; }
        public AttributeId TargetAttribute { get; }
        public float TickInterval { get; }
        public bool ScaleWithStack { get; }

        private float _tickAccumulator;

        public DamageOverTimeEffect(BuffEffectData effectCfg, BuffApplyContext ctx = null)
        {
            float multiplier = ctx != null ? ctx.ValueMultiplier : 1f;
            DamagePerSecond = effectCfg.ParamFloat1 * multiplier;
            TickInterval = effectCfg.ParamFloat2 > 0f ? effectCfg.ParamFloat2 : 1f;
            TargetAttribute = effectCfg.AttrId != global::cfg.ZZZ.AttributeId.None ? (AttributeId)effectCfg.AttrId : AttributeId.HP;
            ScaleWithStack = effectCfg.ParamBool1;
        }

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime)
        {
            _tickAccumulator += deltaTime;

            if (_tickAccumulator >= TickInterval)
            {
                _tickAccumulator -= TickInterval;

                float damage = ScaleWithStack ? DamagePerSecond * buff.CurrentStack : DamagePerSecond;
                float intervalDamage = damage * TickInterval;
                target?.StatusModule?.Attributes?.Modify(TargetAttribute, -intervalDamage);
            }
        }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }
    }

    /// <summary>
    /// 持续治疗 (HoT) 效果。
    /// 由 Luban 的 BuffEffectData (EffectType = HealOverTime) 驱动。
    /// </summary>
    [BuffEffectBinding(BuffEffectType.HealOverTime)]
    public class HealOverTimeEffect : IBuffEffect
    {
        public float HealPerSecond { get; }
        public AttributeId TargetAttribute { get; }
        public float TickInterval { get; }
        public bool ScaleWithStack { get; }

        private float _tickAccumulator;

        public HealOverTimeEffect(BuffEffectData effectCfg, BuffApplyContext ctx = null)
        {
            float multiplier = ctx != null ? ctx.ValueMultiplier : 1f;
            HealPerSecond = effectCfg.ParamFloat1 * multiplier;
            TickInterval = effectCfg.ParamFloat2 > 0f ? effectCfg.ParamFloat2 : 1f;
            TargetAttribute = effectCfg.AttrId != global::cfg.ZZZ.AttributeId.None ? (AttributeId)effectCfg.AttrId : AttributeId.HP;
            ScaleWithStack = effectCfg.ParamBool1;
        }

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }

        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime)
        {
            _tickAccumulator += deltaTime;

            if (_tickAccumulator >= TickInterval)
            {
                _tickAccumulator -= TickInterval;

                float heal = ScaleWithStack ? HealPerSecond * buff.CurrentStack : HealPerSecond;
                float intervalHeal = heal * TickInterval;
                target?.StatusModule?.Attributes?.Modify(TargetAttribute, +intervalHeal);
            }
        }

        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }

        public void OnRemove(BuffInstance buff, CharacterEntity target)
        {
            _tickAccumulator = 0f;
        }
    }

    /// <summary>
    /// 专属机制计数器效果。
    /// </summary>
    public class AttributeCounterEffect : IBuffEffect
    {
        public AttributeId CounterAttribute { get; }
        public bool ResetOnRemove { get; }

        public AttributeCounterEffect(BuffEffectData cfg)
        {
            CounterAttribute = (AttributeId)cfg.AttrId;
            ResetOnRemove = cfg.ParamBool1;
        }

        public void OnApply(BuffInstance buff, CharacterEntity target)
        {
            var attrSet = target?.StatusModule?.Attributes;
            if (attrSet == null) return;

            if (!attrSet.Has(CounterAttribute))
            {
                GLog.Warning(LogTags.Buff, $"属性 {CounterAttribute} 未在角色 {target.name} 的属性配置表中找到。");
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
