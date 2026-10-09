using System;
using System.Collections.Generic;
using UnityEngine;
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
        public RouteExecutionTracker RouteExecutionTracker { get; } = new();

        private int _currentPlayGeneration;
        private bool _isTransitioning;
        private ActionConfigAsset _currentPlayingAction;
        public ActionConfigAsset CurrentPlayingAction => _currentPlayingAction;


        public IReadOnlyList<ExecutionRecord> ExecutionHistory => _fateTracker.History;
        public int ActiveRouteWindowsCount => _activeRouteWindows.Count;

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

        /// <summary>
        /// 动作系统全系统唯一压指令入口。
        /// 严格遵循 100% 时间轴窗口配置驱动原则：
        /// - 当前动作无任何活跃路由窗口时，判定为硬直区间，直接拒绝响应；
        /// - 收到指令仅广播给当前活跃的 RouteWindow 处理；
        /// - 若命中配置为 Immediate (即时裁决) 的路由，现场立即决选执行，无需外部干预。
        /// </summary>
        public void OnInput(CharacterCommand command)
        {
            if (command == null || _isTransitioning) return;

            // 100% 窗口驱动铁律：无任何活跃窗口 = 处于不可打断的硬直区间，绝对不响应任何输入！
            if (_activeRouteWindows.Count == 0) return;

            // 1. 瞬时广播给所有活跃窗口进行前置评估，使用各窗口专属分桶切片
            for (int i = 0; i < _activeRouteWindows.Count; i++)
            {
                var windowData = _activeRouteWindows[i];
                WindowProcessContext ctx = CreateContext(windowData.Window);
                windowData.Processer?.OnCommandReceived(command, ctx);
            }

            // 2. 核心仲裁时机：若仲裁池中存在要求 Immediate (即时抢占) 的候选，立即现场决选执行
            if (_entity?.RouteArbitrator != null && _entity.RouteArbitrator.HasImmediateCandidate)
            {
                ResolveArbitratorImmediately();
                return;
            }
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
            windowData.Processer?.OnEnter(CreateContext(windowData.Window));
        }

        public void OnWindowProcess(RouteWindow routeWindow)
        {
            if (_isTransitioning || routeWindow == null) return;
            RouteWindowData data = FindActiveWindowData(routeWindow);
            data?.Processer?.OnFrameProcess(CreateContext(data.Window));
        }

        public void OnWindowExit(RouteWindow routeWindow)
        {
            if (routeWindow == null) return;

            for (int i = _activeRouteWindows.Count - 1; i >= 0; i--)
            {
                RouteWindowData w = _activeRouteWindows[i];
                if (w.Window == routeWindow)
                {
                    w.Processer?.OnExit(CreateContext(w.Window)); // 触发到期结算，推入 L2
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
                    w.Processer?.OnDisable(CreateContext(w.Window)); // 只注销并清空内部捕获，绝不推入 L2
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
                    w.Processer.OnCommandReceived(eventCommand, CreateContext(w.Window));
                }
                else
                {
                    TryResolveAndSubmitEventToArbitrator(eventCommand, GetCurrentAction(), window);
                }
            }
            else
            {
                // 2. 未指定具体窗口时，统一走时间轴活跃窗口压指令流水线
                OnInput(eventCommand);
            }

            // 3. 立即现场执行统一仲裁结算：若有 Immediate 候选或提交了候选，即时决选执行
            if (_entity?.RouteArbitrator != null && (_entity.RouteArbitrator.HasImmediateCandidate || _entity.RouteArbitrator.HasCandidates))
            {
                return ResolveArbitratorImmediately();
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
            return TryTriggerEvent(eventType, 0f, window);
        }

        private void TryResolveAndSubmitEventToArbitrator(CharacterCommand eventCommand, ActionConfigAsset action, RouteWindow window)
        {
            if (action == null || _entity.RouteArbitrator == null) return;

            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(action, window, GetRouteEvalActor(), _effectiveRoutes);
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
            RouteExecutionTracker.ResetForNewAction();

            // 切动作时，对所有存留活跃窗口调用 OnDisable 清空状态，杜绝打断遗留出招
            if (_activeRouteWindows.Count > 0)
            {
                for (int i = 0; i < _activeRouteWindows.Count; i++)
                {
                    var w = _activeRouteWindows[i];
                    w.Processer?.OnDisable(CreateContext(w.Window));
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

        private WindowProcessContext CreateContext(RouteWindow activeWindow = null)
        {
            ActionConfigAsset action = GetCurrentAction();
            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(action, activeWindow, GetRouteEvalActor(), _effectiveRoutes);
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
            // 副作用在路由确认提交后才执行 保证评估阶段多路由优先级竞争期间不会提前消费一次性状态
            candidate.SourceRoute?.CommitSideEffects(actor);

            var route = candidate.SourceRoute;
            var targets = route?.Targets;

            if (targets == null || targets.Count == 0) return;

            // 严格按照配置目标列表的顺序依次执行
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || !target.IsValid()) continue;
                if (!RouteExecutionTracker.CanExecuteTarget(target)) continue;

                if (target is EventRouteTarget eventTarget)
                {
                    RecordRoute(candidate.Command?.Payload, null, CommandRouteSource.ActionRoute, candidate.RouteTag, candidate.Command?.Id ?? 0);
                    _routeEventReceiver?.OnRouteEventExecuted(eventTarget.RouteExecuteEvent, _entity);
                    RouteExecutionTracker.RecordExecuted(route, eventTarget);
                }
                else if (target is ActionRouteTarget actionTarget)
                {
                    float crossfade = -1f;
                    float transitionStartTime = 0f;
                    ActionConfigAsset currentAction = GetCurrentAction();
                    if (currentAction != null)
                    {
                        var transition = currentAction.GetTransition(actionTarget.Action);
                        if (transition != null)
                        {
                            crossfade = transition.BlendDuration >= 0f ? transition.BlendDuration : -1f;
                            if (transition.HasStartTime)
                            {
                                transitionStartTime = Mathf.Max(0f, transition.StartTime);
                            }
                        }
                    }

                    float startTime = transitionStartTime;
                    if (candidate.Command?.Payload is SystemEventPayload sysPayload && sysPayload.StartTime > 0f)
                    {
                        startTime = sysPayload.StartTime;
                    }
                    else if (candidate.Command?.Payload is DirectAssetPayload directPayload && directPayload.StartTime > 0f)
                    {
                        startTime = directPayload.StartTime;
                    }

                    RouteExecutionTracker.RecordExecuted(route, actionTarget);

                    _isTransitioning = true;
                    try
                    {
                        _activeRouteWindows.Clear();
                        _entity.RouteArbitrator?.Clear();

                        if (_actionData != null) _actionData.Set(nameof(_actionData.NextActionToCast), actionTarget.Action);
                        RecordRoute(candidate.Command?.Payload, actionTarget.Action, CommandRouteSource.ActionRoute, candidate.RouteTag, candidate.Command?.Id ?? 0);
                        PlayAction(actionTarget.Action, crossfade, startTime);
                    }
                    finally
                    {
                        _isTransitioning = false;
                    }
                }
            }
        }

        private void HandleActionComplete()
        {
            ActionConfigAsset finished = _currentPlayingAction;
            _currentPlayingAction = null;

            var onCompleteCallback = _currentActionOnComplete;
            _currentActionOnComplete = null;
            onCompleteCallback?.Invoke();

            if (finished == null || _isTransitioning) return;

            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(finished, null, GetRouteEvalActor(), _effectiveRoutes);
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
                        var transition = finished.GetTransition(finished.CompleteAction);
                        float completeCrossfade = transition != null && transition.BlendDuration >= 0f ? transition.BlendDuration : -1f;
                        float completeStartTime = transition != null && transition.HasStartTime ? Mathf.Max(0f, transition.StartTime) : 0f;
                        PlayAction(finished.CompleteAction, completeCrossfade, completeStartTime);
                        return;
                    }
                    break;
                case ActionCompleteMode.Stay:
                    RecordRoute(null, null, CommandRouteSource.ActionComplete, "Stay");
                    return;
            }

            ActionConfigAsset rootAction = _entity.Config?.ActionRoot;
            RecordRoute(null, rootAction, CommandRouteSource.ActionComplete, "RootFallback");
            var rootTransition = finished != null && rootAction != null ? finished.GetTransition(rootAction) : null;
            float rootCrossfade = rootTransition != null && rootTransition.BlendDuration >= 0f ? rootTransition.BlendDuration : -1f;
            float rootStartTime = rootTransition != null && rootTransition.HasStartTime ? Mathf.Max(0f, rootTransition.StartTime) : 0f;
            PlayAction(rootAction, rootCrossfade, rootStartTime);
        }

        /// <summary>
        /// 检查当前动作是否配置了针对 CompleteAction 的提前退出过渡 (EndTime)。
        /// 若配置了 HasEndTime，则当动作时间轴推进至 EndTime 时，立即提前触发向 CompleteAction 的平滑过渡。
        /// </summary>
        private void CheckEarlyCompleteActionTransition()
        {
            if (_isTransitioning || _currentPlayingAction == null || ActionPlayer == null || !ActionPlayer.IsPlaying)
                return;

            if (_currentPlayingAction.CompleteMode != ActionCompleteMode.TransitToAction

                || _currentPlayingAction.CompleteAction == null)
                return;

            var transition = _currentPlayingAction.GetTransition(_currentPlayingAction.CompleteAction);
            if (transition == null || !transition.HasEndTime)
                return;

            float exitTime = transition.EndTime;
            if (ActionPlayer.CurrentTime >= exitTime)
            {
                _isTransitioning = true;
                try
                {
                    ActionConfigAsset targetAction = _currentPlayingAction.CompleteAction;
                    float crossfade = transition.BlendDuration >= 0f ? transition.BlendDuration : -1f;
                    float startTime = transition.HasStartTime ? Mathf.Max(0f, transition.StartTime) : 0f;

                    _activeRouteWindows.Clear();
                    _entity?.RouteArbitrator?.Clear();

                    if (_actionData != null) _actionData.Set(nameof(_actionData.NextActionToCast), targetAction);
                    RecordRoute(null, targetAction, CommandRouteSource.ActionComplete, "TransitToAction_EarlyExit");
                    PlayAction(targetAction, crossfade, startTime);
                }
                finally
                {
                    _isTransitioning = false;
                }
            }
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _currentPlayingAction ?? ActionPlayer?.CurrentAction;
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
