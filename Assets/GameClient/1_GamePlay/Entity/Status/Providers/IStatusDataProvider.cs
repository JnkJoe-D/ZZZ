using System.Collections.Generic;

namespace Game.GamePlay
{
    public interface IStatusDataProvider
    {
        IEnumerable<AttributeInstance> GetInitialAttributes(int level);
        IEnumerable<string> GetImmuneTags();
        float GetEXSpecialAttackCost(int skillId);
    }
}
