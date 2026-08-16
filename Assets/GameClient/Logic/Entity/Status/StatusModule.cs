using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 状态模块 — 桥接门面。
    /// 组合 AttributeSet 和 BuffContainer，提供统一初始化和 Tick 入口。
    /// 全面接入 Luban 数据驱动架构，由多子表 (Base, Energy, Element, Special) 自动装配。
    /// </summary>
    public class StatusModule
    {
        public AttributeSet Attributes { get; } = new();
        public BuffContainer Buffs { get; } = new();

        private RoleEntity _owner;
        private readonly HashSet<string> _immuneTags = new();

        /// <summary>
        /// 基于角色配置（通过 Luban 配置表装配）进行初始化。
        /// </summary>
        public void Init(RoleEntity owner, CharacterConfigAsset config, int level = 1)
        {
            _owner = owner;
            Attributes.Init(owner);
            Buffs.Init(owner);
            _immuneTags.Clear();

            if (config == null) return;

            var charId = (cfg.ZZZ.CharacterId)config.ID;
            InitFromConfig(owner, charId, level);
        }

        /// <summary>
        /// 从 Luban 数据表装配角色的所有基础、能量、元素和独有特化属性。
        /// </summary>
        public void InitFromConfig(RoleEntity owner, cfg.ZZZ.CharacterId characterId, int level = 1)
        {
            _owner = owner;
            Attributes.Init(owner);
            Buffs.Init(owner);
            _immuneTags.Clear();

            var tables = ConfigManager.Instance?.Tables;
            if (tables == null)
            {
                Debug.LogWarning($"[StatusModule] ConfigManager.Tables 尚未初始化，无法为角色 {characterId} 加载配置！");
                return;
            }

            // 1. 基础攻防属性 (TbCharacterBase)
            if (tables.TbCharacterBase.DataMap.TryGetValue(characterId, out var baseCfg))
            {
                float hp = baseCfg.HpBase + baseCfg.HpGrowth * (level - 1);
                float atk = baseCfg.AtkBase + baseCfg.AtkGrowth * (level - 1);
                float def = baseCfg.DefBase + baseCfg.DefGrowth * (level - 1);
                float impact = baseCfg.ImpactBase + baseCfg.ImpactGrowth * (level - 1);

                Attributes.Register(new AttributeInstance(AttributeId.MaxHp, hp, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.HP, hp, 0f, hp));
                Attributes.Register(new AttributeInstance(AttributeId.ATK, atk, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.DEF, def, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Impact, impact, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.StunDmgMultiplier, baseCfg.StunDmgMult));
                Attributes.Register(new AttributeInstance(AttributeId.CritRate, baseCfg.CritRateBase, 0f, 1f));
                Attributes.Register(new AttributeInstance(AttributeId.CritDmg, baseCfg.CritDmgBase, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.PenRate, baseCfg.PenRateBase, 0f, 1f));
                Attributes.Register(new AttributeInstance(AttributeId.PenValue, baseCfg.PenValBase, 0f, float.MaxValue));
            }
            else
            {
                Debug.LogWarning($"[StatusModule] 找不到角色 {characterId} 的基础配置 (TbCharacterBase)！使用保底数据。");
                Attributes.Register(new AttributeInstance(AttributeId.MaxHp, 10000f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.HP, 10000f, 0f, 10000f));
                Attributes.Register(new AttributeInstance(AttributeId.ATK, 1000f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.DEF, 500f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Impact, 100f, 0f, float.MaxValue));
            }

            // 2. 能量与喧响机制 (TbCharacterEnergy)
            if (tables.TbCharacterEnergy.DataMap.TryGetValue(characterId, out var energyCfg))
            {
                Attributes.Register(new AttributeInstance(AttributeId.MaxEnergy, energyCfg.EnergyMax, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Energy, energyCfg.EnergyInit, 0f, energyCfg.EnergyMax, energyCfg.EnergyRegenSec));
                Attributes.Register(new AttributeInstance(AttributeId.EnergyRegen, energyCfg.EnergyRegenSec, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.EnergyGenRate, energyCfg.EnergyGenRate, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Decibel, 0f, 0f, 3000f));
                Attributes.Register(new AttributeInstance(AttributeId.DecibelGenRate, energyCfg.DecibelGenRate, 0f, float.MaxValue));
            }
            else
            {
                Debug.LogWarning($"[StatusModule] 找不到角色 {characterId} 的能量配置 (TbCharacterEnergy)！使用保底数据。");
                Attributes.Register(new AttributeInstance(AttributeId.MaxEnergy, 120f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Energy, 0f, 0f, 120f, 1.2f));
                Attributes.Register(new AttributeInstance(AttributeId.EnergyRegen, 1.2f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.EnergyGenRate, 1f, 0f, float.MaxValue));
                Attributes.Register(new AttributeInstance(AttributeId.Decibel, 0f, 0f, 3000f));
                Attributes.Register(new AttributeInstance(AttributeId.DecibelGenRate, 1f, 0f, float.MaxValue));
            }

            // 3. 元素属性与抗性 (TbCharacterElement)
            if (tables.TbCharacterElement.DataMap.TryGetValue(characterId, out var elemCfg))
            {
                // 元素抗性与属性掌控预留
            }

            // 4. 角色特化专属机制、免疫标签与初始Buff (TbCharacterSpecial)
            if (tables.TbCharacterSpecial.DataMap.TryGetValue(characterId, out var specialCfg))
            {
                if (specialCfg.ImmuneTags != null)
                {
                    for (int i = 0; i < specialCfg.ImmuneTags.Count; i++)
                    {
                        _immuneTags.Add(specialCfg.ImmuneTags[i]);
                    }
                }

                if (specialCfg.CustomAttributes != null)
                {
                    for (int i = 0; i < specialCfg.CustomAttributes.Count; i++)
                    {
                        var custom = specialCfg.CustomAttributes[i];
                        Attributes.Register(new AttributeInstance((AttributeId)custom.AttrId, custom.InitVal, custom.MinVal, custom.MaxVal));
                    }
                }

                if (specialCfg.InitialBuffs != null)
                {
                    for (int i = 0; i < specialCfg.InitialBuffs.Count; i++)
                    {
                        int buffId = specialCfg.InitialBuffs[i];
                        // 预留给 Luban Buff 表接入
                    }
                }
            }

            Debug.Log($"<color=green>[StatusModule] 角色 {characterId} 成功通过 Luban 数据表初始化属性系统！</color>");
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
            _immuneTags.Clear();
        }

        /// <summary>
        /// 检查 Buff 标签是否被免疫。
        /// </summary>
        public bool IsTagImmune(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            return _immuneTags.Contains(tag);
        }

        /// <summary>
        /// 检查一个 Buff 是否被免疫（其任一标签命中免疫列表即免疫）。
        /// </summary>
        public bool IsBuffImmune(BuffDefAsset buffDef)
        {
            if (buffDef == null || buffDef.Tags == null || buffDef.Tags.Count == 0) return false;

            for (int i = 0; i < buffDef.Tags.Count; i++)
            {
                if (IsTagImmune(buffDef.Tags[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取指定技能（如强化特殊技）的能量消耗阈值
        /// </summary>
        public float GetEXSpecialAttackCost(int skillId)
        {
            if (skillId <= 0) return 0f;
            
            var tables = ConfigManager.Instance?.Tables;
            if (tables == null) return 0f;
            
            var skillConfig = tables.TbSkill.GetOrDefault(skillId);
            if (skillConfig == null) return 0f;

            // 优先从消耗列表 (Cost) 中查找对应能量 (Energy) 的数值
            if (skillConfig.Cost != null)
            {
                foreach (var cost in skillConfig.Cost)
                {
                    if ((int)cost.AttrId == (int)AttributeId.Energy && cost.Amount > 0)
                    {
                        return cost.Amount;
                    }
                }
            }

            // 如果 Cost 未配但 Condition 配置了限制要求，也作为阈值提取
            if (skillConfig.Condition != null)
            {
                foreach (var cond in skillConfig.Condition)
                {
                    if ((int)cond.AttrId == (int)AttributeId.Energy && cond.Value > 0)
                    {
                        return cond.Value;
                    }
                }
            }

            return 0f;
        }
    }
}
