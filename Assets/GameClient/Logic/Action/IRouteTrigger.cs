using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    public interface IRouteTrigger
    {
        bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow);
    }

    [Serializable]
    public class IntentCommandTrigger : IRouteTrigger
    {
        public HardwareInputType RequiredInput;
        public CommandPhase RequiredPhase;
        public string RequiredWindowTag = "Execute1";
        public CommandTriggerMode TriggerMode = CommandTriggerMode.OnWindowExit;

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not InputPayload inputPayload) return false;
            
            if (TriggerMode == CommandTriggerMode.Instant && timing != RouteSingleModifierCheckTiming.EveryFrameInWindow) return false;
            if (TriggerMode == CommandTriggerMode.OnWindowExit && timing != RouteSingleModifierCheckTiming.OnWindowExit) return false;
            
            if (inputPayload.InputType != RequiredInput) return false;
            if (inputPayload.Phase != RequiredPhase) return false;

            if (!string.IsNullOrEmpty(RequiredWindowTag) && RequiredWindowTag != windowTag)
            {
                return false;
            }

            if (Modifiers != null)
            {
                foreach (var mod in Modifiers)
                {
                    if (!mod.Evaluate(actor)) return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public class DirectAssetTrigger : IRouteTrigger
    {
        public string RequiredWindowTag = "Execute1";

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not DirectAssetPayload) return false;
            
            if (!string.IsNullOrEmpty(RequiredWindowTag) && RequiredWindowTag != windowTag)
                return false;

            return true;
        }
    }

    [Serializable]
    public class SystemEventTrigger : IRouteTrigger
    {
        public RouteEventType EventType;
        public string RequiredWindowTag;
        
        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not SystemEventPayload eventPayload) return false;
            
            if (eventPayload.EventType != EventType) return false;

            if (!string.IsNullOrEmpty(RequiredWindowTag) && RequiredWindowTag != windowTag)
                return false;

            if (Modifiers != null)
            {
                foreach (var mod in Modifiers)
                {
                    if (!mod.Evaluate(actor)) return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public class AutoTransitionTrigger : IRouteTrigger
    {
        public RouteSingleModifierCheckTiming Timing = RouteSingleModifierCheckTiming.OnWindowExit;
        public string RequiredWindowTag = "Execute1";

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            // Auto transition evaluates when command is NULL
            if (command != null) return false;

            if (!string.IsNullOrEmpty(RequiredWindowTag) && RequiredWindowTag != windowTag)
                return false;
            
            if (Timing != timing)
                return false;

            if (Modifiers != null)
            {
                foreach (var mod in Modifiers)
                {
                    if (!mod.Evaluate(actor)) return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public class ConditionOnlyTrigger : IRouteTrigger
    {
        public RouteSingleModifierCheckTiming Timing = RouteSingleModifierCheckTiming.EveryFrameInWindow;
        public string RequiredWindowTag = "Execute1";

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            // Condition checks evaluate when command is NULL
            if (command != null) return false;

            if (!string.IsNullOrEmpty(RequiredWindowTag) && RequiredWindowTag != windowTag)
                return false;
            
            if (Timing != timing)
                return false;

            // 如果没有任何条件，直接返回 false（既然是 ConditionOnly，没条件就不应该触发，以防意外当作 AutoTransition）
            if (Modifiers == null || Modifiers.Count == 0) return false;

            foreach (var mod in Modifiers)
            {
                if (!mod.Evaluate(actor)) return false;
            }

            return true;
        }
    }
}
