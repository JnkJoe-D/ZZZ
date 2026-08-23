using UnityEngine;

namespace Game.Logic
{
    public enum HardwareInputType
    {
        None = 0,
        Move = 10,
        BasicAttack = 20,
        SpecialAttack = 30,
        Ultimate = 40,
        Evade = 50,
        Switch = 60,
        Interact = 70
    }

    public enum ConditionCommand
    {
        None = 0,
        Move = 10,
        LostMove = 20,
        ShortMove = 30,
        SwitchOutPending = 40
    }

    public enum CommandPhase
    {
        Started = 0,
        Performed = 10,
        Canceled = 20,
        Held = 30,
        None = 40,
    }

    public enum CommandFate
    {
        Pending,
        Executed,
        Dropped
    }

    public interface ICommandPayload { }

    public class InputPayload : ICommandPayload
    {
        public HardwareInputType InputType;
        public CommandPhase Phase;
        public Vector2 DirectionSnapshot;
        public bool HasMovementInput;
    }

    public class DirectAssetPayload : ICommandPayload
    {
        public ActionConfigAsset TargetAsset;
    }

    public class SystemEventPayload : ICommandPayload
    {
        public RouteEventType EventType;
    }

    public enum CommandTriggerMode
    {
        OnWindowExit = 0,
        Instant = 1
    }
}
