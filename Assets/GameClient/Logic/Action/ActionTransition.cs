using System;
using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    internal static class CommandRouteEvaluator
    {
        public static bool MatchesCommand(
            HardwareInputType requiredType,
            CommandPhase requiredPhase,
            CharacterCommand command)
        {
            return command != null &&
                   command.Payload is InputPayload inputPayload &&
                   inputPayload.InputType != HardwareInputType.None &&
                   inputPayload.InputType == requiredType &&
                   inputPayload.Phase == requiredPhase;
        }

        public static bool MatchesTriggerMode(CommandTriggerMode triggerMode, CommandTriggerMode evaluationMode)
        {
            return triggerMode == evaluationMode;
        }

        public static bool MatchesConditions(List<ITransitionCondition> extraConditions, RoleEntity actor)
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
        public HardwareInputType RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;
        public ActionConfigAsset NextAction;

        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        public CommandTriggerMode TriggerMode = CommandTriggerMode.OnWindowExit;
        public int Priority;

        public bool Evaluate(CharacterCommand command, CommandTriggerMode evaluationMode, RoleEntity actor)
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
