using System.Collections.Generic;
using Game.FSM;
using Game.Logic.Action.Config;
using Game.Logic.Character;
using Game.Logic.Character.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    public enum StateActionType
    {
        None = 0,
        GroundIdle = 10,
        GroundJogStart = 20,
        GroundJogStartEnd = 25,
        GroundJogLoop = 30,
        GroundJogStop = 40,
        GroundDashStart = 50,
        GroundDashLoop = 60,
        GroundDashTurnBack = 70,
        GroundDashStop = 80
    }

    public class ActionController : ISkillComboWindowHandler
    {
        public struct ExecutionRecord
        {
            public CommandType Type;
            public CommandPhase Phase;
            public CommandRouteSource Source;
            public CommandContextType Context;
            public string RouteTag;
            public int ActionId;
            public float Timestamp;
        }

        public sealed class ComboWindowData
        {
            public string Tag;
            public ComboWindowType Type;
        }

        private readonly CharacterEntity _entity;
        private readonly List<ComboWindowData> _activeComboWindows = new();
        private bool _isTransitioning;

        public List<ExecutionRecord> ExecutionHistory { get; } = new();

        public ActionController(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void Update(float deltaTime)
        {
            _entity.CommandBuffer?.Tick();
        }

        public SkillRunner PlayStateAction(StateActionType actionType)
        {
            ActionConfigAsset action = ResolveStateAction(actionType);
            if (action == null)
            {
                return null;
            }

            _entity.RuntimeData?.RecordResolvedRoute(
                CommandRouteSource.StateAction,
                actionType.ToString(),
                CommandType.None,
                CommandPhase.Started,
                action);
            RecordExecution(CommandType.None, CommandPhase.Started, action, CommandRouteSource.StateAction, actionType.ToString());
            return PlayActionDirect(action);
        }

        public SkillRunner PlayPendingAction()
        {
            return PlayActionDirect(_entity.RuntimeData?.NextActionToCast);
        }

        public void OnInput(CharacterCommand command)
        {
            if (_entity.CommandBuffer == null || command == null)
            {
                return;
            }

            _entity.CommandBuffer.Push(command);

            if (_activeComboWindows.Count > 0)
            {
                EvaluateCurrentActionRoutes();
            }

            EvaluateContextRoutes();
        }

        public void OnComboWindowEnter(string comboTag, ComboWindowType windowType)
        {
            _activeComboWindows.Add(new ComboWindowData { Tag = comboTag, Type = windowType });

            if (windowType == ComboWindowType.Execute)
            {
                EvaluateTransitionsAgainst(comboTag);
                return;
            }

            if (windowType == ComboWindowType.Buffer)
            {
                _entity.CommandBuffer?.Clear();
                return;
            }

            if (windowType == ComboWindowType.Fallback)
            {
                _entity.Machine.ChangeState<CharacterActionBackswingState>();
            }
        }

        public void OnComboWindowExit(string comboTag, ComboWindowType windowType)
        {
            _activeComboWindows.RemoveAll(x => x.Tag == comboTag && x.Type == windowType);

            if (windowType == ComboWindowType.Buffer)
            {
                EvaluateTransitionsAgainst(comboTag);
                return;
            }

            if (windowType == ComboWindowType.Fallback &&
                _entity.Machine.CurrentState is CharacterActionBackswingState)
            {
                _entity.Machine.ChangeState<CharacterGroundState>();
            }
        }

        private SkillRunner PlayActionDirect(ActionConfigAsset action)
        {
            if (action == null || _entity.ActionPlayer == null)
            {
                return null;
            }

            _activeComboWindows.Clear();

            if (_entity.RuntimeData != null)
            {
                _entity.RuntimeData.NextActionToCast = action;
            }

            return _entity.ActionPlayer.PlayAction(action);
        }

        private void EvaluateCurrentActionRoutes()
        {
            if (_isTransitioning || _entity.CommandBuffer == null || _activeComboWindows.Count == 0)
            {
                return;
            }

            ActionConfigAsset currentAction = GetCurrentAction();
            List<LocalActionRoute> localRoutes = currentAction?.LocalRoutes;
            if (localRoutes == null || localRoutes.Count == 0)
            {
                return;
            }

            foreach (CharacterCommand command in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                if (ShouldDelayBasicAttackForHold(command, localRoutes))
                {
                    continue;
                }

                bool isBuffered = (Time.time - command.Timestamp) > 0f;

                foreach (ComboWindowData window in _activeComboWindows)
                {
                    if (window.Type != ComboWindowType.Execute)
                    {
                        continue;
                    }

                    if (TryConsumeLocalRoute(command, localRoutes, window.Tag, isBuffered))
                    {
                        return;
                    }
                }
            }
        }

        private void EvaluateTransitionsAgainst(string tagToTest)
        {
            if (_isTransitioning || _entity.CommandBuffer == null)
            {
                return;
            }

            ActionConfigAsset currentAction = GetCurrentAction();
            List<LocalActionRoute> localRoutes = currentAction?.LocalRoutes;
            if (localRoutes == null || localRoutes.Count == 0)
            {
                return;
            }

            foreach (CharacterCommand command in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                if (ShouldDelayBasicAttackForHold(command, localRoutes))
                {
                    continue;
                }

                bool isBuffered = (Time.time - command.Timestamp) > 0f;
                if (TryConsumeLocalRoute(command, localRoutes, tagToTest, isBuffered))
                {
                    return;
                }
            }
        }

        private bool TryConsumeLocalRoute(
            CharacterCommand command,
            List<LocalActionRoute> routes,
            string tagToTest,
            bool isBuffered)
        {
            foreach (LocalActionRoute route in routes)
            {
                if (route == null || !route.Evaluate(command, tagToTest, isBuffered, _entity))
                {
                    continue;
                }

                ActionConfigAsset nextAction = ResolveNextAction(command, route.NextAction);
                if (nextAction == null)
                {
                    return false;
                }

                return CommitResolvedAction(command, nextAction, CommandRouteSource.LocalRoute, tagToTest);
            }

            return false;
        }

        private void EvaluateContextRoutes()
        {
            if (_isTransitioning || _entity.CommandBuffer == null)
            {
                return;
            }

            ActionConfigAsset currentAction = GetCurrentAction();

            foreach (CharacterCommand command in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                if (CurrentActionOwnsCommand(currentAction, command))
                {
                    continue;
                }

                bool isBuffered = (Time.time - command.Timestamp) > 0f;
                ActionConfigAsset nextAction = ResolveConfiguredContextAction(command, isBuffered);
                if (nextAction == null)
                {
                    continue;
                }

                if (CommitResolvedAction(command, nextAction, CommandRouteSource.ContextRoute))
                {
                    return;
                }
            }
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _entity.ActionPlayer?.CurrentAction ?? _entity.RuntimeData?.NextActionToCast;
        }

        private bool ShouldDelayBasicAttackForHold(CharacterCommand command, List<LocalActionRoute> routes)
        {
            if (command.Type != CommandType.BasicAttack || command.Phase != CommandPhase.Started)
            {
                return false;
            }

            if (_entity.Machine.CurrentState is not CharacterSkillState skillState || !skillState.IsBasicAttackHold)
            {
                return false;
            }

            return routes.Exists(t => t.MatchesCommand(CommandType.BasicAttack, CommandPhase.Performed));
        }

        private bool CurrentActionOwnsCommand(ActionConfigAsset currentAction, CharacterCommand command)
        {
            if (_entity.RuntimeData?.CurrentCommandContext == CommandContextType.Backswing)
            {
                return false;
            }

            return currentAction != null &&
                   currentAction.LocalRoutes != null &&
                   currentAction.LocalRoutes.Exists(t => t.MatchesCommand(command.Type, command.Phase));
        }

        private ActionConfigAsset ResolveConfiguredContextAction(CharacterCommand command, bool isBuffered)
        {
            CharacterConfigAsset config = _entity.Config;
            CommandContextConfig contextConfig = config?.CommandContextConfig;
            if (contextConfig == null || _entity.RuntimeData == null)
            {
                return null;
            }

            List<ContextRoute> routes = contextConfig.GetRoutes(_entity.RuntimeData.CurrentCommandContext);
            if (routes == null || routes.Count == 0)
            {
                return null;
            }

            foreach (ContextRoute route in routes)
            {
                if (route == null || !route.Evaluate(command, isBuffered, _entity))
                {
                    continue;
                }

                ActionConfigAsset nextAction = ResolveNextAction(command, route.NextAction);
                if (nextAction != null)
                {
                    return nextAction;
                }
            }

            return null;
        }

        private ActionConfigAsset ResolveNextAction(CharacterCommand command, ActionConfigAsset explicitAction)
        {
            return CommandActionResolverRegistry.Resolve(_entity, command, explicitAction);
        }

        private bool CommitResolvedAction(
            CharacterCommand command,
            ActionConfigAsset nextAction,
            CommandRouteSource routeSource,
            string routeTag = null)
        {
            if (nextAction == null)
            {
                return false;
            }

            _isTransitioning = true;
            command.IsConsumed = true;
            _entity.RuntimeData.NextActionToCast = nextAction;
            _entity.RuntimeData?.RecordResolvedRoute(routeSource, routeTag, command.Type, command.Phase, nextAction);
            RecordExecution(command.Type, command.Phase, nextAction, routeSource, routeTag);

            _activeComboWindows.Clear();
            _entity.CommandBuffer.Clear();

            UpdateCharacterState(nextAction);

            _isTransitioning = false;
            return true;
        }

        private void RecordExecution(
            CommandType commandType,
            CommandPhase commandPhase,
            ActionConfigAsset action,
            CommandRouteSource routeSource,
            string routeTag)
        {
            int actionId = action != null ? action.ID : -1;
            ExecutionHistory.Insert(0, new ExecutionRecord
            {
                Type = commandType,
                Phase = commandPhase,
                Source = routeSource,
                Context = _entity.RuntimeData?.CurrentCommandContext ?? CommandContextType.None,
                RouteTag = routeTag,
                ActionId = actionId,
                Timestamp = Time.time
            });

            if (ExecutionHistory.Count > 10)
            {
                ExecutionHistory.RemoveAt(10);
            }
        }

        private ActionConfigAsset ResolveStateAction(StateActionType actionType)
        {
            CharacterConfigAsset config = _entity.Config;
            if (config == null)
            {
                return null;
            }

            return actionType switch
            {
                StateActionType.GroundIdle => config.IdleConfig,
                StateActionType.GroundJogStart => config.JogStartConfig ?? config.JogConfig,
                StateActionType.GroundJogStartEnd => config.JogStartEndConfig ?? config.JogStopConfig,
                StateActionType.GroundJogLoop => config.JogConfig,
                StateActionType.GroundJogStop => config.JogStopConfig,
                StateActionType.GroundDashStart => config.DashStartConfig ?? config.DashConfig,
                StateActionType.GroundDashLoop => config.DashConfig,
                StateActionType.GroundDashTurnBack => config.DashTurnBackConfig ?? config.DashConfig,
                StateActionType.GroundDashStop => config.DashStopConfig ?? config.JogStopConfig,
                _ => null
            };
        }

        private void UpdateCharacterState(ActionConfigAsset action)
        {
            if (action is SkillConfigAsset skill)
            {
                if (skill.Category == SkillCategory.Evade)
                {
                    _entity.Machine.ChangeState<CharacterEvadeState>();
                    return;
                }

                _entity.Machine.ChangeState<CharacterSkillState>();
                return;
            }

            if (action is LocomotionConfigAsset)
            {
                _entity.Machine.ChangeState<CharacterGroundState>();
            }
        }
    }
}
