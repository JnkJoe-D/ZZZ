using System.Collections.Generic;

namespace Game.GamePlay
{
    internal static class CommandRouteEvaluator
    {
        public static bool MatchesConditions(List<ITransitionCondition> extraConditions, RoleEntity actor)
        {
            if (extraConditions == null || extraConditions.Count == 0)
            {
                return true;
            }

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
    }
}
