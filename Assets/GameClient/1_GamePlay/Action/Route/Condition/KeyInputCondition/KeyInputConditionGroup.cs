using System;
using System.Collections.Generic;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 按键条件组合组 (Composite Pattern)
    /// 允许将多个 IKeyInputCondition 组合为一棵逻辑树，支持 AND / OR 嵌套与整体取反
    /// </summary>
    [Serializable]
    [SubclassDisplayName("条件组合组")]
    public sealed class KeyInputConditionGroup : IKeyInputCondition
    {
        [Tooltip("子条件集合之间的逻辑运算符")]
        public ConditionCombineMode CombineMode = ConditionCombineMode.AllMatch_AND;

        [SerializeReference, SubclassSelector]
        public List<IKeyInputCondition> SubConditions = new();
        public bool Check(RoleEntity actor)
        {
            if (SubConditions == null || SubConditions.Count == 0)
            {
                return false;
            }

            bool result;
            if (CombineMode == ConditionCombineMode.AllMatch_AND)
            {
                result = true;
                for (int i = 0; i < SubConditions.Count; i++)
                {
                    if (SubConditions[i] != null && !SubConditions[i].Check(actor))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else // AnyMatch_OR
            {
                result = false;
                for (int i = 0; i < SubConditions.Count; i++)
                {
                    if (SubConditions[i] != null && SubConditions[i].Check(actor))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
