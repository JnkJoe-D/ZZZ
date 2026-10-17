using UnityEngine;
using cfg.ZZZ;

namespace Game.GamePlay
{
    /// <summary>
    /// 统一动作打断与抗打断韧性裁决辅助类 (基于 Luban TbSkill.Resilience)
    /// </summary>
    public static class ActionResilienceHelper
    {
        /// <summary>
        /// 获取实体当前正在播放的动作在 Luban 技能表中的打断力与抗打断韧性配置
        /// </summary>
        public static ActionResilience GetCurrentActionResilience(CharacterEntity entity)
        {
            if (entity?.ActionPlayer?.CurrentAction == null) return null;
            return GetActionResilienceById(entity.ActionPlayer.CurrentAction.ID);
        }

        /// <summary>
        /// 根据动作/技能 ID 获取其在 Luban 技能表中的打断力与抗打断韧性配置
        /// </summary>
        public static ActionResilience GetActionResilienceById(int actionId)
        {
            if (actionId <= 0) return null;
            var tables = ConfigManager.Instance?.Tables;
            if (tables == null) return null;
            var skillCfg = tables.TbSkill.GetOrDefault(actionId);
            return skillCfg?.Resilience;
        }

        /// <summary>
        /// 获取实体当前动作附带的打断等级。若未配置或读不到严格返回 0，严禁擅自篡改为默认值。
        /// </summary>
        public static int GetInterruptLevel(CharacterEntity attacker)
        {
            var resilience = GetCurrentActionResilience(attacker);
            return resilience != null ? resilience.InterruptLevel : 0;
        }

        /// <summary>
        /// 根据动作 ID 获取附带的打断等级。若未配置或读不到严格返回 0。
        /// </summary>
        public static int GetInterruptLevelById(int actionId)
        {
            var resilience = GetActionResilienceById(actionId);
            return resilience != null ? resilience.InterruptLevel : 0;
        }

        /// <summary>
        /// 计算实体的当前实时总抗打断韧性：
        /// TotalResilience = Resilience (属性最终值，含基础值与 Buff 加成) + ActionResilienceBonus (当前出招动作配表加成)
        /// </summary>
        public static int GetTotalResilience(CharacterEntity victim)
        {
            if (victim == null) return 0;

            float attrResilience = 0f;

            if (victim.StatusModule?.Attributes != null)
            {
                var resAttr = victim.StatusModule.Attributes.Get(AttributeId.Resilience);
                if (resAttr != null)
                {
                    attrResilience = resAttr.FinalValue;
                }
            }

            var actionRes = GetCurrentActionResilience(victim);
            float actionBonus = actionRes != null ? actionRes.ResilienceBonus : 0f;

            return Mathf.Max(0, Mathf.RoundToInt(attrResilience + actionBonus));
        }
    }
}
