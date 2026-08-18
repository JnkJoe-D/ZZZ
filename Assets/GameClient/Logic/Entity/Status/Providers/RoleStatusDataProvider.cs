using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    public class RoleStatusDataProvider : IStatusDataProvider
    {
        private readonly cfg.ZZZ.CharacterId _characterId;

        public RoleStatusDataProvider(cfg.ZZZ.CharacterId characterId)
        {
            _characterId = characterId;
        }

        public IEnumerable<AttributeInstance> GetInitialAttributes(int level)
        {
            var tables = ConfigManager.Instance?.Tables;
            if (tables == null) yield break;

            if (tables.TbCharacterBase.DataMap.TryGetValue(_characterId, out var baseCfg))
            {
                float hp = baseCfg.HpBase + baseCfg.HpGrowth * (level - 1);
                yield return new AttributeInstance(AttributeId.MaxHp, hp, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.HP, hp, 0f, hp);
                yield return new AttributeInstance(AttributeId.ATK, baseCfg.AtkBase + baseCfg.AtkGrowth * (level - 1), 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.DEF, baseCfg.DefBase + baseCfg.DefGrowth * (level - 1), 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Impact, baseCfg.ImpactBase + baseCfg.ImpactGrowth * (level - 1), 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.StunDmgMultiplier, baseCfg.StunDmgMult);
                yield return new AttributeInstance(AttributeId.CritRate, baseCfg.CritRateBase, 0f, 1f);
                yield return new AttributeInstance(AttributeId.CritDmg, baseCfg.CritDmgBase, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.PenRate, baseCfg.PenRateBase, 0f, 1f);
                yield return new AttributeInstance(AttributeId.PenValue, baseCfg.PenValBase, 0f, float.MaxValue);
            }
            else
            {
                Debug.LogWarning($"[StatusModule] 找不到角色 {_characterId} 的基础配置 (TbCharacterBase)！使用保底数据。");
                yield return new AttributeInstance(AttributeId.MaxHp, 10000f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.HP, 10000f, 0f, 10000f);
                yield return new AttributeInstance(AttributeId.ATK, 1000f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.DEF, 500f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Impact, 100f, 0f, float.MaxValue);
            }

            if (tables.TbCharacterEnergy.DataMap.TryGetValue(_characterId, out var energyCfg))
            {
                yield return new AttributeInstance(AttributeId.MaxEnergy, energyCfg.EnergyMax, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Energy, energyCfg.EnergyInit, 0f, energyCfg.EnergyMax, energyCfg.EnergyRegenSec);
                yield return new AttributeInstance(AttributeId.EnergyRegen, energyCfg.EnergyRegenSec, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.EnergyGenRate, energyCfg.EnergyGenRate, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Decibel, 0f, 0f, 3000f);
                yield return new AttributeInstance(AttributeId.DecibelGenRate, energyCfg.DecibelGenRate, 0f, float.MaxValue);
            }
            else
            {
                Debug.LogWarning($"[StatusModule] 找不到角色 {_characterId} 的能量配置 (TbCharacterEnergy)！使用保底数据。");
                yield return new AttributeInstance(AttributeId.MaxEnergy, 120f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Energy, 0f, 0f, 120f, 1.2f);
                yield return new AttributeInstance(AttributeId.EnergyRegen, 1.2f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.EnergyGenRate, 1f, 0f, float.MaxValue);
                yield return new AttributeInstance(AttributeId.Decibel, 0f, 0f, 3000f);
                yield return new AttributeInstance(AttributeId.DecibelGenRate, 1f, 0f, float.MaxValue);
            }

            if (tables.TbCharacterSpecial.DataMap.TryGetValue(_characterId, out var specialCfg))
            {
                yield return new AttributeInstance(AttributeId.BaseResilience, specialCfg.BaseResilience, 0f, float.MaxValue);

                if (specialCfg.CustomAttributes != null)
                {
                    for (int i = 0; i < specialCfg.CustomAttributes.Count; i++)
                    {
                        var custom = specialCfg.CustomAttributes[i];
                        yield return new AttributeInstance((AttributeId)custom.AttrId, custom.InitVal, custom.MinVal, custom.MaxVal);
                    }
                }
            }
        }

        public IEnumerable<string> GetImmuneTags()
        {
            var tables = ConfigManager.Instance?.Tables;
            if (tables != null && tables.TbCharacterSpecial.DataMap.TryGetValue(_characterId, out var specialCfg))
            {
                if (specialCfg.ImmuneTags != null)
                {
                    for (int i = 0; i < specialCfg.ImmuneTags.Count; i++)
                    {
                        yield return specialCfg.ImmuneTags[i];
                    }
                }
            }
        }

        public float GetEXSpecialAttackCost(int skillId)
        {
            if (skillId <= 0) return 0f;
            
            var tables = ConfigManager.Instance?.Tables;
            if (tables == null) return 0f;
            
            var skillConfig = tables.TbSkill.GetOrDefault(skillId);
            if (skillConfig == null) return 0f;

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
