using UnityEngine;

namespace Game.Logic.Character
{
    public enum CommandType
    {
        None = 0,
        Move = 10,
        BasicAttack = 20,
        SpecialAttack = 30,
        Ultimate = 40,
        Evade = 50
    }

    public enum CommandPhase
    {
        Started = 0,
        Performed = 10,
        Canceled = 20
    }

    public struct CommandPayload
    {
        public Vector2 DirectionSnapshot;
        public bool HasMovementInput;
    }
}
