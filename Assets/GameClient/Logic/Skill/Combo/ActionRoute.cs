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
    public class RouteModifierCheck
    {
        public ModifierCategory Category = ModifierCategory.None;

        [ShowIf("Category", ModifierCategory.KeyState)]
        public InputCommand RequiredKey;

        [ShowIf("Category", ModifierCategory.Condition)]
        public ConditionCommand Condition = ConditionCommand.None;

        public bool Inverse = false;

        public bool Evaluate(CharacterEntity actor)
        {
            switch (Category)
            {
                case ModifierCategory.None:
                    return true;
                case ModifierCategory.Condition:
                    bool conditionResult = CheckCondition(Condition, actor);
                    return Inverse ? !conditionResult : conditionResult;
                case ModifierCategory.KeyState:
                    if (actor?.InputProvider == null)
                        return false;
                    bool isHeld = actor.InputProvider.IsHeld((int)RequiredKey);
                    return Inverse ? !isHeld : isHeld;
                default:
                    return true;
            }
        }

        private bool CheckCondition(ConditionCommand condition, CharacterEntity entity)
        {
            if (entity == null) return false;
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
    }

    [Serializable]
    public class ActionRoute : ISerializationCallbackReceiver
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

        [Header("Modifiers")]
        public List<RouteModifierCheck> Modifiers = new();

        [HideInInspector, SerializeField]
        private ModifierCategory Modifier = ModifierCategory.None;

        [HideInInspector, SerializeField]
        private InputCommand ModifierRequiredKey;

        [HideInInspector, SerializeField]
        private bool InverseKeyStatus = false;

        [HideInInspector, SerializeField]
        private List<ConditionCommand> ModifierConditions;

        [Header("Execution Target")]
        public ExecuteTarget ExecuteType = ExecuteTarget.Action;

        [ShowIf("ExecuteType", ExecuteTarget.Action)]
        public ActionConfigAsset ExecuteAction;

        [ShowIf("ExecuteType", ExecuteTarget.Event)]
        public ExecuteEvent RouteExecuteEvent;

        [Header("Extra Conditions")]
        [SerializeReference, SubclassSelector]
        public List<ITransitionCondition> ExtraConditions = new();

        [Header("Execute Effects")]
        [Tooltip("路由被选中执行时的附加副作用（消耗属性、施加 Buff 等）。")]
        [SerializeReference, SubclassSelector]
        public List<IRouteEffect> OnExecuteEffects = new();

        [Header("Execution")]
        public int Priority;

        public bool HasModifier => Modifiers != null && Modifiers.Count > 0;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (Modifier != ModifierCategory.None)
            {
                if (Modifiers == null) Modifiers = new List<RouteModifierCheck>();
                Modifiers.Add(new RouteModifierCheck
                {
                    Category = Modifier,
                    RequiredKey = ModifierRequiredKey,
                    Inverse = InverseKeyStatus,
                    Condition = (ModifierConditions != null && ModifierConditions.Count > 0) ? ModifierConditions[0] : ConditionCommand.None
                });
                Modifier = ModifierCategory.None;
            }
        }

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

            bool conditionResult = CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
            if (!conditionResult && ExecuteAction != null && ExecuteAction.Name.Contains("Attack"))
            {
                Debug.Log($"<color=orange>[RouteTrace] {ExecuteAction.Name} 的 ExtraConditions 检查未通过！</color>");
            }
            return conditionResult;
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
            if (Modifiers == null || Modifiers.Count == 0) return true;
            foreach (var mod in Modifiers)
            {
                if (!mod.Evaluate(actor)) return false;
            }
            return true;
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



        private bool MatchesWindowTag(string activeWindowTag)
        {
            return string.Equals(RequiredWindowTag, activeWindowTag, StringComparison.Ordinal);
        }
    }
}
