using System;
using System.Collections.Generic;
using Game.FSM;
using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 动作控制器：连接 ActionPlayer（底层播放）和 FSM（状态机）的核心中枢。
    /// 
    /// 重构后：完全多态化！
    /// - Controller 不再区分路由是 Command 还是 Event，统一封装进 CharacterCommand (Command Envelope)。
    /// - 评估时调用唯一入口 route.Evaluate(command, windowTag, actor, timing)。
    /// </summary>
    public class ActionController : IRouteWindowHandler
    {
        // ─── 内部数据结构 ───

        private struct RouteCandidate
        {
            public CharacterCommand Command;
            public ActionConfigAsset NextAction;
            public ExecuteEvent RouteExecuteEvent;
            public ExecuteTarget ExecuteType;
            public int Priority;
            public string RouteTag;
            public ActionRoute SourceRoute;
        }

        public struct ExecutionRecord
        {
            public long CommandId;
            public CommandRouteSource Source;
            public string RouteTag;
            public int ActionId;
            public string ActionName;
            public float Timestamp;
            public ActionConfigAsset Asset;
        }

        public sealed class RouteWindowData
        {
            public string Tag;
            public List<CharacterCommand> CapturedCommands = new();
        }

        // ─── 字段 ───

        protected readonly CharacterEntity _entity;
        private readonly List<RouteWindowData> _activeRouteWindows = new();
        private readonly List<ActionRoute> _effectiveRoutes = new();

        private bool _isTransitioning;
        private ActionConfigAsset _currentPlayingAction;
        public ActionConfigAsset CurrentPlayingAction => _currentPlayingAction;
    
        public List<ExecutionRecord> ExecutionHistory { get; } = new();

        protected ActionRuntimeData _actionData;

        public ActionController(CharacterEntity entity)
        {
            _entity = entity;
            _actionData = _entity.DataModule?.Get<ActionRuntimeData>();
        }

        // ═══════════════════════════════════════════
        //  公共接口
        // ═══════════════════════════════════════════

        public void Update(float deltaTime)
        {
            _entity.CommandBuffer?.Tick();

            // 评估当前活跃窗口的自动过渡 (AutoTransition) 或 Condition
            EvalAutoTransitionsPerFrame();

            // 兜底评估：当没有任何活跃窗口时（如 Idle 状态），或对于缓冲中未被消费的指令
            // 保证待机状态或 AI 指令能随时切入
            if (!_isTransitioning && _activeRouteWindows.Count == 0 && _entity.CommandBuffer != null)
            {
                foreach (var cmd in _entity.CommandBuffer.GetUnconsumedCommands())
                {
                    ActionConfigAsset action = GetCurrentAction();
                    if (action == null) break;

                    action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
                    if (_effectiveRoutes.Count == 0) continue;

                    // 将空闲状态视为一个全局的匹配窗（tag=""）
                    if (TryResolve(cmd, "", RouteSingleModifierCheckTiming.EveryFrameInWindow, out RouteCandidate candidate))
                    {
                        Apply(candidate);
                        cmd.IsConsumed = true;
                        break;
                    }
                }
            }
        }

        public bool PlayAction(ActionConfigAsset action, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (action == null) return false;

            if (PlayAndTrack(action, crossfadeOverride, startTime))
            {
                OnActionStateSwitch(action);
                return true;
            }
            return false;
        }

        public void OnInput(CharacterCommand command)
        {
            if (_entity.CommandBuffer == null || command == null) return;

            // 清理旧 Move Canceled
            if (command.Payload is InputPayload p && p.InputType == HardwareInputType.Move && p.Phase == CommandPhase.Canceled)
            {
                PurgeActiveWindowMoveCommands();
            }

            CaptureToActiveWindows(command);

            // 无论怎样先做一次 Instant 评估，如果立即匹配成功就消费掉，不入缓冲区。
            if (_activeRouteWindows.Count > 0 && !_isTransitioning)
            {
                if (TryMatchInstant(command)) return;
            }

            _entity.CommandBuffer.Push(command);
        }

        public void OnComboWindowEnter(string comboTag)
        {
            _activeRouteWindows.Add(new RouteWindowData { Tag = comboTag });
            EvalAutoTransitions(comboTag, RouteSingleModifierCheckTiming.OnWindowEnter);
        }

        public void OnComboWindowExit(string comboTag)
        {
            int idx = FindWindowIndex(comboTag);
            RouteWindowData window = idx >= 0 ? _activeRouteWindows[idx] : null;

            List<CharacterCommand> captured = CollectCaptured(window);

            // 1. Auto Transitions OnExit
            if (EvalAutoTransitions(comboTag, RouteSingleModifierCheckTiming.OnWindowExit)) return;

            // 2. Buffer Commands
            if (EvalBufferRoutes(comboTag, captured)) return;

            if (idx >= 0)
                _activeRouteWindows.RemoveAt(idx);
        }

        public bool TryTriggerEvent(RouteEventType eventType, string windowTag = null)
        {
            if (_isTransitioning) return false;

            var eventCommand = CharacterCommandFactory.CreateSystemEventCommand(eventType);

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            
            // 优先检查指定窗口或所有活跃窗口
            if (!string.IsNullOrEmpty(windowTag))
            {
                if (TryResolve(eventCommand, windowTag, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                    return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);
            }
            else
            {
                foreach (var w in _activeRouteWindows)
                {
                    if (TryResolve(eventCommand, w.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                        return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);
                }
            }

            // 回退到根动作的路由
            ActionConfigAsset root = _entity.Config?.ActionRoot;
            if (root == null || root == action) return false;

            root.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            
            if (!string.IsNullOrEmpty(windowTag))
            {
                if (TryResolve(eventCommand, windowTag, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                    return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);
            }
            else
            {
                foreach (var w in _activeRouteWindows)
                {
                    if (TryResolve(eventCommand, w.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                        return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);
                }
            }

            return false;
        }

        // ═══════════════════════════════════════════
        //  核心播放
        // ═══════════════════════════════════════════

        private bool PlayAndTrack(ActionConfigAsset action, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (action == null || _entity.ActionPlayer == null) return false;

            _activeRouteWindows.Clear();

            _currentPlayingAction = action;
            if (_actionData != null)
                _actionData.NextActionToCast = action;

            _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;

            bool success = _entity.ActionPlayer.PlayAction(action, crossfadeOverride, startTime);

            if (_currentPlayingAction != action)
                return false;

            if (!success)
            {
                _currentPlayingAction = null;
                return false;
            }

            _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;
            _entity.ActionPlayer.OnActionComplete += HandleActionComplete;

            if (_entity.Config != null)
                _entity.ActionPlayer.SetPlaySpeed(action.PlaybackSpeed);

            return true;
        }

        // ═══════════════════════════════════════════
        //  统一多态评估
        // ═══════════════════════════════════════════

        private bool TryMatchInstant(CharacterCommand command)
        {
            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return false;

            foreach (RouteWindowData window in _activeRouteWindows)
            {
                if (TryResolve(command, window.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow, out RouteCandidate candidate))
                {
                    Apply(candidate);
                    return true;
                }
            }
            return false;
        }

        private bool EvalBufferRoutes(string tag, List<CharacterCommand> commands)
        {
            if (_isTransitioning || commands == null) return false;

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return false;

            RouteCandidate best = default;
            bool found = false;

            foreach (var cmd in commands)
            {
                if (!TryResolve(cmd, tag, RouteSingleModifierCheckTiming.OnWindowExit, out var c)) continue;
                if (!found || IsHigherPriority(c, best)) { best = c; found = true; }
            }

            if (found)
            {
                Apply(best);
                return true;
            }
            return false;
        }

        private bool EvalAutoTransitions(string tag, RouteSingleModifierCheckTiming timing)
        {
            if (_isTransitioning) return false;

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return false;

            if (TryResolve(null, tag, timing, out var candidate))
            {
                Apply(candidate);
                return true;
            }
            return false;
        }

        private void EvalAutoTransitionsPerFrame()
        {
            foreach (RouteWindowData window in _activeRouteWindows)
            {
                if (EvalAutoTransitions(window.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow))
                    return;
            }
        }

        private bool TryResolve(CharacterCommand command, string tag, RouteSingleModifierCheckTiming timing, out RouteCandidate best)
        {
            best = default;
            bool found = false;

            foreach (ActionRoute route in _effectiveRoutes)
            {
                if (route == null) continue;
                if (!route.IsInvalid()) continue;

                if (!route.Evaluate(command, tag, GetRouteEvalActor(), timing)) 
                    continue;

                var c = new RouteCandidate
                {
                    Command = command,
                    NextAction = route.ExecuteAction,
                    RouteExecuteEvent = route.RouteExecuteEvent,
                    ExecuteType = route.ExecuteType,
                    Priority = route.Priority,
                    RouteTag = tag,
                    SourceRoute = route
                };

                if (!found || IsHigherPriority(c, best)) { best = c; found = true; }
            }
            return found;
        }

        // ═══════════════════════════════════════════
        //  应用 & 提交
        // ═══════════════════════════════════════════

        private void Apply(RouteCandidate candidate)
        {
            candidate.SourceRoute?.ConsumeSkillCost(GetRouteEvalActor());
            float crossfade = candidate.SourceRoute?.CrossfadeOverride ?? -1f;
            Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag, crossfade);
        }

        private bool Commit(
            CharacterCommand command,
            ActionConfigAsset nextAction,
            ExecuteEvent routeExecuteEvent,
            ExecuteTarget executeType,
            CommandRouteSource source,
            string tag = null,
            float crossfadeOverride = -1f)
        {
            if (executeType == ExecuteTarget.None) return false;
            if (executeType == ExecuteTarget.Action && nextAction == null) return false;
            if (executeType == ExecuteTarget.Event && routeExecuteEvent == ExecuteEvent.None) return false;

            if (command != null) command.IsConsumed = true;

            if (executeType == ExecuteTarget.Action)
            {
                _isTransitioning = true;
                try
                {
                    _activeRouteWindows.Clear();
                    _entity.CommandBuffer?.Clear();

                    if (_actionData != null) _actionData.NextActionToCast = nextAction;
                    RecordRoute(command?.Payload, nextAction, source, tag, command?.Id ?? 0);
                    PlayAction(nextAction, crossfadeOverride);
                }
                finally
                {
                    _isTransitioning = false;
                }
            }
            else if (executeType == ExecuteTarget.Event)
            {
                RecordRoute(command?.Payload, null, source, tag, command?.Id ?? 0);
                OnRouteEventCommit(routeExecuteEvent);

                if (routeExecuteEvent == ExecuteEvent.TimelineRewind)
                {
                    _entity.ActionPlayer?.SendTimelineMessage(ExecuteEvent.TimelineRewind.ToString());
                }
                else if (routeExecuteEvent == ExecuteEvent.TimelineSkip)
                {
                    _entity.ActionPlayer?.SetTimelineFlag(ExecuteEvent.TimelineSkip.ToString());
                }
            }

            return true;
        }

        private void HandleActionComplete()
        {
            ActionConfigAsset finished = _currentPlayingAction;
            _currentPlayingAction = null;
            
            float overshoot = _entity.ActionPlayer != null ? _entity.ActionPlayer.OvershootTime : 0f;

            if (finished == null || _isTransitioning) return;

            finished.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (TryResolve(null, "", RouteSingleModifierCheckTiming.OnWindowExit, out var c))
            {
                Apply(c);
                return;
            }

            switch (finished.CompleteMode)
            {
                case ActionCompleteMode.TransitToAction:
                    if (finished.CompleteAction != null)
                    {
                        RecordRoute(null, finished.CompleteAction, CommandRouteSource.ActionComplete, "TransitToAction");
                        PlayAction(finished.CompleteAction, finished.CompleteTransitCrossfade, overshoot);
                        return;
                    }
                    break;
                case ActionCompleteMode.Stay:
                    RecordRoute(null, null, CommandRouteSource.ActionComplete, "Stay");
                    return;
            }

            ActionConfigAsset rootAction = _entity.Config?.ActionRoot;
            RecordRoute(null, rootAction, CommandRouteSource.ActionComplete, "RootFallback");
            PlayAction(rootAction, -1f, overshoot);
        }

        private static bool IsHigherPriority(RouteCandidate a, RouteCandidate b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            long orderA = a.Command?.BufferOrder ?? 0L;
            long orderB = b.Command?.BufferOrder ?? 0L;
            return orderA > orderB;
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _entity.ActionPlayer?.CurrentAction 
                ?? _actionData?.NextActionToCast 
                ?? _entity.Config?.ActionRoot;
        }

        private void RecordRoute(ICommandPayload payload, ActionConfigAsset action, CommandRouteSource source, string tag, long commandId = 0)
        {
            RecordComboRoute(source, tag, payload, action);

            ExecutionHistory.Insert(0, new ExecutionRecord
            {
                CommandId = commandId, Source = source,
                RouteTag = tag, ActionId = action?.ID ?? -1, ActionName = action?.name, Timestamp = Time.time,
                Asset = action
            });
            if (ExecutionHistory.Count > 10) ExecutionHistory.RemoveAt(10);
        }

        public CommandFate CheckCommandFate(long commandId)
        {
            if (commandId <= 0) return CommandFate.Dropped;

            foreach (var record in ExecutionHistory)
            {
                if (record.CommandId == commandId) return CommandFate.Executed;
            }

            if (_entity.CommandBuffer != null)
            {
                foreach (var cmd in _entity.CommandBuffer.GetUnconsumedCommands())
                {
                    if (cmd.Id == commandId && !cmd.IsConsumed) return CommandFate.Pending;
                }
            }

            foreach (var window in _activeRouteWindows)
            {
                if (window.CapturedCommands != null)
                {
                    foreach (var cmd in window.CapturedCommands)
                    {
                        if (cmd.Id == commandId && !cmd.IsConsumed) return CommandFate.Pending;
                    }
                }
            }

            return CommandFate.Dropped;
        }

        private void PurgeActiveWindowMoveCommands()
        {
            foreach (RouteWindowData w in _activeRouteWindows)
            {
                w.CapturedCommands.RemoveAll(cmd => cmd.Payload is InputPayload p && p.InputType == HardwareInputType.Move);
            }
        }

        private void CaptureToActiveWindows(CharacterCommand command)
        {
            if (command == null) return;
            foreach (RouteWindowData w in _activeRouteWindows)
                w.CapturedCommands.Add(CloneCommand(command));
        }

        private int FindWindowIndex(string tag)
        {
            for (int i = _activeRouteWindows.Count - 1; i >= 0; i--)
                if (_activeRouteWindows[i].Tag == tag) return i;
            return -1;
        }

        private List<CharacterCommand> CollectCaptured(RouteWindowData window)
        {
            List<CharacterCommand> result = new();
            if (window != null)
                foreach (var cmd in window.CapturedCommands) result.Add(CloneCommand(cmd));
            return result;
        }

        private static CharacterCommand CloneCommand(CharacterCommand s)
        {
            return s == null ? null : new CharacterCommand
            {
                Id = s.Id,
                Payload = s.Payload,
                Timestamp = s.Timestamp, BufferOrder = s.BufferOrder,
                IsConsumed = s.IsConsumed
            };
        }

        protected virtual RoleEntity GetRouteEvalActor() => null;
        protected virtual void OnActionStateSwitch(ActionConfigAsset action) { }
        protected virtual void OnRouteEventCommit(ExecuteEvent routeExecuteEvent) { }
        protected virtual void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action) { }
    }
}
