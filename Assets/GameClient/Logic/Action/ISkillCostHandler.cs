using ATEditor;
using UnityEngine;
using Game.Framework;

namespace Game.Logic
{
    /// <summary>
    /// 技能配置验证与消耗接口，负责解耦 ActionRoute 中的具体消耗和验证逻辑
    /// </summary>
    public interface ISkillCostHandler
    {
        bool CheckSkillRequirement(ActionConfigAsset action, CharacterEntity entity);
        void ConsumeSkillCost(ActionConfigAsset action, CharacterEntity entity);
    }

    /// <summary>
    /// 默认的技能前置验证与消耗处理器（通过实体的 IAttributeResolver 门面多态调度个体属性与小队共享资源，零硬编码特判）
    /// </summary>
    public class DefaultSkillCostHandler : ISkillCostHandler
    {
        public bool CheckSkillRequirement(ActionConfigAsset action, CharacterEntity entity)
        {
            if (action == null || action.ID <= 0 || entity == null) return true;

            var resolver = entity.AttributeResolver;
            if (resolver == null) return true;

            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(action.ID);
            if (skillConfig == null || skillConfig.Condition == null) return true;

            foreach (var cond in skillConfig.Condition)
            {
                float currentVal = resolver.GetAttribute((AttributeId)cond.AttrId);

                bool pass = cond.Op switch
                {
                    cfg.ZZZ.CompareOp.GreaterEqual => currentVal >= cond.Value,
                    cfg.ZZZ.CompareOp.Greater => currentVal > cond.Value,
                    cfg.ZZZ.CompareOp.LessEqual => currentVal <= cond.Value,
                    cfg.ZZZ.CompareOp.Less => currentVal < cond.Value,
                    cfg.ZZZ.CompareOp.Equal => Mathf.Approximately(currentVal, cond.Value),
                    _ => true
                };
                if (!pass) return false;
            }

            return true;
        }

        public void ConsumeSkillCost(ActionConfigAsset action, CharacterEntity entity)
        {
            if (action == null || action.ID <= 0 || entity == null) return;

            var resolver = entity.AttributeResolver;
            if (resolver == null) return;

            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(action.ID);
            if (skillConfig == null || skillConfig.Cost == null) return;

            foreach (var cost in skillConfig.Cost)
            {
                if (Mathf.Abs(cost.Amount) > 0.0001f)
                {
                    // 正数消耗扣除，负数回复加算（delta = -amount）
                    float delta = -cost.Amount;
                    resolver.ModifyAttribute((AttributeId)cost.AttrId, delta);
                    Debug.Log($"<color=cyan>[SkillCost] {entity.name} 动作 '{action.name}'(ID:{action.ID}) 触发属性 {cost.AttrId} 变动: {(delta >= 0 ? "+" + delta : delta.ToString())}</color>");
                }
            }
        }
    }
}
