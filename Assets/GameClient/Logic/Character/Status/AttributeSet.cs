using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性集合容器。持有角色的所有运行时属性实例。
    /// 对外提供统一的查询/修改接口，内部处理 Max 属性联动和自然回复。
    /// </summary>
    public class AttributeSet
    {
        private readonly Dictionary<AttributeId, AttributeInstance> _attributes = new(8);
        private CharacterEntity _owner;

        // Max → Current 的映射表（如 MaxHP → HP），用于联动 Clamp
        private static readonly Dictionary<AttributeId, AttributeId> MaxToCurrentMap = new()
        {
            { AttributeId.MaxHP, AttributeId.HP },
            { AttributeId.MaxEnergy, AttributeId.Energy },
            { AttributeId.MaxDaze, AttributeId.Daze },
        };

        public void Init(CharacterEntity owner)
        {
            _owner = owner;
        }

        /// <summary>注册一个属性实例。</summary>
        public void Register(AttributeInstance instance)
        {
            if (instance == null) return;
            _attributes[instance.Id] = instance;
        }

        /// <summary>是否拥有指定属性。</summary>
        public bool Has(AttributeId id) => _attributes.ContainsKey(id);

        /// <summary>获取属性实例。不存在时返回 null。</summary>
        public AttributeInstance Get(AttributeId id)
        {
            _attributes.TryGetValue(id, out var instance);
            return instance;
        }

        /// <summary>获取当前值。属性不存在返回 0。</summary>
        public float GetCurrent(AttributeId id)
        {
            return _attributes.TryGetValue(id, out var inst) ? inst.CurrentValue : 0f;
        }

        /// <summary>获取最终值（应用修改器后）。属性不存在返回 0。</summary>
        public float GetFinal(AttributeId id)
        {
            return _attributes.TryGetValue(id, out var inst) ? inst.FinalValue : 0f;
        }

        /// <summary>
        /// 获取当前值占最大值的百分比 (0~1)。
        /// 需要同时存在对应的 Max 属性，否则返回 1。
        /// </summary>
        public float GetPercent(AttributeId id)
        {
            if (!_attributes.TryGetValue(id, out var inst)) return 1f;

            // 查找对应的 Max 属性
            AttributeId maxId = GetMaxIdFor(id);
            if (maxId != AttributeId.None && _attributes.TryGetValue(maxId, out var maxInst))
            {
                float max = maxInst.FinalValue;
                return max > 0f ? inst.CurrentValue / max : 0f;
            }

            return 1f;
        }

        /// <summary>修改当前值（增量），并发布事件。</summary>
        public void Modify(AttributeId id, float delta)
        {
            if (!_attributes.TryGetValue(id, out var inst)) return;

            float old = inst.CurrentValue;
            inst.ModifyCurrent(delta);

            // 如果有对应的 Max 属性，Clamp 到 Max
            ClampToMax(id);

            PublishChange(id, old, inst.CurrentValue);
        }

        /// <summary>设置当前值（绝对值），并发布事件。</summary>
        public void SetValue(AttributeId id, float value)
        {
            if (!_attributes.TryGetValue(id, out var inst)) return;

            float old = inst.CurrentValue;
            inst.SetCurrent(value);
            ClampToMax(id);
            PublishChange(id, old, inst.CurrentValue);
        }

        /// <summary>每帧驱动：自然回复。</summary>
        public void Tick(float deltaTime)
        {
            foreach (var pair in _attributes)
            {
                AttributeInstance inst = pair.Value;
                float regen = inst.Definition.RegenPerSecond;
                if (Mathf.Approximately(regen, 0f)) continue;

                float old = inst.CurrentValue;
                inst.ModifyCurrent(regen * deltaTime);
                ClampToMax(pair.Key);

                if (!Mathf.Approximately(old, inst.CurrentValue))
                {
                    PublishChange(pair.Key, old, inst.CurrentValue);
                }
            }
        }

        /// <summary>清空所有属性。</summary>
        public void Clear()
        {
            _attributes.Clear();
        }

        /// <summary>
        /// 当 Max 属性的修改器变化后，需要重新 Clamp 对应的 Current 属性。
        /// 由外部（如 BuffEffect 修改 MaxHP 后）主动调用。
        /// </summary>
        public void OnMaxAttributeChanged(AttributeId maxId)
        {
            if (MaxToCurrentMap.TryGetValue(maxId, out AttributeId currentId))
            {
                ClampToMax(currentId);
            }
        }

        // ────────────────── 内部 ──────────────────

        private void ClampToMax(AttributeId id)
        {
            AttributeId maxId = GetMaxIdFor(id);
            if (maxId == AttributeId.None) return;
            if (!_attributes.TryGetValue(maxId, out var maxInst)) return;
            if (!_attributes.TryGetValue(id, out var inst)) return;

            inst.ClampCurrentToMax(maxInst.FinalValue);
        }

        private static AttributeId GetMaxIdFor(AttributeId id)
        {
            return id switch
            {
                AttributeId.HP => AttributeId.MaxHP,
                AttributeId.Energy => AttributeId.MaxEnergy,
                AttributeId.Daze => AttributeId.MaxDaze,
                _ => AttributeId.None
            };
        }

        private void PublishChange(AttributeId id, float oldValue, float newValue)
        {
            if (Mathf.Approximately(oldValue, newValue)) return;
            if (_owner == null) return;

            // 查找 Max 用于事件
            AttributeId maxId = GetMaxIdFor(id);
            float maxValue = maxId != AttributeId.None && _attributes.TryGetValue(maxId, out var maxInst)
                ? maxInst.FinalValue
                : 0f;

            Game.Framework.EventCenter.Publish(new Game.Framework.PlayerStatChangedEvent
            {
                PlayerId = _owner.GetInstanceID(),
                StatType = MapToStatType(id),
                OldValue = oldValue,
                NewValue = newValue,
                MaxValue = maxValue
            });
        }

        private static Game.Framework.StatType MapToStatType(AttributeId id)
        {
            return id switch
            {
                AttributeId.HP => Game.Framework.StatType.HP,
                AttributeId.Energy => Game.Framework.StatType.MP,
                AttributeId.Daze => Game.Framework.StatType.Stamina,
                _ => Game.Framework.StatType.Experience // fallback
            };
        }
    }
}
