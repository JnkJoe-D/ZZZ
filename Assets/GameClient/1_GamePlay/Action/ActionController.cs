using System.Collections.Generic;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 动作控制器：连接 ActionPlayer（底层播放）和 FSM（状态机）的核心中枢。
    /// 
    /// 重构后：完全多态化！
    /// - Controller 不再区分路由是 Command 还是 Event，统一封装进 CharacterCommand (Command Envelope)。
    /// - 评估时调用唯一入口 route.Evaluate(command, windowTag, actor, timing)。
    /// </summary>
    public class ActionController : IRouteWindowHandler, IEntityController
    {
        // ─── 内部数据结构 ───

        public sealed class RouteWindowData
        {
            public string Tag;
            public object Token;
            public int Generation;
            public ActionConfigAsset OwnerAction;
            public List<CharacterCommand> CapturedCommands = new();
        }

        // ─── 字段 ───

        protected CharacterEntity _entity;
        private readonly List<RouteWindowData> _activeRouteWindows = new();
        private readonly List<ActionRoute> _effectiveRoutes = new();
        private readonly CommandFateTracker _fateTracker = new();

        private int _currentPlayGeneration;
        private bool _isTransitioning;
        private ActionConfigAsset _currentPlayingAction;
        public ActionConfigAsset CurrentPlayingAction => _currentPlayingAction;
    
        public IReadOnlyList<ExecutionRecord> ExecutionHistory => _fateTracker.History;

        protected ActionRuntimeData _actionData;
        protected IRouteEventReceiver _routeEventReceiver;
        protected ISkillCostHandler _skillCostHandler;

        public virtual void Initialize(CharacterEntity owner)
        {
            _entity = owner;
            _actionData = _entity.DataModule?.Get<ActionRuntimeData>();
        }

        public void ResetController()
        {
            _activeRouteWindows.Clear();
            _effectiveRoutes.Clear();
            _isTransitioning = false;
        }

        public ISkillCostHandler SkillCostHandler => _skillCostHandler;

        // ═══════════════════════════════════════════
        //  公共接口
        // ═══════════════════════════════════════════

        public void LogicTick(float logicDeltaTime)
        {
            _entity.CommandBuffer?.Tick();

            // 评估当前活跃窗口的自动过渡 (AutoTransition) 或 Condition
            EvalAutoTransitionsPerFrame();

            // 兜底评估：当没有任何活跃窗口时（如 Idle 状态），或对于缓冲中未被消费的指令
            // 保证待机状态或 AI 指令能随时切入
            if (!_isTransitioning && _activeRouteWindows.Count == 0 && _entity.CommandBuffer != null)
            {
                List<CharacterCommand> unconsumed = _entity.CommandBuffer.GetUnconsumedCommands();
                for (int i = 0; i < unconsumed.Count; i++)
                {
                    CharacterCommand cmd = unconsumed[i];
                    ActionConfigAsset action = GetCurrentAction();
                    if (action == null) break;

                    action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
                    if (_effectiveRoutes.Count == 0) continue;

                    // 将空闲状态视为一个全局的匹配窗（tag=""）
                    if (RouteResolver.TryResolve(_effectiveRoutes, cmd, "", GetRouteEvalActor(), _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out RouteCandidate candidate))
                    {
                        Apply(candidate);
                        cmd.IsConsumed = true;
                        break;
                    }
                }
            }
        }

        [System.Obsolete("Update 已过时，请统一使用 OnLogicTick")]
        public void Update(float deltaTime) => LogicTick(deltaTime);

        public bool PlayAction(ActionConfigAsset action, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (action == null) return false;

            if (PlayAndTrack(action, crossfadeOverride, startTime))
            {
                OnActionPlaySucceed(action);
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
            if (!_isTransitioning)
            {
                if (TryMatchInstant(command)) return;
            }

            _entity.CommandBuffer.Push(command);
        }

        public void OnComboWindowEnter(string comboTag, object windowToken = null)
        {
            if (string.IsNullOrEmpty(comboTag)) return;

            _activeRouteWindows.Add(new RouteWindowData 
            { 
                Tag = comboTag,
                Token = windowToken,
                Generation = _currentPlayGeneration,
                OwnerAction = _currentPlayingAction
            });
            EvalAutoTransitions(comboTag, RouteSingleModifierCheckTiming.OnWindowEnter);
        }

        public void OnComboWindowExit(string comboTag, object windowToken = null)
        {
            if (string.IsNullOrEmpty(comboTag)) return;

            // 优先通过 Token 精确匹配实例；若无 Token 则按当前代际查找
            int idx = FindWindowIndex(comboTag, windowToken, _currentPlayGeneration);
            if (idx < 0)
            {
                // 该退出事件属于已被打断/替换的旧动作代际，直接安全忽略，绝不误删当前动作的新窗口
                return;
            }

            RouteWindowData window = _activeRouteWindows[idx];
            int windowGeneration = window.Generation;
            object targetToken = window.Token;
            List<CharacterCommand> captured = window.CapturedCommands;

            try
            {
                // 1. Auto Transitions OnExit
                if (EvalAutoTransitions(comboTag, RouteSingleModifierCheckTiming.OnWindowExit)) return;

                // 2. Buffer Commands
                if (EvalBufferRoutes(comboTag, captured)) return;
            }
            finally
            {
                // 仅当动作未切入更新代际时，才移除本代际的该窗口
                if (_currentPlayGeneration == windowGeneration)
                {
                    int removeIdx = FindWindowIndex(comboTag, targetToken, windowGeneration);
                    if (removeIdx >= 0)
                    {
                        _activeRouteWindows.RemoveAt(removeIdx);
                    }
                }
            }
        }

        public bool TryTriggerEvent(RouteEventType eventType, string windowTag = null)
        {
            if (_isTransitioning) return false;

            var eventCommand = CharacterCommandFactory.CreateSystemEventCommand(eventType);

            // 1. 优先尝试当前动作路由
            if (TryResolveAndCommitEvent(eventCommand, GetCurrentAction(), windowTag))
                return true;

            // 2. 回退尝试全局 ActionRoot 路由
            ActionConfigAsset root = _entity.Config?.ActionRoot;
            if (root != null && root != GetCurrentAction())
            {
                if (TryResolveAndCommitEvent(eventCommand, root, windowTag))
                    return true;
            }

            return false;
        }

        private bool TryResolveAndCommitEvent(CharacterCommand eventCommand, ActionConfigAsset action, string windowTag)
        {
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return false;

            RoleEntity actor = GetRouteEvalActor();

            if (!string.IsNullOrEmpty(windowTag))
            {
                if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, windowTag, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                {
                    Apply(candidate);
                    return true;
                }
            }
            else
            {
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    RouteWindowData w = _activeRouteWindows[i];
                    if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, w.Tag, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                    {
                        Apply(candidate);
                        return true;
                    }
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

            unchecked { _currentPlayGeneration++; }
            _activeRouteWindows.Clear();

            _currentPlayingAction = action;
            if (_actionData != null)
                _actionData.Set(nameof(_actionData.NextActionToCast), action);

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

            RoleEntity actor = GetRouteEvalActor();

            if (_activeRouteWindows.Count > 0)
            {
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    RouteWindowData window = _activeRouteWindows[i];
                    if (RouteResolver.TryResolve(_effectiveRoutes, command, window.Tag, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out RouteCandidate candidate))
                    {
                        Apply(candidate);
                        return true;
                    }
                }
            }
            else
            {
                // 空闲或未限制窗口状态（tag=""），以全局空闲窗尝试匹配
                if (RouteResolver.TryResolve(_effectiveRoutes, command, "", actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out RouteCandidate candidate))
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

            RoleEntity actor = GetRouteEvalActor();
            RouteCandidate best = default;
            bool found = false;

            for (int i = 0; i < commands.Count; i++)
            {
                CharacterCommand cmd = commands[i];
                if (!RouteResolver.TryResolve(_effectiveRoutes, cmd, tag, actor, _skillCostHandler, RouteSingleModifierCheckTiming.OnWindowExit, out var c)) continue;
                if (!found || RouteResolver.IsHigherPriority(c, best)) { best = c; found = true; }
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

            if (RouteResolver.TryResolve(_effectiveRoutes, null, tag, GetRouteEvalActor(), _skillCostHandler, timing, out var candidate))
            {
                Apply(candidate);
                return true;
            }
            return false;
        }

        private void EvalAutoTransitionsPerFrame()
        {
            for (int i = 0; i < _activeRouteWindows.Count; i++)
            {
                RouteWindowData window = _activeRouteWindows[i];
                if (EvalAutoTransitions(window.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow))
                    return;
            }
        }

        // ═══════════════════════════════════════════
        //  应用 & 提交
        // ═══════════════════════════════════════════

        private void Apply(RouteCandidate candidate)
        {
            candidate.SourceRoute?.ConsumeSkillCost(GetRouteEvalActor(), _skillCostHandler);
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

                    if (_actionData != null) _actionData.Set(nameof(_actionData.NextActionToCast), nextAction);
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
                _routeEventReceiver?.OnRouteEventExecuted(routeExecuteEvent, _entity);
            }

            return true;
        }

        private void HandleActionComplete()
        {
            ActionConfigAsset finished = _currentPlayingAction;
            _currentPlayingAction = null;

            if (finished == null || _isTransitioning) return;

            finished.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (RouteResolver.TryResolve(_effectiveRoutes, null, "", GetRouteEvalActor(), _skillCostHandler, RouteSingleModifierCheckTiming.OnWindowExit, out var c))
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
                        PlayAction(finished.CompleteAction, finished.CompleteTransitCrossfade);
                        return;
                    }
                    break;
                case ActionCompleteMode.Stay:
                    RecordRoute(null, null, CommandRouteSource.ActionComplete, "Stay");
                    return;
            }

            ActionConfigAsset rootAction = _entity.Config?.ActionRoot;
            RecordRoute(null, rootAction, CommandRouteSource.ActionComplete, "RootFallback");
            PlayAction(rootAction, -1f);
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _currentPlayingAction
                ?? _entity.ActionPlayer?.CurrentAction 
                ?? _actionData?.NextActionToCast 
                ?? _entity.Config?.ActionRoot;
        }

        private void RecordRoute(ICommandPayload payload, ActionConfigAsset action, CommandRouteSource source, string tag, long commandId = 0)
        {
            RecordComboRoute(source, tag, payload, action);
            _fateTracker.Record(commandId, source, tag, action);
        }

        public CommandFate CheckCommandFate(long commandId) =>
            _fateTracker.CheckFate(commandId, _entity.CommandBuffer, _activeRouteWindows);

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
            for (int i = 0; i < _activeRouteWindows.Count; i++)
            {
                _activeRouteWindows[i].CapturedCommands.Add(command);
            }
        }

        private int FindWindowIndex(string tag, object token = null, int generation = -1)
        {
            for (int i = _activeRouteWindows.Count - 1; i >= 0; i--)
            {
                RouteWindowData w = _activeRouteWindows[i];
                if (w.Tag != tag) continue;

                if (token != null)
                {
                    if (ReferenceEquals(w.Token, token)) return i;
                    continue;
                }

                if (generation >= 0)
                {
                    if (w.Generation == generation) return i;
                    continue;
                }

                return i;
            }
            return -1;
        }

        private int FindWindowIndex(string tag)
        {
            return FindWindowIndex(tag, null, _currentPlayGeneration);
        }

        protected virtual RoleEntity GetRouteEvalActor() => null;
        protected virtual void OnActionPlaySucceed(ActionConfigAsset action) { }
        protected virtual void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action) { }
    }
}
