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
            CommandType requiredType,
            CommandPhase requiredPhase,
            CharacterCommand command)
        {
            return command != null &&
                   command.Type != CommandType.None &&
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
    public class LocalActionRoute
    {
        [Header("Trigger Command")]
        public CommandType RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;

        [Header("Next Action")]
        [Tooltip("Leave empty to let the command resolver choose a context-sensitive action variant.")]
        public ActionConfigAsset NextAction;

        [Header("Extra Conditions")]
        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        [Header("Window Tag")]
        [Tooltip("Only routes matching the active ComboWindow tag can fire.")]
        public string RequiredWindowTag = "Normal";

        [Tooltip("InstantOnly rejects buffered input. Other modes currently accept both live and buffered input.")]
        public ComboTriggerMode TriggerMode = ComboTriggerMode.Buffered;

        public bool Evaluate(CharacterCommand command, string tag, bool isBuffered, CharacterEntity actor)
        {
            if (!CommandRouteEvaluator.MatchesCommand(RequiredType, RequiredPhase, command))
            {
                return false;
            }

            if (!CommandRouteEvaluator.MatchesTriggerMode(TriggerMode, isBuffered))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(RequiredWindowTag) && tag != RequiredWindowTag)
            {
                return false;
            }

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        public bool MatchesCommand(CommandType commandType, CommandPhase commandPhase)
        {
            return RequiredType == commandType && RequiredPhase == commandPhase;
        }
    }

    [Serializable]
    public class ContextRoute
    {
        [Header("Trigger Command")]
        public CommandType RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;

        [Header("Next Action")]
        [Tooltip("Leave empty to let the command resolver choose a context-sensitive action variant.")]
        public ActionConfigAsset NextAction;

        [Header("Extra Conditions")]
        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        [Tooltip("InstantOnly rejects buffered input. Other modes currently accept both live and buffered input.")]
        public ComboTriggerMode TriggerMode = ComboTriggerMode.Buffered;

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

    public interface ICommandActionResolver
    {
        ActionConfigAsset Resolve(CharacterEntity entity, CharacterCommand command, ActionConfigAsset configuredAction);
    }

    public static class CommandActionResolverRegistry
    {
        private static readonly Dictionary<CommandType, ICommandActionResolver> Resolvers = new()
        {
            { CommandType.Evade, new EvadeCommandActionResolver() }
        };

        public static ActionConfigAsset Resolve(
            CharacterEntity entity,
            CharacterCommand command,
            ActionConfigAsset configuredAction)
        {
            if (command == null)
            {
                return configuredAction;
            }

            if (Resolvers.TryGetValue(command.Type, out ICommandActionResolver resolver) && resolver != null)
            {
                return resolver.Resolve(entity, command, configuredAction);
            }

            return configuredAction;
        }
    }

    public sealed class EvadeCommandActionResolver : ICommandActionResolver
    {
        public ActionConfigAsset Resolve(
            CharacterEntity entity,
            CharacterCommand command,
            ActionConfigAsset configuredAction)
        {
            if (configuredAction != null)
            {
                return configuredAction;
            }

            CharacterConfigAsset config = entity?.Config;
            CharacterRuntimeData runtimeData = entity?.RuntimeData;
            if (config == null || runtimeData == null || !runtimeData.CanEvade(config))
            {
                return null;
            }

            bool preferFront = command.Payload.HasMovementInput &&
                               command.Payload.DirectionSnapshot.sqrMagnitude > 0.01f;

            if (preferFront)
            {
                return ResolveFirst(config.evadeFront) ?? ResolveFirst(config.evadeBack);
            }

            return ResolveFirst(config.evadeBack) ?? ResolveFirst(config.evadeFront);
        }

        private static ActionConfigAsset ResolveFirst(SkillConfigAsset[] actions)
        {
            return actions != null && actions.Length > 0 ? actions[0] : null;
        }
    }
}
