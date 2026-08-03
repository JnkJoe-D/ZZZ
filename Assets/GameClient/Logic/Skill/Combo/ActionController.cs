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
    /// 指令分流设计：
    ///   Instant 路由 — 仅在 OnInput 时用当前指令评估，不查缓冲区
    ///   Buffer 路由  — 指令存入缓冲区，在 OnComboWindowExit 时统一结算
    ///   Condition 路由 — 不依赖指令，由窗口生命周期或每帧驱动
    ///   Event 路由    — 由外部事件（受击、切人等）触发
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
            public InputCommand Type;
            public CommandPhase Phase;
            public CommandRouteSource Source;
            public string RouteTag;
            public int ActionId;
            public string ActionName;
            public float Timestamp;
        }

        public sealed class ComboWindowData
        {
            public string Tag;
            public List<CharacterCommand> CapturedCommands = new();
        }

        // ─── 字段 ───

        private readonly CharacterEntity _entity;
        private readonly List<ComboWindowData> _activeComboWindows = new();
        private readonly List<ActionRoute> _effectiveRoutes = new();

        private bool _isTransitioning;
        private SkillRunner _currentRunner;
        private ActionConfigAsset _currentPlayingAction;

        public List<ExecutionRecord> ExecutionHistory { get; } = new();

        public ActionController(CharacterEntity entity)
        {
            _entity = entity;
        }

        // ═══════════════════════════════════════════
        //  公共接口
        // ═══════════════════════════════════════════

        /// <summary> 每帧更新 </summary>
        public void Update(float deltaTime)
        {
            _entity.CommandBuffer?.Tick();

            EvalConditionPerFrame();
        }

        /// <summary> 播放指定动作并同步状态机 </summary>
        public SkillRunner PlayAction(ActionConfigAsset action)
        {
            if (action == null) return null;

            if (PlayAndTrack(action))
            {
                SwitchState(action.EnterState);
            }
            return _currentRunner;
        }

        /// <summary>
        /// 指令入口 — Instant/Buffer 在此分流。
        /// 
        /// Instant 路由：仅用当前这条指令做瞬时匹配，命中则执行，不入缓冲区。
        /// Buffer 路由：指令入缓冲区 + 记录到活跃窗口，等 OnComboWindowExit 结算。
        /// </summary>
        public void OnInput(CharacterCommand command)
        {
            if (_entity.CommandBuffer == null || command == null) return;

            // 1. 记录到活跃窗口（供 OnWindowExit 结算使用）
            CaptureToActiveWindows(command);

            // 2. Instant 分流：如果有活跃窗口，用当前指令单独做 Instant 评估
            if (_activeComboWindows.Count > 0 && !_isTransitioning)
            {
                if(TryMatchInstant(command))return;// Instant 指令不入缓冲区
            }
        
            // 3. buffer指令 → 入缓冲区，等待 OnWindowExit 或后续评估
            _entity.CommandBuffer.Push(command);
        }

        /// <summary>
        /// 窗口开启回调 — 仅评估 Condition 路由 + 注入 Held 快照用于 Held 指令匹配。
        /// 不再做 Instant 评估（Instant 的语义是"指令到达时"，不是"窗口开启时"）。
        /// </summary>
        public void OnComboWindowEnter(string comboTag)
        {
            _activeComboWindows.Add(new ComboWindowData { Tag = comboTag });

            // 评估窗口进入时的 Condition 路由
            EvalCondition(comboTag, RouteSingleModifierCheckTiming.OnWindowEnter);
        }

        /// <summary> 窗口关闭回调 — 用窗口期内捕获的指令做 OnWindowExit 结算 </summary>
        public void OnComboWindowExit(string comboTag)
        {
            int idx = FindWindowIndex(comboTag);
            ComboWindowData window = idx >= 0 ? _activeComboWindows[idx] : null;

            List<CharacterCommand> captured = CollectCaptured(window);

            // 1. Condition 路由（OnWindowExit 时机）
            if(EvalCondition(comboTag, RouteSingleModifierCheckTiming.OnWindowExit))return ;

            // 2. Buffer 路由（OnWindowExit 模式的指令结算）
            if(EvalBufferRoutes(comboTag, captured))return;

            if (idx >= 0)
                _activeComboWindows.RemoveAt(idx);
        }

        /// <summary> 外部事件触发路由（如受击、切人） </summary>
        public bool TryTriggerEvent(RouteEventType eventType, string windowTag = null)
        {
            if (_isTransitioning) return false;

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes);
            if (FindBestEvent(eventType, windowTag, out var candidate))
                return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);

            // 回退到根动作的路由
            ActionConfigAsset root = _entity.Config?.ActionRoot;
            if (root == null || root == action) return false;

            root.CollectEffectiveRoutes(_effectiveRoutes);
            if (FindBestEvent(eventType, windowTag, out candidate))
                return Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);

            return false;
        }

        // ═══════════════════════════════════════════
        //  核心播放
        // ═══════════════════════════════════════════

        /// <summary> 播放并追踪动作生命周期 </summary>
        private bool PlayAndTrack(ActionConfigAsset action)
        {
            if (action == null || _entity.ActionPlayer == null) return false;

            _activeComboWindows.Clear();

            _currentPlayingAction = action;
            if (_entity.RuntimeData != null)
                _entity.RuntimeData.NextActionToCast = action;

            // Play() 内部会 Tick(0f)，可能触发嵌套路由
            _currentRunner = _entity.ActionPlayer.PlayAction(action);

            // 嵌套打断检测：Tick(0f) 期间如果触发了更高优先级路由，
            // _currentPlayingAction 已被内层替换，外层不应再切状态
            if (_currentPlayingAction != action)
                return false;

            if (_currentRunner == null)
            {
                _currentPlayingAction = null;
                return false;
            }

            // 幂等绑定 OnComplete（Runner 是同一缓存实例）
            _currentRunner.OnComplete -= HandleActionComplete;
            _currentRunner.OnComplete += HandleActionComplete;

            if (_entity.Config != null)
                _entity.ActionPlayer.SetPlaySpeed(action.PlaybackSpeed);

            return true;
        }

        // ═══════════════════════════════════════════
        //  Instant 评估（仅在 OnInput 时触发）
        // ═══════════════════════════════════════════

        /// <summary> 用单条指令在所有活跃窗口中做 Instant 匹配 </summary>
        private bool TryMatchInstant(CharacterCommand command)
        {
            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes);
            if (_effectiveRoutes.Count == 0) return false;

            // 只用当前这条指令做 Instant 匹配，不查缓冲区
            foreach (ComboWindowData window in _activeComboWindows)
            {
                if (TryResolveCommand(command, window.Tag, CommandTriggerMode.Instant, out RouteCandidate candidate))
                {
                    Apply(candidate);
                    return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════
        //  Buffer 评估（仅在 OnComboWindowExit 时触发）
        // ═══════════════════════════════════════════

        /// <summary> 用捕获的指令列表做 OnWindowExit 结算 </summary>
        private bool EvalBufferRoutes(string tag, List<CharacterCommand> commands)
        {
            if (_isTransitioning || commands == null) return false;

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes);
            if (_effectiveRoutes.Count == 0) return false;

            if (FindBestCommand(tag, CommandTriggerMode.OnWindowExit, commands, out var candidate))
            {
                Apply(candidate);
                return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════
        //  Condition 评估
        // ═══════════════════════════════════════════

        /// <summary> 评估指定时机的 Condition 路由 </summary>
        private bool EvalCondition(string tag, RouteSingleModifierCheckTiming timing)
        {
            if (_isTransitioning) return false;

            ActionConfigAsset action = GetCurrentAction();
            if (action == null) return false;

            action.CollectEffectiveRoutes(_effectiveRoutes);
            if (_effectiveRoutes.Count == 0) return false;

            if (FindBestCondition(tag, timing, out var candidate))
            {
                Apply(candidate);
                return true;
            }
            return false;
        }

        /// <summary> 每帧评估活跃窗口内的 EveryFrame 条件路由 </summary>
        private void EvalConditionPerFrame()
        {
            foreach (ComboWindowData window in _activeComboWindows)
            {
                if (EvalCondition(window.Tag, RouteSingleModifierCheckTiming.EveryFrameInWindow))
                    return;
            }
        }

        // ═══════════════════════════════════════════
        //  路由查找
        // ═══════════════════════════════════════════

        /// <summary> 将单条指令与所有路由做匹配，返回最优候选 </summary>
        private bool TryResolveCommand(CharacterCommand command, string tag, CommandTriggerMode mode, out RouteCandidate best)
        {
            best = default;
            bool found = false;

            foreach (ActionRoute route in _effectiveRoutes)
            {
                if (route == null) continue;
                if (!route.IsInvalid()) continue;

                bool conditionOk = route.EvaluatePlayerCommand(command, tag, mode, _entity);
                
                if (!conditionOk) continue;

                bool modOk = !route.HasModifier || route.EvaluateModifier(_entity, tag);
                if (!modOk) continue;

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

        /// <summary> 在指令列表中找最优 PlayerCommand 候选 </summary>
        private bool FindBestCommand(string tag, CommandTriggerMode mode, List<CharacterCommand> commands, out RouteCandidate best)
        {
            best = default;
            bool found = false;
            if (commands == null) return false;

            foreach (CharacterCommand cmd in commands)
            {
                if (!TryResolveCommand(cmd, tag, mode, out var c)) continue;
                if (!found || IsHigherPriority(c, best)) { best = c; found = true; }
            }
            return found;
        }

        /// <summary> 查找最优 Condition 候选 </summary>
        private bool FindBestCondition(string tag, RouteSingleModifierCheckTiming timing, out RouteCandidate best)
        {
            best = default;
            bool found = false;

            foreach (ActionRoute route in _effectiveRoutes)
            {
                if (route == null) continue;
                if (!route.IsInvalid()) continue;

                if (!route.EvaluateConditionTrigger(_entity, tag, timing)) continue;

                bool modOk = !route.HasModifier || route.EvaluateModifier(_entity, tag);
                if (!modOk) continue;

                var c = new RouteCandidate
                {
                    Command = null,
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

        /// <summary> 查找最优 Event 候选（跨活跃窗口或指定窗口） </summary>
        private bool FindBestEvent(RouteEventType eventType, string explicitTag, out RouteCandidate best)
        {
            best = default;
            bool found = false;

            if (!string.IsNullOrEmpty(explicitTag))
                return FindEventInTag(eventType, explicitTag, out best);

            foreach (ComboWindowData w in _activeComboWindows)
            {
                if (w == null || string.IsNullOrEmpty(w.Tag)) continue;
                if (!FindEventInTag(eventType, w.Tag, out var c)) continue;
                if (!found || IsHigherPriority(c, best)) { best = c; found = true; }
            }
            return found;
        }

        private bool FindEventInTag(RouteEventType eventType, string tag, out RouteCandidate best)
        {
            best = default;
            bool found = false;

            foreach (ActionRoute route in _effectiveRoutes)
            {
                if (route == null) continue;
                if (!route.IsInvalid()) continue;

                if (!route.EvaluateEvent(eventType, _entity, tag)) continue;

                bool modOk = !route.HasModifier || route.EvaluateModifier(_entity, tag);
                if (!modOk) continue;

                var c = new RouteCandidate
                {
                    Command = null,
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
            // 1. 技能表配置的基础消耗（能量扣除等）
            candidate.SourceRoute?.ConsumeSkillCost(_entity);

            Commit(candidate.Command, candidate.NextAction, candidate.RouteExecuteEvent, candidate.ExecuteType, CommandRouteSource.ActionRoute, candidate.RouteTag);
        }

        private bool Commit(
            CharacterCommand command,
            ActionConfigAsset nextAction,
            ExecuteEvent routeExecuteEvent,
            ExecuteTarget executeType,
            CommandRouteSource source,
            string tag = null)
        {
            if (executeType == ExecuteTarget.None) return false;
            if (executeType == ExecuteTarget.Action && nextAction == null) return false;
            if (executeType == ExecuteTarget.Event && routeExecuteEvent == ExecuteEvent.None) return false;

            if (command != null) command.IsConsumed = true;

            if (executeType == ExecuteTarget.Action)
            {
                // Action 类型：切换动作，需要锁定过渡状态防止嵌套，并清理窗口/缓冲
                _isTransitioning = true;
                try
                {
                    _activeComboWindows.Clear();
                    _entity.CommandBuffer?.Clear();

                    _entity.RuntimeData.NextActionToCast = nextAction;
                    RecordRoute(command?.Type ?? InputCommand.None, command?.Phase ?? CommandPhase.Started, nextAction, source, tag);
                    PlayAction(nextAction);
                }
                finally
                {
                    _isTransitioning = false;
                }
            }
            else if (executeType == ExecuteTarget.Event)
            {
                // Event 类型：不切换动作，保留当前窗口状态和过渡锁
                // 这样同步事件链中的后续 TryTriggerEvent 才能正常工作
                RecordRoute(command?.Type ?? InputCommand.None, command?.Phase ?? CommandPhase.Started, null, source, tag);
                if (_entity is RoleEntity roleEntity)
                {
                    Game.Framework.EventCenter.Publish(new ActionRouteExecuteEvent
                    {
                        SourceEntity = roleEntity,
                        Event = routeExecuteEvent,
                        TargetSlotHint = -1
                    });
                }
            }

            return true;
        }

        // ═══════════════════════════════════════════
        //  动作完成回调
        // ═══════════════════════════════════════════

        private void HandleActionComplete()
        {
            ActionConfigAsset finished = _currentPlayingAction;
            _currentPlayingAction = null;

            if (finished == null || _isTransitioning) return;

            // 1. 动作完成时的条件路由（如持续移动输入）
            finished.CollectEffectiveRoutes(_effectiveRoutes);
            if (FindBestCondition(null, RouteSingleModifierCheckTiming.OnWindowExit, out var c))
            {
                Apply(c);
                return;
            }

            // 2. CompleteMode 处理
            switch (finished.CompleteMode)
            {
                case ActionCompleteMode.TransitToAction:
                    if (finished.CompleteAction != null)
                    {
                        RecordRoute(InputCommand.None, CommandPhase.None, finished.CompleteAction, CommandRouteSource.ActionComplete, "TransitToAction");
                        PlayAction(finished.CompleteAction);
                        return;
                    }
                    break;
                case ActionCompleteMode.Stay:
                    RecordRoute(InputCommand.None, CommandPhase.None, null, CommandRouteSource.ActionComplete, "Stay");
                    return;
            }

            // 3. 兜底回根动作
            ActionConfigAsset rootAction = _entity.Config?.ActionRoot;
            RecordRoute(InputCommand.None, CommandPhase.None, rootAction, CommandRouteSource.ActionComplete, "RootFallback");
            PlayAction(rootAction);
        }

        // ═══════════════════════════════════════════
        //  辅助方法
        // ═══════════════════════════════════════════

        private static bool IsHigherPriority(RouteCandidate a, RouteCandidate b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            long orderA = a.Command?.BufferOrder ?? 0L;
            long orderB = b.Command?.BufferOrder ?? 0L;
            return orderA > orderB;
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _entity.ActionPlayer?.CurrentAction ?? _entity.RuntimeData?.NextActionToCast;
        }

        private void RecordRoute(InputCommand type, CommandPhase phase, ActionConfigAsset action, CommandRouteSource source, string tag)
        {
            _entity.RuntimeData?.RecordResolvedRoute(source, tag, type, phase, action);

            ExecutionHistory.Insert(0, new ExecutionRecord
            {
                Type = type, Phase = phase, Source = source,
                RouteTag = tag, ActionId = action?.ID ?? -1, ActionName = action?.name, Timestamp = Time.time
            });
            if (ExecutionHistory.Count > 10) ExecutionHistory.RemoveAt(10);
        }

        private void CaptureToActiveWindows(CharacterCommand command)
        {
            if (command == null) return;
            foreach (ComboWindowData w in _activeComboWindows)
                w.CapturedCommands.Add(CloneCommand(command));
        }

        private int FindWindowIndex(string tag)
        {
            for (int i = _activeComboWindows.Count - 1; i >= 0; i--)
                if (_activeComboWindows[i].Tag == tag) return i;
            return -1;
        }

        /// <summary> 收集窗口捕获的指令（供 Buffer 路由结算使用） </summary>
        private List<CharacterCommand> CollectCaptured(ComboWindowData window)
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
                Type = s.Type, Phase = s.Phase, Payload = s.Payload,
                Timestamp = s.Timestamp, BufferOrder = s.BufferOrder,
                IsConsumed = s.IsConsumed
            };
        }

        private void SwitchState(ActionState state)
        {
            switch (state)
            {
                case ActionState.Idle:
                case ActionState.Jog:
                case ActionState.Dash:
                case ActionState.Stop:
                    if (_entity.RuntimeData != null)
                        _entity.RuntimeData.TargetGroundSubState = state;
                    _entity.Machine.ChangeState<CharacterGroundState>();
                    break;
                case ActionState.Skill:
                    _entity.Machine.ChangeState<CharacterSkillState>();
                    break;
                case ActionState.Evade:
                    _entity.Machine.ChangeState<CharacterEvadeState>();
                    break;
                case ActionState.Hit:
                    _entity.Machine.ChangeState<CharacterHitStunState>();
                    break;
                case ActionState.Switch:
                    _entity.Machine.ChangeState<CharacterSwitchState>();
                    break;
            }
        }
    }
}
