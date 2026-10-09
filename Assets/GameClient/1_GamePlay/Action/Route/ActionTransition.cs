using System.Collections.Generic;

namespace Game.GamePlay
{
    public static class CommandRouteEvaluator
    {
        /// <summary>
        /// 评估额外条件集合，支持 AND 与 OR 逻辑组合
        /// </summary>
        public static bool MatchesConditions(
            List<ITransitionCondition> extraConditions,
            CharacterEntity actor,
            ConditionCombineMode combineMode = ConditionCombineMode.AllMatch_AND)
        {
            if (extraConditions == null || extraConditions.Count == 0)
            {
                return true;
            }

            if (combineMode == ConditionCombineMode.AllMatch_AND)
            {
                for (int i = 0; i < extraConditions.Count; i++)
                {
                    ITransitionCondition condition = extraConditions[i];
                    if (condition != null && !condition.Check(actor))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // AnyMatch_OR
            {
                for (int i = 0; i < extraConditions.Count; i++)
                {
                    ITransitionCondition condition = extraConditions[i];
                    if (condition != null && condition.Check(actor))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 评估输入修饰条件集合 (RouteModifierCheck)，支持 AND 与 OR 逻辑组合
        /// </summary>
        public static bool MatchesInputModifiers(
            List<RouteModifierCheck> inputConditions,
            CharacterEntity actor,
            ConditionCombineMode combineMode = ConditionCombineMode.AllMatch_AND)
        {
            if (inputConditions == null || inputConditions.Count == 0)
            {
                return true;
            }

            if (combineMode == ConditionCombineMode.AllMatch_AND)
            {
                for (int i = 0; i < inputConditions.Count; i++)
                {
                    RouteModifierCheck mod = inputConditions[i];
                    if (mod != null && !mod.Evaluate(actor))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // AnyMatch_OR
            {
                for (int i = 0; i < inputConditions.Count; i++)
                {
                    RouteModifierCheck mod = inputConditions[i];
                    if (mod != null && mod.Evaluate(actor))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
