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
        bool CheckSkillRequirement(ActionConfigAsset action, RoleEntity entity);
        void ConsumeSkillCost(ActionConfigAsset action, RoleEntity entity);
    }

    /// <summary>
    /// 默认的技能前置验证与消耗处理器
    /// </summary>
    public class DefaultSkillCostHandler : ISkillCostHandler
    {
        public bool CheckSkillRequirement(ActionConfigAsset action, RoleEntity entity)
        {
            if (action == null || action.ID <= 0) return true;
            if (entity?.StatusModule?.Attributes == null) return true;

            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(action.ID);
            if (skillConfig == null) return true;

            if (skillConfig.Condition != null)
            {
                foreach (var cond in skillConfig.Condition)
                {
                    float currentVal = entity.StatusModule.Attributes.GetCurrent((AttributeId)cond.AttrId);
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
            }
            return true;
        }

        public void ConsumeSkillCost(ActionConfigAsset action, RoleEntity entity)
        {
            if (action == null || action.ID <= 0) return;
            if (entity?.StatusModule?.Attributes == null) return;

            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(action.ID);
            if (skillConfig == null || skillConfig.Cost == null) return;

            foreach (var cost in skillConfig.Cost)
            {
                if (cost.Amount > 0)
                {
                    entity.StatusModule.Attributes.Modify((AttributeId)cost.AttrId, -cost.Amount);
                    Debug.Log($"<color=cyan>[SkillCost] {entity.name} 使用技能 {action.ID} 消耗了 {cost.Amount} 点 {cost.AttrId}</color>");
                }
            }
        }
    }
}
