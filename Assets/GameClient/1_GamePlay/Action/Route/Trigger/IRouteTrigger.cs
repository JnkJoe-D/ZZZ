using System;
using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public interface IRouteTrigger
    {
        bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow);
    }

    [Serializable]
    [ConditionScope(ConditionScope.Role)]
    public class IntentCommandTrigger : IRouteTrigger
    {
        public HardwareInputType RequiredInput;
        public CommandPhase RequiredPhase;

        [SerializeReference]
        [ComboWindowTag(typeof(BufferRouteWindow), typeof(ExecuteRouteWindow))]
        public RouteWindow RequiredWindow;

        // 仅用于承接历史 YAML 资产反序列化数据，以便数据迁移工具无损升级
        [SerializeField, HideInInspector]
        private string RequiredWindowTag;

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public void UpgradeLegacyData(Func<string, RouteWindow> resolver)
        {
            if (RequiredWindow == null && !string.IsNullOrEmpty(RequiredWindowTag))
            {
                RequiredWindow = resolver?.Invoke(RequiredWindowTag);
            }
        }

        public bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not InputPayload inputPayload) return false;

            if (inputPayload.InputType != RequiredInput) return false;
            if (inputPayload.Phase != RequiredPhase) return false;

            if (RequiredWindow != null)
            {
                if (activeWindow == null || !RequiredWindow.Matches(activeWindow))
                    return false;
            }

            if (Modifiers != null && Modifiers.Count > 0)
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
        [SerializeReference]
        [ComboWindowTag(typeof(BufferRouteWindow), typeof(ExecuteRouteWindow))]
        public RouteWindow RequiredWindow;

        [SerializeField, HideInInspector]
        private string RequiredWindowTag;

        public void UpgradeLegacyData(Func<string, RouteWindow> resolver)
        {
            if (RequiredWindow == null && !string.IsNullOrEmpty(RequiredWindowTag))
            {
                RequiredWindow = resolver?.Invoke(RequiredWindowTag);
            }
        }

        public bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not DirectAssetPayload) return false;
            
            // 如果指定了窗口，必须满足匹配；若处于全局/空闲状态（activeWindow == null）且未指定窗口（RequiredWindow == null），允许直接切入
            if (RequiredWindow != null)
            {
                if (activeWindow == null || !RequiredWindow.Matches(activeWindow))
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public class SystemEventTrigger : IRouteTrigger
    {
        public RouteEventType EventType;

        [SerializeReference]
        [ComboWindowTag(typeof(BufferRouteWindow), typeof(ExecuteRouteWindow))]
        public RouteWindow RequiredWindow;

        [SerializeField, HideInInspector]
        private string RequiredWindowTag;

        public void UpgradeLegacyData(Func<string, RouteWindow> resolver)
        {
            if (RequiredWindow == null && !string.IsNullOrEmpty(RequiredWindowTag))
            {
                RequiredWindow = resolver?.Invoke(RequiredWindowTag);
            }
        }

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (command == null || command.Payload is not SystemEventPayload eventPayload) return false;
            
            if (eventPayload.EventType != EventType) return false;

            if (RequiredWindow != null)
            {
                if (activeWindow == null || !RequiredWindow.Matches(activeWindow))
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
    public class AutoTransitionTrigger : IRouteTrigger
    {
        public RouteSingleModifierCheckTiming Timing = RouteSingleModifierCheckTiming.OnWindowExit;

        [SerializeReference]
        [ComboWindowTag(typeof(AutoRouteWindow))]
        public RouteWindow RequiredWindow;

        [SerializeField, HideInInspector]
        private string RequiredWindowTag;

        public void UpgradeLegacyData(Func<string, RouteWindow> resolver)
        {
            if (RequiredWindow == null && !string.IsNullOrEmpty(RequiredWindowTag))
            {
                RequiredWindow = resolver?.Invoke(RequiredWindowTag);
            }
        }

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            // Auto transition evaluates when command is NULL
            if (command != null) return false;

            if (RequiredWindow != null)
            {
                if (activeWindow == null || !RequiredWindow.Matches(activeWindow))
                    return false;
            }
            
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
    [ConditionScope(ConditionScope.Role)]
    public class ConditionOnlyTrigger : IRouteTrigger
    {
        public RouteSingleModifierCheckTiming Timing = RouteSingleModifierCheckTiming.EveryFrameInWindow;

        [SerializeReference]
        [ComboWindowTag(typeof(AutoRouteWindow))]
        public RouteWindow RequiredWindow;

        [SerializeField, HideInInspector]
        private string RequiredWindowTag;

        public void UpgradeLegacyData(Func<string, RouteWindow> resolver)
        {
            if (RequiredWindow == null && !string.IsNullOrEmpty(RequiredWindowTag))
            {
                RequiredWindow = resolver?.Invoke(RequiredWindowTag);
            }
        }

        public List<RouteModifierCheck> Modifiers = new List<RouteModifierCheck>();

        public bool Evaluate(CharacterCommand command, RouteWindow activeWindow, CharacterEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            // Condition checks evaluate when command is NULL
            if (command != null) return false;

            if (RequiredWindow != null)
            {
                if (activeWindow == null || !RequiredWindow.Matches(activeWindow))
                    return false;
            }
            
            if (Timing != timing)
                return false;

            // 如果没有任何条件，直接返回 false
            if (Modifiers == null || Modifiers.Count == 0) return false;

            foreach (var mod in Modifiers)
            {
                if (!mod.Evaluate(actor)) return false;
            }

            return true;
        }
    }
}
