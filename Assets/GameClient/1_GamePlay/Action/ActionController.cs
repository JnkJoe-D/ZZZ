using System;
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
            public ActionConfigAsset OwnerAction;
            public RouteWindow Window;
            public IRouteWindowProcesser Processer;
        }

        // ─── 字段 ───

        protected CharacterEntity _entity;
        public ActionPlayer ActionPlayer { get; private set; }

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
            if (ActionPlayer == null && _entity != null)
            {
                ActionPlayer = EntityModuleFactory.Create<ActionPlayer>(_entity);
            }
        }

        public void ResetController()
        {
            if (_activeRouteWindows.Count > 0)
            {
                WindowProcessContext ctx = CreateContext();
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    _activeRouteWindows[i].Processer?.OnDisable(ctx);
                }
                _activeRouteWindows.Clear();
            }
            _effectiveRoutes.Clear();
            _isTransitioning = false;
        }

        public ISkillCostHandler SkillCostHandler => _skillCostHandler;

        // ═══════════════════════════════════════════
        //  公共接口
        // ═══════════════════════════════════════════

        public void LogicTick(float logicDeltaTime)
        {
            // 1. 先推进 ActionPlayer（时间轴）：
            //    时间轴前进驱动各个活跃的 RuntimeRouteWindowProcess，
            //    主动触发 OnWindowEnter / OnWindowProcess / OnWindowExit / OnWindowDisable
            ActionPlayer?.LogicTick(logicDeltaTime);

            // 2. 帧末路由统一仲裁：取本帧所有到期或即时窗口提交的最高优先级候选（0 帧时滞！）
            if (!_isTransitioning && _entity.RouteArbitrator != null
                && _entity.RouteArbitrator.TryResolveBest(out var best))
            {
                Apply(best);
                return;
            }
        }

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
            if (command == null || _isTransitioning) return;

            // 瞬时广播给所有活跃窗口进行前置评估，不进行多余的物理指令存储与生命周期维持
            if (_activeRouteWindows.Count > 0)
            {
                WindowProcessContext ctx = CreateContext();
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                    _activeRouteWindows[i].Processer?.OnCommandReceived(command, ctx);
            }
        }

        public void OnWindowEnter(RouteWindow routeWindow)
        {
            if (routeWindow == null || !routeWindow.IsValid) return;

            RouteWindowData windowData = ResolveRouteWindow(routeWindow);
            if (windowData == null) return;
            _activeRouteWindows.Add(windowData);
            windowData.Processer?.OnEnter(CreateContext());
        }

        public void OnWindowProcess(RouteWindow routeWindow)
        {
            if (_isTransitioning || routeWindow == null) return;
            RouteWindowData data = FindActiveWindowData(routeWindow);
            data?.Processer?.OnFrameProcess(CreateContext());
        }

        public void OnWindowExit(RouteWindow routeWindow)
        {
            if (routeWindow == null) return;

            for (int i = _activeRouteWindows.Count - 1; i >= 0; i--)
            {
                RouteWindowData w = _activeRouteWindows[i];
                if (w.Window == routeWindow)
                {
                    w.Processer?.OnExit(CreateContext()); // 触发到期结算，推入 L2
                    _activeRouteWindows.RemoveAt(i);
                }
            }
        }

        public void OnWindowDisable(RouteWindow routeWindow)
        {
            if (routeWindow == null) return;

            for (int i = _activeRouteWindows.Count - 1; i >= 0; i--)
            {
                RouteWindowData w = _activeRouteWindows[i];
                if (w.Window == routeWindow)
                {
                    w.Processer?.OnDisable(CreateContext()); // 只注销并清空内部捕获，绝不推入 L2
                    _activeRouteWindows.RemoveAt(i);
                }
            }
        }

        public bool TryTriggerEvent(RouteEventType eventType, RouteWindow window = null)
        {
            if (_isTransitioning) return false;

            var eventCommand = CharacterCommandFactory.CreateSystemEventCommand(eventType);

            // 1. 优先尝试当前动作路由
            if (TryResolveAndCommitEvent(eventCommand, GetCurrentAction(), window))
                return true;

            // 2. 回退尝试全局 ActionRoot 路由
            ActionConfigAsset root = _entity.Config?.ActionRoot;
            if (root != null && root != GetCurrentAction())
            {
                if (TryResolveAndCommitEvent(eventCommand, root, window))
                    return true;
            }

            return false;
        }

        public bool TryTriggerEvent(RouteEventType eventType, string windowTag)
        {
            RouteWindow window = null;
            if (!string.IsNullOrEmpty(windowTag))
            {
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    if (string.Equals(_activeRouteWindows[i].Tag, windowTag, StringComparison.Ordinal))
                    {
                        window = _activeRouteWindows[i].Window;
                        break;
                    }
                }
            }
            return TryTriggerEvent(eventType, window);
        }

        private bool TryResolveAndCommitEvent(CharacterCommand eventCommand, ActionConfigAsset action, RouteWindow window)
        {
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return false;

            CharacterEntity actor = GetRouteEvalActor();

            if (window != null)
            {
                if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, window, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
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
                    if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, w.Window, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
                    {
                        Apply(candidate);
                        return true;
                    }
                }

                // 兜底尝试全局无窗口事件路由
                if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, null, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var globalCandidate))
                {
                    Apply(globalCandidate);
                    return true;
                }
            }

            return false;
        }

        // ═══════════════════════════════════════════
        //  核心播放
        // ═══════════════════════════════════════════

        private bool PlayAndTrack(ActionConfigAsset action, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (action == null || ActionPlayer == null) return false;

            unchecked { _currentPlayGeneration++; }

            // 切动作时，对所有存留活跃窗口调用 OnDisable 清空状态，杜绝打断遗留出招
            if (_activeRouteWindows.Count > 0)
            {
                WindowProcessContext ctx = CreateContext();
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    _activeRouteWindows[i].Processer?.OnDisable(ctx);
                }
                _activeRouteWindows.Clear();
            }

            _currentPlayingAction = action;
            if (_actionData != null)
                _actionData.Set(nameof(_actionData.NextActionToCast), action);

            ActionPlayer.OnActionComplete -= HandleActionComplete;

            bool success = ActionPlayer.PlayAction(action, crossfadeOverride, startTime);

            if (_currentPlayingAction != action)
                return false;

            if (!success)
            {
                _currentPlayingAction = null;
                return false;
            }

            ActionPlayer.OnActionComplete -= HandleActionComplete;
            ActionPlayer.OnActionComplete += HandleActionComplete;

            return true;
        }

        private RouteWindowData ResolveRouteWindow(RouteWindow routeWindow)
        {
            if (routeWindow == null || !routeWindow.IsValid) return null;

            IRouteWindowProcesser processer = RouteWindowProcessorFactory.Create(routeWindow);
            if (processer == null) return null;

            return new RouteWindowData
            {
                Tag = routeWindow.Tag,
                OwnerAction = GetCurrentAction(),
                Window = routeWindow,
                Processer = processer
            };
        }

        private RouteWindowData FindActiveWindowData(RouteWindow routeWindow)
        {
            for (int i = 0; i < _activeRouteWindows.Count; i++)
            {
                if (_activeRouteWindows[i].Window == routeWindow)
                    return _activeRouteWindows[i];
            }
            return null;
        }

        private WindowProcessContext CreateContext()
        {
            ActionConfigAsset action = GetCurrentAction();
            action?.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            return new WindowProcessContext
            {
                Routes = _effectiveRoutes,
                Actor = GetRouteEvalActor(),
                SkillHandler = _skillCostHandler,
                Arbitrator = _entity.RouteArbitrator
            };
        }

        // ═══════════════════════════════════════════
        //  应用 & 提交
        // ═══════════════════════════════════════════

        private void Apply(RouteCandidate candidate)
        {
            CharacterEntity actor = GetRouteEvalActor();
            candidate.SourceRoute?.ConsumeSkillCost(actor, _skillCostHandler);
            // Bug #3 修复：副作用（如清除招架标记）在路由确认提交后才执行，
            // 保证 Evaluate 阶段多路由优先级竞争期间不会提前消费一次性状态
            candidate.SourceRoute?.CommitSideEffects(actor);
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

            if (executeType == ExecuteTarget.Action)
            {
                _isTransitioning = true;
                try
                {
                    _activeRouteWindows.Clear();
                    _entity.RouteArbitrator?.Clear();

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
            if (RouteResolver.TryResolve(_effectiveRoutes, null, null, GetRouteEvalActor(), _skillCostHandler, RouteSingleModifierCheckTiming.OnWindowExit, out var c))
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
                ?? ActionPlayer?.CurrentAction 
                ?? _actionData?.NextActionToCast 
                ?? _entity.Config?.ActionRoot;
        }

        private void RecordRoute(ICommandPayload payload, ActionConfigAsset action, CommandRouteSource source, string tag, long commandId = 0)
        {
            RecordComboRoute(source, tag, payload, action);
            _fateTracker.Record(commandId, source, tag, action);
        }

        public CommandFate CheckCommandFate(long commandId) =>
            _fateTracker.CheckFate(commandId, _activeRouteWindows);

        public virtual void Dispose()
        {
            ActionPlayer?.Dispose();
        }

        protected virtual CharacterEntity GetRouteEvalActor() => null;
        protected virtual void OnActionPlaySucceed(ActionConfigAsset action) { }
        protected virtual void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action) { }
    }
}
