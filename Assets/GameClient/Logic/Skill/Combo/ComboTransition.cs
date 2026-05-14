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
        OnWindowExit = 0,
        Instant = 1
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

        public static bool MatchesTriggerMode(ComboTriggerMode triggerMode, ComboTriggerMode evaluationMode)
        {
            return triggerMode == evaluationMode;
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
        public InputCommand RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;
        public ActionConfigAsset NextAction;

        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        public ComboTriggerMode TriggerMode = ComboTriggerMode.OnWindowExit;
        public int Priority;

        public bool Evaluate(CharacterCommand command, ComboTriggerMode evaluationMode, CharacterEntity actor)
        {
            if (!CommandRouteEvaluator.MatchesCommand(RequiredType, RequiredPhase, command))
            {
                return false;
            }

            if (!CommandRouteEvaluator.MatchesTriggerMode(TriggerMode, evaluationMode))
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }
    }
}
