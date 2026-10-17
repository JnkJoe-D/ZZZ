using System;
using UnityEngine;

namespace Game.GamePlay
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

    public enum CommandRouteSource
    {
        None = 0,
        ActionRoute = 10,
        ActionComplete = 20
    }

    public class CharacterCommand
    {
        public long Id;
        public ICommandPayload Payload;
        public float Timestamp;
        public long BufferOrder;
        public bool IsConsumed;
    }

    public interface ICommandPayload { }

    public class InputPayload : ICommandPayload
    {
        public HardwareInputType InputType;
        public CommandPhase Phase;
        public Vector2 DirectionSnapshot;
        public bool HasMovementInput;
    }

    public class DirectAssetPayload : CharacterActionPlayInfo
    {
        public ActionConfigAsset TargetAsset
        {
            get => ActionConfig;
            set => ActionConfig = value;
        }

        public float CrossfadeOverride
        {
            get => TransitionInfo?.CrossfadeDuration ?? -1f;
            set
            {
                TransitionInfo ??= new ActionTransitionInfo();
                TransitionInfo.CrossfadeDuration = value;
            }
        }

        public float StartTime
        {
            get => TransitionInfo?.StartTime ?? 0f;
            set
            {
                TransitionInfo ??= new ActionTransitionInfo();
                TransitionInfo.StartTime = value;
            }
        }

        public Action OnComplete
        {
            get => EventInfo?.OnActionComplete;
            set
            {
                EventInfo ??= new ActionEventInfo();
                EventInfo.OnActionComplete = value;
            }
        }

        public Action OnInterrupt
        {
            get => EventInfo?.OnActionInterrupt;
            set
            {
                EventInfo ??= new ActionEventInfo();
                EventInfo.OnActionInterrupt = value;
            }
        }

        public Action OnStart
        {
            get => EventInfo?.OnActionStart;
            set
            {
                EventInfo ??= new ActionEventInfo();
                EventInfo.OnActionStart = value;
            }
        }

        public DirectAssetPayload() { }

        public DirectAssetPayload(ActionConfigAsset action, float crossfade = -1f, float startTime = 0f, Action onComplete = null, Action onInterrupt = null, Action onStart = null)
            : base(action, crossfade, startTime)
        {
            if (onComplete != null || onInterrupt != null || onStart != null)
            {
                EventInfo = new ActionEventInfo
                {
                    OnActionComplete = onComplete,
                    OnActionInterrupt = onInterrupt,
                    OnActionStart = onStart
                };
            }
        }
    }

    public class SystemEventPayload : ICommandPayload
    {
        public RouteEventType EventType;
        public float StartTime = 0f;
    }
}

