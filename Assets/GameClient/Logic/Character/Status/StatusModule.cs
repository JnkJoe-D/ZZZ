using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 状态模块 — 桥接门面。
    /// 组合 AttributeSet 和 BuffContainer，提供统一初始化和 Tick 入口。
    /// 以组合模式挂载在 CharacterEntity 上。
    /// </summary>
    public class StatusModule
    {
        public AttributeSet Attributes { get; } = new();
        public BuffContainer Buffs { get; } = new();

        private CharacterEntity _owner;
        private StatusProfile _profile;

        /// <summary>
        /// 初始化状态模块。从 StatusProfile 读取配置，创建属性实例和初始 Buff。
        /// </summary>
        public void Init(CharacterEntity owner, StatusProfile profile)
        {
            _owner = owner;
            _profile = profile;

            Attributes.Init(owner);
            Buffs.Init(owner);

            if (profile == null) return;

            InitAttributes(profile);
            InitBuffs(profile);
        }

        /// <summary>每帧驱动。</summary>
        public void Tick(float deltaTime)
        {
            Attributes.Tick(deltaTime);
            Buffs.Tick(deltaTime);
        }

        /// <summary>清理所有状态。</summary>
        public void Clear()
        {
            Buffs.Clear();
            Attributes.Clear();
        }

        /// <summary>
        /// 检查 Buff 标签是否被免疫。
        /// 在施加 Buff 前由 BuffContainer 外部逻辑调用（或可在 AddBuff 扩展中集成）。
        /// </summary>
        public bool IsTagImmune(string tag)
        {
            if (_profile == null || _profile.ImmuneTags == null) return false;
            return _profile.ImmuneTags.Contains(tag);
        }

        /// <summary>
        /// 检查一个 Buff 是否被免疫（其任一标签命中免疫列表即免疫）。
        /// </summary>
        public bool IsBuffImmune(BuffDefAsset buffDef)
        {
            if (buffDef == null || _profile == null || _profile.ImmuneTags == null) return false;
            if (buffDef.Tags == null || buffDef.Tags.Count == 0) return false;

            for (int i = 0; i < buffDef.Tags.Count; i++)
            {
                if (_profile.ImmuneTags.Contains(buffDef.Tags[i]))
                {
                    return true;
                }
            }
            return false;
        }

        // ────────────────── 内部初始化 ──────────────────

        private void InitAttributes(StatusProfile profile)
        {
            if (profile.Attributes == null) return;

            // 构建覆盖字典
            Dictionary<AttributeId, AttributeOverride> overrideMap = null;
            if (profile.Overrides != null && profile.Overrides.Count > 0)
            {
                overrideMap = new Dictionary<AttributeId, AttributeOverride>(profile.Overrides.Count);
                for (int i = 0; i < profile.Overrides.Count; i++)
                {
                    AttributeOverride ov = profile.Overrides[i];
                    if (ov?.Attribute != null)
                    {
                        overrideMap[ov.Attribute.Id] = ov;
                    }
                }
            }

            for (int i = 0; i < profile.Attributes.Count; i++)
            {
                AttributeDefAsset def = profile.Attributes[i];
                if (def == null) continue;

                float initialValue = def.DefaultInitial;
                float maxValue = def.DefaultMax;

                // 应用覆盖
                if (overrideMap != null && overrideMap.TryGetValue(def.Id, out AttributeOverride ov))
                {
                    initialValue = ov.InitialValue;
                    maxValue = ov.MaxValue;
                }

                var instance = new AttributeInstance(def, initialValue);
                Attributes.Register(instance);
            }
        }

        private void InitBuffs(StatusProfile profile)
        {
            if (profile.InitialBuffs == null) return;

            for (int i = 0; i < profile.InitialBuffs.Count; i++)
            {
                BuffDefAsset buffDef = profile.InitialBuffs[i];
                if (buffDef == null) continue;

                Buffs.AddBuff(buffDef, _owner);
            }
        }
    }
}
