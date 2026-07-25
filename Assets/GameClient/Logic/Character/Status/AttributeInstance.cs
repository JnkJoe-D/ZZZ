using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 单个属性的运行时实例。
    /// 管理基础值、修改器列表，并负责重新计算最终值。
    /// </summary>
    public class AttributeInstance
    {
        private readonly List<AttributeModifier> _modifiers = new(4);
        private bool _dirty = true;
        private float _cachedFinal;

        /// <summary>该属性的定义资产。</summary>
        public AttributeDefAsset Definition { get; }

        /// <summary>属性 ID（从 Definition 缓存，避免反复寻址）。</summary>
        public AttributeId Id => Definition.Id;

        /// <summary>基础值（未经修改器处理的原始值）。</summary>
        public float BaseValue { get; private set; }

        /// <summary>当前值（应用 Clamp 后的最终可用值）。</summary>
        public float CurrentValue { get; private set; }

        /// <summary>
        /// 当前有效的最大值。
        /// 如果此属性有对应的 Max 属性（如 HP 对应 MaxHP），
        /// 由 AttributeSet 在外部负责 Clamp；此处仅做自身 ClampMin/Max 裁剪。
        /// </summary>
        public float FinalValue
        {
            get
            {
                if (_dirty)
                {
                    Recalculate();
                }
                return _cachedFinal;
            }
        }

        /// <summary>属性变化回调。参数: (旧值, 新值)。</summary>
        public event Action<float, float> OnValueChanged;

        public AttributeInstance(AttributeDefAsset definition, float initialValue)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            BaseValue = initialValue;
            _dirty = true;
            // 初始化 CurrentValue
            CurrentValue = Mathf.Clamp(initialValue, definition.ClampMin, definition.ClampMax);
        }

        // ────────────────── 基础值操作 ──────────────────

        /// <summary>设置基础值并重新计算。</summary>
        public void SetBase(float value)
        {
            BaseValue = value;
            MarkDirty();
            ApplyFinal();
        }

        /// <summary>直接设置当前值（绕过修改器，但仍受 Clamp 限制）。</summary>
        public void SetCurrent(float value)
        {
            float old = CurrentValue;
            CurrentValue = Mathf.Clamp(value, Definition.ClampMin, Definition.ClampMax);
            if (!Mathf.Approximately(old, CurrentValue))
            {
                OnValueChanged?.Invoke(old, CurrentValue);
            }
        }

        /// <summary>修改当前值（增量）。</summary>
        public void ModifyCurrent(float delta)
        {
            SetCurrent(CurrentValue + delta);
        }

        /// <summary>将当前值限制在 [ClampMin, maxFromSet] 范围内。由 AttributeSet 调用。</summary>
        public void ClampCurrentToMax(float maxFromSet)
        {
            float clamped = Mathf.Clamp(CurrentValue, Definition.ClampMin, maxFromSet);
            if (!Mathf.Approximately(clamped, CurrentValue))
            {
                float old = CurrentValue;
                CurrentValue = clamped;
                OnValueChanged?.Invoke(old, CurrentValue);
            }
        }

        // ────────────────── 修改器操作 ──────────────────

        public void AddModifier(AttributeModifier modifier)
        {
            if (modifier == null) return;
            _modifiers.Add(modifier);
            MarkDirty();
            ApplyFinal();
        }

        public bool RemoveModifier(int modifierId)
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Id == modifierId)
                {
                    _modifiers.RemoveAt(i);
                    MarkDirty();
                    ApplyFinal();
                    return true;
                }
            }
            return false;
        }

        /// <summary>移除所有来源为指定 SourceId 的修改器。</summary>
        public int RemoveModifiersBySource(int sourceId)
        {
            int removed = 0;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].SourceId == sourceId)
                {
                    _modifiers.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0)
            {
                MarkDirty();
                ApplyFinal();
            }
            return removed;
        }

        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            MarkDirty();
            ApplyFinal();
        }

        // ────────────────── 内部 ──────────────────

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void Recalculate()
        {
            float flat = 0f;
            float percent = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                AttributeModifier mod = _modifiers[i];
                switch (mod.Op)
                {
                    case ModifierOp.Flat:
                        flat += mod.Value;
                        break;
                    case ModifierOp.Percent:
                        percent += mod.Value;
                        break;
                }
            }

            // 最终值 = 基础值 × (1 + 百分比之和) + 固定值之和
            _cachedFinal = BaseValue * (1f + percent) + flat;
            _cachedFinal = Mathf.Clamp(_cachedFinal, Definition.ClampMin, Definition.ClampMax);
            _dirty = false;
        }

        /// <summary>将 FinalValue 同步到 CurrentValue（仅用于 Max 类属性等需要直接跟踪 Final 的场景）。</summary>
        public void SyncCurrentToFinal()
        {
            SetCurrent(FinalValue);
        }

        private void ApplyFinal()
        {
            // 当修改器变化时，重新计算 FinalValue
            // 注意：对于 MaxHP 这类属性，FinalValue 的变化会由 AttributeSet 传播到 HP 的 Clamp
            _ = FinalValue; // 触发 Recalculate
        }
    }
}
