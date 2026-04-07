using System;
using System.Collections.Generic;
using Game.Logic.Action.Config;
using Game.Logic.Character;
using Game.Logic.Character.Config;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    public enum ComboTriggerMode
    {
        Buffered,
        InstantOnly,
        BufferedAndInstant
    }

    internal static class CommandRouteEvaluator
    {
        public static bool MatchesCommand(
            InputCommand requiredType,
            CommandPhase requiredPhase,
            CharacterCommand command)
        {
            return command != null &&
                   command.Type != InputCommand.None &&
                   command.Type == requiredType &&
                   command.Phase == requiredPhase;
        }

        public static bool MatchesTriggerMode(ComboTriggerMode triggerMode, bool isBuffered)
        {
            return !(isBuffered && triggerMode == ComboTriggerMode.InstantOnly);
        }

        public static bool MatchesConditions(List<ITransitionCondition> extraConditions, CharacterEntity actor)
        {
            if (extraConditions == null)
            {
                return true;
            }

            foreach (ITransitionCondition condition in extraConditions)
            {
                if (condition != null && !condition.Check(actor))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public class ContextRoute
    {
        [Header("Trigger Command")]
        public InputCommand RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;

        [Header("Next Action")]
        [Tooltip("Leave empty to let the command resolver choose a context-sensitive action variant.")]
        public ActionConfigAsset NextAction;

        [Header("Extra Conditions")]
        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        [Tooltip("Buffer只在Buffer结束触发,Instantly只在Execute/RecoveryExecute期间触发")]
        public ComboTriggerMode TriggerMode = ComboTriggerMode.Buffered;

        [Tooltip("Higher values win when multiple valid commands compete. Same priority prefers the latest input.")]
        public int Priority = 0;

        public bool Evaluate(CharacterCommand command, bool isBuffered, CharacterEntity actor)
        {
            if (!CommandRouteEvaluator.MatchesCommand(RequiredType, RequiredPhase, command))
            {
                return false;
            }

            if (!CommandRouteEvaluator.MatchesTriggerMode(TriggerMode, isBuffered))
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }
    }
}
