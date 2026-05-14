using UnityEngine;

namespace Game.Logic.Character
{
    public enum InputCommand
    {
        None = 0,
        Move = 10,
        BasicAttack = 20,
        SpecialAttack = 30,
        Ultimate = 40,
        Evade = 50,
        Switch = 60,
        Interact = 70,
    }
    public enum ConditionCommand
    {
        None = 0,
        Move = 10,
        LostMove = 20,
        ShortMove = 30
    }

    public enum CommandPhase
    {
        Started = 0,
        Performed = 10,
        Canceled = 20,
        Held = 30,
    }

    public struct CommandPayload
    {
        public Vector2 DirectionSnapshot;
        public bool HasMovementInput;
    }
}
