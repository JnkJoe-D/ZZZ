using System;
using UnityEngine;

namespace Game.Logic
{
    public interface IRouteInputCondition
    {
        bool Check(RoleEntity actor);
    }

    [Serializable]
    public class MoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && actor.InputProvider.HasMovementInput();
        }
    }

    [Serializable]
    public class ShortMoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.DataModule?.Get<ActionRuntimeData>() != null && actor.DataModule.Get<ActionRuntimeData>().IsShortMoveInput;
        }
    }

    [Serializable]
    public class LostMoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && !actor.InputProvider.HasMovementInput();
        }
    }
}
