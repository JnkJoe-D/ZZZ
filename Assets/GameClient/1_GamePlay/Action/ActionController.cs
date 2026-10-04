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

        /// <summary>
        /// 当通过 TryTriggerEvent 派发路由系统事件时触发（供表现层/测试监听）
        /// </summary>
        public event Action<RouteEventType> OnRouteEventTriggered;

        protected ActionRuntimeData _actionData;
        protected IRouteEventReceiver _routeEventReceiver;
        protected ISkillCostHandler _skillCostHandler;
        private ActionConfigAsset _rootAction;
        private Action _currentActionOnComplete;

        public virtual void Initialize(CharacterEntity owner)
        {
            _entity = owner;
            _rootAction = _entity?.Config?.ActionRoot;
            _actionData = _entity?.DataModule?.Get<ActionRuntimeData>();
            if (ActionPlayer == null && _entity != null)
            {
                ActionPlayer = EntityModuleFactory.Create<ActionPlayer>(_entity);
            }
        }

        /// <summary>
        /// 专用于播放实体的根节点动作 (ActionRoot / 默认待机态)。
        /// 仅限初始化与切人待机复位调用。其余所有业务动作严禁使用本接口，一律走 OnInput 压指令流。
        /// </summary>
        public bool PlayRootAction(float crossfadeOverride = -1f)
        {
            if (_rootAction == null) return false;
            return PlayAction(_rootAction, crossfadeOverride);
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

            // 3. 检查 CompleteAction 的提前 ExitTime 过渡 (若配置了 HasCustomExitTime)
            CheckEarlyCompleteActionTransition();
        }

        private bool PlayAction(ActionConfigAsset action, float crossfadeOverride = -1f, float startTime = 0f)
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

            // 1. 瞬时广播给所有活跃窗口进行前置评估，不进行多余的物理指令存储与生命周期维持
            if (_activeRouteWindows.Count > 0)
            {
                WindowProcessContext ctx = CreateContext();
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                    _activeRouteWindows[i].Processer?.OnCommandReceived(command, ctx);
            }

            // 2. 针对 DirectAssetPayload（直接资产指令，如状态机出招、失衡、受击）：
            //    若活跃窗口未将其接纳入仲裁池，且未处于切动作过渡中，走直接资产通道立即执行
            if (command.Payload is DirectAssetPayload directPayload && directPayload.TargetAsset != null)
            {
                bool isQueuedInArbitrator = _entity?.RouteArbitrator != null && _entity.RouteArbitrator.HasCandidates;
                if (!isQueuedInArbitrator)
                {
                    _currentActionOnComplete = directPayload.OnComplete;
                    Commit(command, directPayload.TargetAsset, ExecuteEvent.None, ExecuteTarget.Action, CommandRouteSource.ActionRoute, "DirectAsset", directPayload.CrossfadeOverride, directPayload.StartTime);
                }
            }
        }

        /// <summary>
        /// 接收指令并同帧现场执行路由仲裁结算（0 帧时差）。
        /// 专用于受击、招架等高时效性事件，100% 遵守现存 RouteWindow 校验与 RouteArbitrator 优先级仲裁。
        /// </summary>
        public bool OnInputAndResolveImmediately(CharacterCommand command)
        {
            if (command == null || _isTransitioning) return false;

            OnInput(command);

            return ResolveArbitratorImmediately();
        }

        /// <summary>
        /// 现场立即从仲裁池取出最高优先级候选并执行切换。
        /// </summary>
        public bool ResolveArbitratorImmediately()
        {
            if (_isTransitioning || _entity?.RouteArbitrator == null) return false;

            if (_entity.RouteArbitrator.TryResolveBest(out var best))
            {
                Apply(best);
                return true;
            }
            return false;
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

        public bool TryTriggerEvent(RouteEventType eventType, RouteWindow window) => TryTriggerEvent(eventType, 0f, window);

        public bool TryTriggerEvent(RouteEventType eventType, float startTime = 0f, RouteWindow window = null)
        {
            if (_isTransitioning) return false;

            OnRouteEventTriggered?.Invoke(eventType);

            var eventCommand = CharacterCommandFactory.CreateSystemEventCommand(eventType, startTime);

            // 1. 若显式指定了具体窗口，仅针对该窗口进行评估并提交至仲裁器
            if (window != null)
            {
                RouteWindowData w = FindActiveWindowData(window);
                if (w?.Processer != null)
                {
                    w.Processer.OnCommandReceived(eventCommand, CreateContext());
                }
                else
                {
                    TryResolveAndSubmitEventToArbitrator(eventCommand, GetCurrentAction(), window);
                }
            }
            else
            {
                // 2. 未指定具体窗口时，统一走即时仲裁压指令流水线：
                // a. 广播给所有活跃窗口进行前置评估与候选提交（例如 ExecuteRouteWindow / BufferRouteWindow）
                OnInput(eventCommand);

                // b. 评估当前动作未挂载到特定窗口的全局/无窗口事件路由，提交至仲裁池统一竞优
                TryResolveAndSubmitEventToArbitrator(eventCommand, GetCurrentAction(), null);

                // c. 评估 ActionRoot 中的全局系统事件路由，提交至仲裁池统一竞优
                ActionConfigAsset root = _entity.Config?.ActionRoot;
                if (root != null && root != GetCurrentAction())
                {
                    TryResolveAndSubmitEventToArbitrator(eventCommand, root, null);
                }
            }

            // 3. 立即现场执行统一仲裁结算（0 帧时滞，严格按 RouteArbitrator 优先级决选最佳动作）
            return ResolveArbitratorImmediately();
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
            return TryTriggerEvent(eventType, 0f, window);
        }

        private void TryResolveAndSubmitEventToArbitrator(CharacterCommand eventCommand, ActionConfigAsset action, RouteWindow window)
        {
            if (action == null || _entity.RouteArbitrator == null) return;

            action.CollectEffectiveRoutes(_effectiveRoutes, GetRouteEvalActor());
            if (_effectiveRoutes.Count == 0) return;

            CharacterEntity actor = GetRouteEvalActor();

            if (RouteResolver.TryResolve(_effectiveRoutes, eventCommand, window, actor, _skillCostHandler, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
            {
                _entity.RouteArbitrator.Submit(candidate);
            }
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


            float crossfade = -1f;
            if (candidate.ExecuteType == ExecuteTarget.Action && candidate.NextAction != null)
            {
                ActionConfigAsset currentAction = GetCurrentAction();
                if (currentAction != null)
                {
                    crossfade = currentAction.GetTransitionCrossfade(candidate.NextAction);
                }
            }

            float startTime = 0f;
            if (candidate.Command?.Payload is SystemEventPayload sysPayload)
            {
                startTime = sysPayload.StartTime;
            }
            else if (candidate.Command?.Payload is DirectAssetPayload directPayload)
            {
                startTime = directPayload.StartTime;
            }

            Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag, crossfade, startTime);
        }

        private bool Commit(
            CharacterCommand command,
            ActionConfigAsset nextAction,
            ExecuteEvent routeExecuteEvent,
            ExecuteTarget executeType,
            CommandRouteSource source,
            string tag = null,
            float crossfadeOverride = -1f,
            float startTime = 0f)
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
                    PlayAction(nextAction, crossfadeOverride, startTime);
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

            var onCompleteCallback = _currentActionOnComplete;
            _currentActionOnComplete = null;
            onCompleteCallback?.Invoke();

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
                        float completeCrossfade = finished.GetTransitionCrossfade(finished.CompleteAction);
                        PlayAction(finished.CompleteAction, completeCrossfade);
                        return;
                    }
                    break;
                case ActionCompleteMode.Stay:
                    RecordRoute(null, null, CommandRouteSource.ActionComplete, "Stay");
                    return;
            }

            ActionConfigAsset rootAction = _entity.Config?.ActionRoot;
            RecordRoute(null, rootAction, CommandRouteSource.ActionComplete, "RootFallback");
            float rootCrossfade = finished != null && rootAction != null ? finished.GetTransitionCrossfade(rootAction) : -1f;
            PlayAction(rootAction, rootCrossfade);
        }

        /// <summary>
        /// 检查当前动作是否配置了针对 CompleteAction 的提前退出过渡 (ExitTime)。
        /// 若配置了 HasCustomExitTime，则当动作时间轴推进至 ExitTime 时，立即提前触发向 CompleteAction 的平滑过渡。
        /// </summary>
        private void CheckEarlyCompleteActionTransition()
        {
            if (_isTransitioning || _currentPlayingAction == null || ActionPlayer == null || !ActionPlayer.IsPlaying)
                return;

            if (_currentPlayingAction.CompleteMode != ActionCompleteMode.TransitToAction

                || _currentPlayingAction.CompleteAction == null)
                return;

            var transition = _currentPlayingAction.GetTransition(_currentPlayingAction.CompleteAction);
            if (transition == null || !transition.HasCustomExitTime)
                return;

            float exitTime = transition.CustomExitTime;
            if (ActionPlayer.CurrentTime >= exitTime)
            {
                _isTransitioning = true;
                try
                {
                    ActionConfigAsset targetAction = _currentPlayingAction.CompleteAction;
                    float crossfade = _currentPlayingAction.GetTransitionCrossfade(targetAction);

                    _activeRouteWindows.Clear();
                    _entity?.RouteArbitrator?.Clear();

                    if (_actionData != null) _actionData.Set(nameof(_actionData.NextActionToCast), targetAction);
                    RecordRoute(null, targetAction, CommandRouteSource.ActionComplete, "TransitToAction_EarlyExit");
                    PlayAction(targetAction, crossfade);
                }
                finally
                {
                    _isTransitioning = false;
                }
            }
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
