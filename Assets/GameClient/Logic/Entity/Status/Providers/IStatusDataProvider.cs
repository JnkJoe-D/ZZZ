using System.Collections.Generic;

namespace Game.Logic
{
    public interface IStatusDataProvider
    {
        IEnumerable<AttributeInstance> GetInitialAttributes(int level);
        IEnumerable<string> GetImmuneTags();
        float GetEXSpecialAttackCost(int skillId);
    }
}
