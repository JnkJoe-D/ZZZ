using System;
using System.Collections.Generic;
using Game.Input;
using Game.Logic;
using Game.Framework;
using UnityEngine;

namespace Game.Logic
{
    public enum RouteTriggerCategory
    {
        PlayerCommand = 0,
        SingleModifier = 10,
        Event = 20
    }

    public enum RouteEventType
    {
        None = 0,
        SwitchIn = 10,
        SwitchOut = 20,
    }

    public enum ModifierCategory
    {
        None = 0,
        KeyState = 10,
        Condition = 20
    }

    public enum SpeedMultiplierType
    {
        None = 0,
        Jog = 10,
        Dash = 20,
        Dodge = 30,
        Attack = 40,
        Skill = 50
    }

    public enum RouteSingleModifierCheckTiming
    {
        EveryFrameInWindow = 0,
        OnWindowEnter = 10,
        OnWindowExit = 20,
    }

    public enum ExecuteTarget
    {
        None = 0,
        Action = 10,
        Event = 20,
    }
    public enum ExecuteEvent
    {
        None = 0,
        SwitchCaptureSucceed = 10,
    }

    [Serializable]
    public class ActionRoute
    {
        [Header("Trigger")]
        public RouteTriggerCategory Category;
        public string RequiredWindowTag = "Execute1";

        [ShowIf("Category", RouteTriggerCategory.PlayerCommand)]
        public CommandTriggerMode TriggerMode = CommandTriggerMode.OnWindowExit;

        [Header("Modifier Trigger")]
        [ShowIf("Category", RouteTriggerCategory.SingleModifier)]
        public RouteSingleModifierCheckTiming ModifierCheckTiming = RouteSingleModifierCheckTiming.OnWindowExit;

        [Header("Event Trigger")]
        [ShowIf("Category", RouteTriggerCategory.Event)]
        public RouteEventType EventType = RouteEventType.None;

        [Header("Player Command Trigger")]
        [ShowIf("Category", RouteTriggerCategory.PlayerCommand)]
        public InputCommand RequiredType;

        [ShowIf("Category", RouteTriggerCategory.PlayerCommand)]
        public CommandPhase RequiredPhase = CommandPhase.Started;

        [Header("Modifier")]
        public ModifierCategory Modifier = ModifierCategory.None;

        [ShowIf("Modifier", ModifierCategory.KeyState)]
        public InputCommand ModifierRequiredKey;

        [ShowIf("Modifier", ModifierCategory.KeyState)]
        public bool InverseKeyStatus = false;

        [ShowIf("Modifier", ModifierCategory.Condition)]
        public List<ConditionCommand> ModifierConditions = new();

        [Header("Execution Target")]
        public ExecuteTarget ExecuteType = ExecuteTarget.Action;

        [ShowIf("ExecuteType", ExecuteTarget.Action)]
        public ActionConfigAsset ExecuteAction;

        [ShowIf("ExecuteType", ExecuteTarget.Event)]
        public ExecuteEvent RouteExecuteEvent;

        [Header("Extra Conditions")]
        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        [Header("Execution")]
        public int Priority;

        public bool HasModifier => Modifier != ModifierCategory.None;

        public bool EvaluatePlayerCommand(
            CharacterCommand command,
            string activeWindowTag,
            CommandTriggerMode evaluationMode,
            CharacterEntity actor)
        {
            if (Category != RouteTriggerCategory.PlayerCommand)
            {
                return false;
            }

            if (!MatchesWindowTag(activeWindowTag))
            {
                return false;
            }

            if (!CommandRouteEvaluator.MatchesTriggerMode(TriggerMode, evaluationMode))
            {
                return false;
            }

            if (!CommandRouteEvaluator.MatchesCommand(RequiredType, RequiredPhase, command))
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        public bool EvaluateConditionTrigger(
            CharacterEntity actor,
            string activeWindowTag,
            RouteSingleModifierCheckTiming evaluationTiming)
        {
            if (Category != RouteTriggerCategory.SingleModifier)
            {
                return false;
            }

            if (!MatchesWindowTag(activeWindowTag))
            {
                return false;
            }

            if (ModifierCheckTiming != evaluationTiming)
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        public bool EvaluateEvent(
            RouteEventType eventType,
            CharacterEntity actor,
            string activeWindowTag = null)
        {
            if (Category != RouteTriggerCategory.Event)
            {
                return false;
            }

            if (EventType != eventType)
            {
                return false;
            }

            if (!MatchesWindowTag(activeWindowTag))
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        public bool EvaluateModifier(CharacterEntity actor, string activeWindowTag = null)
        {
            switch (Modifier)
            {
                case ModifierCategory.None:
                    return true;

                case ModifierCategory.Condition:
                    return EvaluateModifierConditions(actor);

                case ModifierCategory.KeyState:
                    if (actor?.InputProvider == null)
                        return false;
                    return actor.InputProvider.IsHeld((int)ModifierRequiredKey) || InverseKeyStatus;

                default:
                    return true;
            }
        }

        public bool MatchesCommand(InputCommand commandType, CommandPhase commandPhase)
        {
            return Category == RouteTriggerCategory.PlayerCommand &&
                   RequiredType == commandType &&
                   RequiredPhase == commandPhase;
        }

        public bool IsInvalid()
        {
            if(ExecuteType == ExecuteTarget.None
            || (ExecuteType == ExecuteTarget.Action && ExecuteAction == null)
            || (ExecuteType == ExecuteTarget.Event && RouteExecuteEvent == ExecuteEvent.None))
            {
                return false;
            }
            return true;
        }

        private bool EvaluateModifierConditions(CharacterEntity actor)
        {
            if (ModifierConditions == null || ModifierConditions.Count == 0)
            {
                return true;
            }

            foreach (ConditionCommand condition in ModifierConditions)
            {
                if (!CheckCondition(condition, actor))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckCondition(ConditionCommand condition, CharacterEntity entity)
        {
            if (entity == null)
            {
                return false;
            }

            return condition switch
            {
                ConditionCommand.None => true,
                ConditionCommand.Move => entity.InputProvider != null && entity.InputProvider.HasMovementInput(),
                ConditionCommand.ShortMove => entity.RuntimeData != null && entity.RuntimeData.IsShortMoveInput,
                ConditionCommand.LostMove => entity.InputProvider != null && !entity.InputProvider.HasMovementInput(),
                ConditionCommand.SwitchOutPending => entity.RuntimeData != null && entity.RuntimeData.IsSwitchOutPending, 
                _ => false
            };
        }

        private bool MatchesWindowTag(string activeWindowTag)
        {
            return string.Equals(RequiredWindowTag, activeWindowTag, StringComparison.Ordinal);
        }
    }
}
