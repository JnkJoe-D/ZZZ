using System.Collections.Generic;
using Game.FSM;
using Game.Logic.Action.Config;
using Game.Logic.Character;
using Game.Logic.Character.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Action.Combo
{


    /// <summary>
    /// 【动作执行中枢】
    /// 1. 输入入口统一收口 (OnInput)
    /// 2. 统一路由评估 (ActionRoute)
    /// 3. 上下文路由兼容 (ContextRoute，用于 BackSwing 场景)
    /// 4. 驱动 ActionPlayer 播放 SkillTimeline
    /// </summary>
    public class ActionController : ISkillComboWindowHandler
    {
        /// <summary>
        /// 路由评估产生的候选者
        /// </summary>
        private struct RouteCandidate
        {
            public CharacterCommand Command;            // 匹配的指令
            public ActionConfigAsset NextAction;        // 目标动作资产 (由 Resolver 解析得出)
            public int Priority;                        // 路由优先级
            public string RouteTag;                     // 路由所属 Tag (一般对应窗口 Tag)

            // ── 组合触发源 ──
            public bool IsPending;                              // 是否需要暂存等待 Modifier 满足
            public ActionRoute SourceRoute;                     // 产生此候选者的原始路由 (供 Modifier 评估)
        }

        /// <summary>
        /// 动作执行历史记录点
        /// </summary>
        public struct ExecutionRecord
        {
            public InputCommand Type;           // 指令类型
            public CommandPhase Phase;         // 指令相位
            public CommandRouteSource Source;  // 路由来源
            public string RouteTag;            // 路由标识
            public int ActionId;               // 动作资产 ID
            public float Timestamp;            // 执行时间戳
        }

        /// <summary>
        /// 当前激活的指令/连招窗口数据
        /// </summary>
        public sealed class ComboWindowData
        {
            public string Tag;
            public ComboWindowType Type;
        }

        private readonly CharacterEntity _entity;
        
        // ── 基建 ──
        private readonly List<ComboWindowData> _activeComboWindows = new(); // 当前激活的 Timeline 窗口集

        private bool _isTransitioning; // 防止重入的转换标志

        // ── 统一路由评估管线 ──
        private readonly List<ActionRoute> _effectiveRoutes = new(); // 缓存的统一路由 (ActionRoute)

        /// <summary>
        /// 暂存路由数据区：主触发源已匹配，等待 Modifier 满足后执行。
        /// </summary>
        private struct PendingRoute
        {
            public ActionConfigAsset TargetAction;
            public ActionRoute SourceRoute;      // 原始路由引用 (用于 Modifier 评估)
            public string WindowTag;             // 关联的窗口 Tag (窗口退出时清除)
        }

        private PendingRoute? _pendingRoute;

        private SkillRunner _currentRunner;
        private ActionConfigAsset _currentPlayingAction;

        /// <summary>
        /// 动作执行环形缓冲区 (记录最近 10 次执行记录)
        /// </summary>
        public List<ExecutionRecord> ExecutionHistory { get; } = new();

        public ActionController(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void Update(float deltaTime)
        {
            // 更新指令缓冲区的倒计时 (0.3s 有效期)
            _entity.CommandBuffer?.Tick();

            // 每帧检查暂存路由的 Modifier 是否满足执行条件
            if (_pendingRoute.HasValue)
            {
                EvaluatePendingRoute();
            }
        }

        public SkillRunner PlayAction(ActionConfigAsset action)
        {
            if (action == null) return null;
            PlayAndTrack(action);
            SwitchState(action.EnterState);
            return _currentRunner;
        }

        private void PlayAndTrack(ActionConfigAsset action)
        {
            if (action == null || _entity.ActionPlayer == null)
            {
                return;
            }

            // 清理旧状态
            _activeComboWindows.Clear();
            ClearPendingRoute();

            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= HandleActionComplete;
                _currentRunner = null;
            }

            _currentPlayingAction = action;
            if (_entity.RuntimeData != null)
            {
                _entity.RuntimeData.NextActionToCast = action;
            }

            _currentRunner = _entity.ActionPlayer.PlayAction(action);
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete += HandleActionComplete;
            }

            ResolvePlaySpeed(action);
        }

        private void ResolvePlaySpeed(ActionConfigAsset action)
        {
            if (action == null || _entity.Config == null) return;

            float speed = 1.0f;
            switch (action.SpeedMultiplier)
            {
                case SpeedMultiplierType.Jog: speed = _entity.Config.JogMultipier; break;
                case SpeedMultiplierType.Dash: speed = _entity.Config.DashMultipier; break;
                case SpeedMultiplierType.Dodge: speed = _entity.Config.DodgeMultipier; break;
                case SpeedMultiplierType.Attack: speed = _entity.Config.AttackMultipier; break;
                case SpeedMultiplierType.Skill: speed = _entity.Config.SkillMultipier; break;
                case SpeedMultiplierType.None:
                default: speed = 1.0f; break;
            }

            _entity.ActionPlayer.SetPlaySpeed(speed);
        }

        /// <summary>
        /// 指令输入唯一入口 (由 CharacterInputEventAdapter 调用)
        /// </summary>
        public void OnInput(CharacterCommand command)
        {
            if (_entity.CommandBuffer == null || command == null)
            {
                return;
            }

            // 1. 命令压入缓冲区
            _entity.CommandBuffer.Push(command);

            // 2. 若当前有激活的窗口 (Execute/Buffer等)，优先评估当前动作相关的路由
            if (_activeComboWindows.Count > 0)
            {
                EvaluateCurrentActionRoutes();
            }

        }

        /// <summary>
        /// 当 Timeline 运行到 ComboWindowClip 的起始点时触发
        /// </summary>
        public void OnComboWindowEnter(string comboTag, ComboWindowType windowType)
        {
            _activeComboWindows.Add(new ComboWindowData { Tag = comboTag, Type = windowType });

            // 如果是立即执行窗口 (Execute)，立即扫描缓冲区尝试派生下一动作
            if (windowType == ComboWindowType.Execute)
            {
                _entity.CommandBuffer?.InjectHeldStateSnapshot();
                EvaluateTransitionsAgainst(comboTag);
                _entity.CommandBuffer?.ClearSyntheticCommands();
                return;
            }

            // 如果是 Buffer 窗口起始点，通常清空以往指令以保证指令的新鲜度
            if (windowType == ComboWindowType.Buffer)
            {
                _entity.CommandBuffer?.Clear();
                return;
            }

        }

        /// <summary>
        /// 当 Timeline 运行到 ComboWindowClip 的结束点时触发
        /// </summary>
        public void OnComboWindowExit(string comboTag, ComboWindowType windowType)
        {
            _activeComboWindows.RemoveAll(x => x.Tag == comboTag && x.Type == windowType);

            // 如果暂存路由关联的窗口退出，清除暂存路由
            if (_pendingRoute.HasValue && _pendingRoute.Value.WindowTag == comboTag)
            {
                ClearPendingRoute();
            }

            // 如果是 Buffer 窗口退出，此时冲刷一次缓冲区进行最后的评估
            if (windowType == ComboWindowType.Buffer)
            {
                EvaluateTransitionsAgainst(comboTag);
                return;
            }

        }



        /// <summary>
        /// 评估当前动作的路由 (由 OnInput 触发)
        /// </summary>
        private void EvaluateCurrentActionRoutes()
        {
            if (_isTransitioning || _entity.CommandBuffer == null || _activeComboWindows.Count == 0)
            {
                return;
            }

            ActionConfigAsset currentAction = GetCurrentAction();
            _effectiveRoutes.Clear();

            currentAction?.CollectEffectiveRoutes(_effectiveRoutes);

            if (_effectiveRoutes.Count == 0)
            {
                return;
            }

            // 扫描整个指令缓冲区，寻找最高优先级的候选路由
            if (TryFindBestImmediateCandidate(out RouteCandidate candidate))
            {
                if (candidate.IsPending)
                {
                    candidate.Command.IsConsumed = true;
                    SetPendingRoute(candidate.NextAction, candidate.SourceRoute, candidate.RouteTag);
                }
                else
                {
                    CommitResolvedAction(candidate.Command, candidate.NextAction, CommandRouteSource.ActionRoute, candidate.RouteTag);
                }
            }
        }

        /// <summary>
        /// 评估指定 Tag 的路由 (由 OnComboWindowEnter/Exit 触发)
        /// 一般用于处理窗口开启瞬间或关闭瞬间的"指令预输入冲刷"
        /// </summary>
        private void EvaluateTransitionsAgainst(string tagToTest)
        {
            if (_isTransitioning || _entity.CommandBuffer == null)
            {
                return;
            }

            ActionConfigAsset currentAction = GetCurrentAction();
            _effectiveRoutes.Clear();

            currentAction?.CollectEffectiveRoutes(_effectiveRoutes);

            if (_effectiveRoutes.Count == 0)
            {
                return;
            }

            if (TryFindBestCandidateForTag(tagToTest, out RouteCandidate candidate))
            {
                if (candidate.IsPending)
                {
                    candidate.Command.IsConsumed = true;
                    SetPendingRoute(candidate.NextAction, candidate.SourceRoute, candidate.RouteTag);
                }
                else
                {
                    CommitResolvedAction(candidate.Command, candidate.NextAction, CommandRouteSource.ActionRoute, candidate.RouteTag);
                }
            }
        }

        private ActionConfigAsset GetCurrentAction()
        {
            return _entity.ActionPlayer?.CurrentAction ?? _entity.RuntimeData?.NextActionToCast;
        }

        /// <summary>
        /// 【冲突规避】处理普攻长短按冲突逻辑
        /// 如果当前正在按住普攻，且当前动作拥有“Performed”相位路由，
        /// 则跳过本帧对“Started”相位的处理，防止短按触发抢走长按判定。
        /// </summary>
        private bool ShouldDelayBasicAttackForHold(CharacterCommand command)
        {
            if (command.Type != InputCommand.BasicAttack || command.Phase != CommandPhase.Started)
            {
                return false;
            }

            if (!(_entity.RuntimeData?.IsBasicAttackHold == true))
            {
                return false;
            }

            // 检查统一路由
            return _effectiveRoutes.Exists(t => t.MatchesCommand(InputCommand.BasicAttack, CommandPhase.Performed));
        }


        /// <summary>
        /// 【核心决策逻辑】在所有激活窗口下，寻找最优的局部路由候选
        /// 决策规则：
        /// 1. 枚举缓冲区中所有未消费指令
        /// 2. 检查指令是否因长按冲突需要延迟
        /// 3. 在所有激活的 Execute/Recovery 窗口中寻找匹配项
        /// 4. 比较优先级 (Priority) 和 新鲜度 (BufferOrder)
        /// </summary>
        private bool TryFindBestImmediateCandidate(out RouteCandidate bestCandidate)
        {
            bestCandidate = default;
            bool hasCandidate = false;

            foreach (CharacterCommand command in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                if (ShouldDelayBasicAttackForHold(command))
                {
                    continue;
                }

                bool isBuffered = (Time.time - command.Timestamp) > 0f;
                foreach (ComboWindowData window in _activeComboWindows)
                {
                    if (window.Type == ComboWindowType.Execute)
                    {
                        if (TryResolveUnifiedCandidate(command, _effectiveRoutes, window.Tag, isBuffered, out RouteCandidate candidateUnified))
                        {
                            if (!hasCandidate || IsHigherPriorityCandidate(candidateUnified, bestCandidate))
                            {
                                bestCandidate = candidateUnified;
                                hasCandidate = true;
                            }
                        }
                    }
                }
            }

            return hasCandidate;
        }

        private bool TryFindBestCandidateForTag(
            string tagToTest,
            out RouteCandidate bestCandidate)
        {
            bestCandidate = default;
            bool hasCandidate = false;

            foreach (CharacterCommand command in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                if (ShouldDelayBasicAttackForHold(command))
                {
                    continue;
                }

                bool isBuffered = (Time.time - command.Timestamp) > 0f;

                if (TryResolveUnifiedCandidate(command, _effectiveRoutes, tagToTest, isBuffered, out RouteCandidate candidateUnified))
                {
                    if (!hasCandidate || IsHigherPriorityCandidate(candidateUnified, bestCandidate))
                    {
                        bestCandidate = candidateUnified;
                        hasCandidate = true;
                    }
                }
            }

            return hasCandidate;
        }



        /// <summary>
        /// 评估统一路由 (ActionRoute)
        /// 支持组合触发源：主触发源匹配后，检查 Modifier 是否满足，不满足则标记为 IsPending。
        /// </summary>
        private bool TryResolveUnifiedCandidate(
            CharacterCommand command,
            List<ActionRoute> routes,
            string tagToTest,
            bool isBuffered,
            out RouteCandidate candidate)
        {
            candidate = default;
            bool hasCandidate = false;

            foreach (ActionRoute route in routes)
            {
                // 1. 核心判定逻辑：主触发源匹配 (指令 + 窗口 + TriggerMode + ExtraConditions)
                if (route == null || !route.EvaluatePlayerCommand(command, tagToTest, isBuffered, _entity))
                {
                    continue;
                }

                // 2. 目标动作
                ActionConfigAsset nextAction = route.NextAction;
                if (nextAction == null)
                {
                    continue;
                }

                // 3. 检查 Modifier：如果有组合触发源，判断是否已满足
                bool modifierSatisfied = !route.HasModifier ||
                    route.EvaluateModifier(_entity, _entity.CommandBuffer, tagToTest);

                if (route.HasModifier && !modifierSatisfied)
                {
                    // 如果策略是穿透寻优，则当前路由视为不匹配，直接寻找下一个
                    if (route.NonSatisfiedPolicy == ModifierNonSatisfiedPolicy.FallThrough)
                    {
                        continue;
                    }
                }

                RouteCandidate resolvedCandidate = new RouteCandidate
                {
                    Command = command,
                    NextAction = nextAction,
                    Priority = route.Priority,
                    RouteTag = tagToTest,
                    IsPending = route.HasModifier && !modifierSatisfied, // 此时能到这里说明 Policy 是 Pending
                    SourceRoute = route
                };

                if (!hasCandidate || IsHigherPriorityCandidate(resolvedCandidate, candidate))
                {
                    candidate = resolvedCandidate;
                    hasCandidate = true;
                }
            }

            return hasCandidate;
        }

        /// <summary>
        /// 【竞争规则】判断 A 是否比 B 更优
        /// 1. 优先级 (Priority) 更大的绝对优先
        /// 2. 优先级相同时，指令序号 (BufferOrder) 更大的优先 (即：同级输入，后按的抢占)
        /// </summary>
        private static bool IsHigherPriorityCandidate(RouteCandidate candidate, RouteCandidate currentBest)
        {
            if (candidate.Priority != currentBest.Priority)
            {
                return candidate.Priority > currentBest.Priority;
            }

            return candidate.Command.BufferOrder > currentBest.Command.BufferOrder;
        }

        /// <summary>
        /// 【执行提交】应用路由裁决结果
        /// 1. 标记指令已消费
        /// 2. 状态机切换 (UpdateCharacterState)
        /// 3. 清理上一动作残留窗口
        /// </summary>
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
            command.IsConsumed = true; // 物理标记指令消费，CommandBuffer.Tick 会据此移除它
            _entity.RuntimeData.NextActionToCast = nextAction;
            _entity.RuntimeData?.RecordResolvedRoute(routeSource, routeTag, command.Type, command.Phase, nextAction);
            RecordExecution(command.Type, command.Phase, nextAction, routeSource, routeTag);

            _activeComboWindows.Clear();
            _entity.CommandBuffer.Clear(); // 动作派生一旦发生，通常冲刷所有预输入 (避免连续误触)

            PlayAction(nextAction);

            _isTransitioning = false;
            return true;
        }

        private void RecordExecution(
            InputCommand commandType,
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
                RouteTag = routeTag,
                ActionId = actionId,
                Timestamp = Time.time
            });

            if (ExecutionHistory.Count > 10)
            {
                ExecutionHistory.RemoveAt(10);
            }
        }

        // ════════════════════════════════════════════════════════
        // 生命周期、暂存路由、状态切换
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 处理动作生命周期结束事件
        /// </summary>
        private void HandleActionComplete()
        {
            var finishedAction = _currentPlayingAction;
            _currentPlayingAction = null;

            if (finishedAction == null || _isTransitioning)
                return;

            _pendingRoute = null; // 动作自然结束，清理未触发的暂存路由

            _effectiveRoutes.Clear();
            finishedAction.CollectEffectiveRoutes(_effectiveRoutes);

            ActionRoute bestRoute = null;
            foreach (var route in _effectiveRoutes)
            {
                if (route.Category != RouteTriggerCategory.ActionLifecycle) continue;
                if (!route.EvaluateLifecycle(_entity)) continue;
                if (bestRoute == null || route.Priority > bestRoute.Priority)
                    bestRoute = route;
            }

            if (bestRoute?.NextAction != null)
            {
                _entity.RuntimeData?.RecordResolvedRoute(
                    CommandRouteSource.ActionRoute, "ActionLifecycle",
                    InputCommand.None, CommandPhase.Started, bestRoute.NextAction);
                RecordExecution(InputCommand.None, CommandPhase.Started, bestRoute.NextAction,
                    CommandRouteSource.ActionRoute, "ActionLifecycle");

                PlayAndTrack(bestRoute.NextAction);
                SwitchState(bestRoute.NextAction.EnterState);
                return;
            }

            // 2. 静态后继
            switch (finishedAction.CompleteMode)
            {
                case ActionCompleteMode.TransitToAction:
                    if (finishedAction.CompleteAction != null)
                    {
                        PlayAndTrack(finishedAction.CompleteAction);
                        SwitchState(finishedAction.CompleteAction.EnterState);
                        return;
                    }
                    break;

                case ActionCompleteMode.Stay:
                    return; // 不做任何转换

                case ActionCompleteMode.Default:
                default:
                    break;
            }

            // 3. Fallback → Idle
            SwitchState(ActionState.Idle);
        }

        /// <summary>
        /// 每帧轮询暂存路由的 Modifier 是否满足执行条件。
        /// 主触发源已匹配，Modifier 一旦满足则立即执行转换。
        /// </summary>
        private void EvaluatePendingRoute()
        {
            if (!_pendingRoute.HasValue || _isTransitioning) return;

            var pending = _pendingRoute.Value;
            if (pending.SourceRoute == null || pending.TargetAction == null)
            {
                _pendingRoute = null;
                return;
            }

            // 获取当前窗口 Tag（Modifier PlayerCommand 需要窗口约束）
            string activeTag = pending.WindowTag;

            // 评估 Modifier 是否满足
            if (!pending.SourceRoute.EvaluateModifier(_entity, _entity.CommandBuffer, activeTag))
                return;

            _pendingRoute = null; // Modifier 满足，消费掉暂存占位

            _entity.RuntimeData?.RecordResolvedRoute(
                CommandRouteSource.ActionRoute, $"Pending:{activeTag}",
                InputCommand.None, CommandPhase.Started, pending.TargetAction);
            RecordExecution(InputCommand.None, CommandPhase.Started, pending.TargetAction,
                CommandRouteSource.ActionRoute, $"Pending:{activeTag}");

            PlayAndTrack(pending.TargetAction);
            SwitchState(pending.TargetAction.EnterState);
        }

        /// <summary>
        /// 将一个动作路由设为“暂存等待 Modifier”
        /// 典型场景：主触发源已匹配，但 Modifier 条件尚未满足。
        /// </summary>
        public void SetPendingRoute(ActionConfigAsset targetAction,
            ActionRoute sourceRoute,
            string windowTag = null)
        {
            _pendingRoute = new PendingRoute
            {
                TargetAction = targetAction,
                SourceRoute = sourceRoute,
                WindowTag = windowTag ?? "Pending"
            };
        }

        public void ClearPendingRoute()
        {
            _pendingRoute = null;
        }

        /// <summary>
        /// v3: 统一且唯一的状态切换入口。
        /// 根据动作配置显式执行。
        /// </summary>
        private void SwitchState(ActionState state)
        {
            switch (state)
            {
                case ActionState.Idle:
                case ActionState.Jog:
                case ActionState.Dash:
                case ActionState.Stop:
                    // 先写入目标子状态，GroundState.OnEnter 会读取
                    if (_entity.RuntimeData != null)
                    {
                        _entity.RuntimeData.TargetGroundSubState = state;
                    }
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
            }
        }
    }
}

