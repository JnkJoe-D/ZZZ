using System.Collections.Generic;
using Game.Framework;

using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 切出任务的生命周期阶段。
    /// </summary>
    public enum SwitchOutPhase
    {
        /// <summary> 已入队，等待角色自身路由条件命中切出动作。 </summary>
        Pending,

        /// <summary> 切出动作正在播放中（碰撞体已禁用）。 </summary>
        PlayingExit,

        /// <summary> 切出完成，渲染已隐藏。 </summary>
        Completed,

        /// <summary> 被取消（角色被重新切入）。 </summary>
        Cancelled
    }

    /// <summary>
    /// 单个角色的切出任务。
    /// 每个待切出角色在队列中拥有一个独立的任务实例。
    /// </summary>
    public class SwitchOutTask
    {
        /// <summary> 对应的队伍成员。 </summary>
        public PartyMember Member;

        /// <summary> 当前切出阶段。 </summary>
        public SwitchOutPhase Phase;

        /// <summary> 退场生命周期管理策略。 </summary>
        public OutgoingExitPolicy ExitPolicy;

        /// <summary>
        /// 创建任务时的激活版本号。
        /// 用于防腐校验：如果 Member.ActivationVersion 已变化，
        /// 说明该角色已被重新激活过，此任务应视为过期。
        /// </summary>
        public int CreationVersion;

        /// <summary> 任务已流逝时间（秒）。 </summary>
        public float ElapsedTime;

        /// <summary> 退场最大保护超时（秒），超时仍未收到隐藏关键帧则强制完成退场。 </summary>
        public const float MaxSwitchOutTimeout = 2.5f;
    }

    /// <summary>
    /// 切人执行器微内核适配器。
    ///
    /// 职责：
    ///   1. 订阅 ActionRouteExecuteEvent，解析切人请求并交由 SwitchPipeline 统一处理
    ///   2. 管理切出任务队列：支持动作自动托管生命周期、时间轴事件兼容、取消/复活、超时兜底
    ///   3. 监听 CharacterTimelineEvent，驱动存量切出动作的阶段过渡
    /// </summary>
    public class SwitchExecutor
    {
        // ─── 常量：Timeline 事件名（存量兼容） ───
        private const string EventSwitchOutDisableLogic = "SwitchOutDisableLogic";
        private const string EventHideOutgoingRole = "HideOutgoingRole";

        // ─── 字段 ───
        private readonly TeamManager _manager;
        private readonly SwitchPipeline _pipeline;
        private readonly List<SwitchOutTask> _switchOutQueue = new();

        /// <summary> 是否有正在进行中的切出任务。 </summary>
        public bool IsSwitching => _switchOutQueue.Count > 0;

        // ─── 构造与生命周期 ───

        public SwitchExecutor(TeamManager manager)
        {
            _manager = manager;
            _pipeline = SwitchPipeline.Default;
            Subscribe();
        }

        /// <summary> 订阅全局事件。 </summary>
        public void Subscribe()
        {
            EventCenter.Subscribe<ActionRouteExecuteEvent>(OnActionRouteEvent);
        }

        /// <summary> 退订全局事件。 </summary>
        public void Unsubscribe()
        {
            EventCenter.Unsubscribe<ActionRouteExecuteEvent>(OnActionRouteEvent);
        }

        /// <summary> 清空队列与状态（场景切换 / 小队重建时调用）。 </summary>
        public void Reset()
        {
            _switchOutQueue.Clear();
        }

        /// <summary>
        /// 每帧更新，清理已完成/已取消的任务，并驱动 AutoFollowThrough 自动退场。
        /// 由 TeamManager.Update 驱动。
        /// </summary>
        public void Update(float deltaTime)
        {
            for (int i = _switchOutQueue.Count - 1; i >= 0; i--)
            {
                SwitchOutTask task = _switchOutQueue[i];

                // 版本防腐：如果角色已被重新激活过，此任务过期
                if (task.Member.ActivationVersion != task.CreationVersion)
                {
                    task.Phase = SwitchOutPhase.Cancelled;
                }

                if (task.Phase == SwitchOutPhase.Pending || task.Phase == SwitchOutPhase.PlayingExit)
                {
                    task.ElapsedTime += deltaTime;
                    var entity = task.Member?.Entity;
                    bool isActionPlaying = entity != null && entity.ActionPlayer != null && entity.ActionPlayer.IsPlaying;

                    // 1. 动作自动托管策略
                    if (task.ExitPolicy == OutgoingExitPolicy.AutoByRoute)
                    {
                        // 动作自然播放完毕，自动隐藏并完成退场
                        if (task.Phase == SwitchOutPhase.PlayingExit && !isActionPlaying)
                        {
                            GLog.Info(LogTags.Team, $"AutoFollowThrough: {task.Member?.Config?.Name} 动作自然播放完毕，自动完成退场");
                            CompleteSwitchOut(task);
                        }
                    }
                }

                if (task.Phase == SwitchOutPhase.Completed ||
                    task.Phase == SwitchOutPhase.Cancelled)
                {
                    _switchOutQueue.RemoveAt(i);
                }
            }
        }

        /// <summary> 查询指定角色是否在切出队列中（Pending 或 PlayingExit）。 </summary>
        public bool IsInSwitchOutQueue(RoleEntity entity)
        {
            if (entity == null) return false;

            for (int i = 0; i < _switchOutQueue.Count; i++)
            {
                SwitchOutTask task = _switchOutQueue[i];
                if (ReferenceEquals(task.Member?.Entity, entity) &&
                    task.Phase != SwitchOutPhase.Completed &&
                    task.Phase != SwitchOutPhase.Cancelled)
                {
                    return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════
        //  事件响应与管线驱动入口
        // ═══════════════════════════════════════════

        /// <summary>
        /// ActionRouteExecuteEvent 的回调入口。
        /// </summary>
        private void OnActionRouteEvent(ActionRouteExecuteEvent evt)
        {
            switch (evt.Event)
            {
                case ExecuteEvent.SwitchCaptureSucceed:
                    RequestSwitch(SwitchType.NormalSwitch, evt.SourceEntity, evt.TargetSlotHint);
                    break;
                case ExecuteEvent.ParryAidStart:
                    RequestSwitch(SwitchType.ParryAid, evt.SourceEntity, evt.TargetSlotHint, targetAttacker: evt.TargetAttacker, warningMarker: evt.WarningMarker);
                    break;
                case ExecuteEvent.FallbackEvasionStart:
                    RequestSwitch(SwitchType.FallbackEvasion, evt.SourceEntity, evt.TargetSlotHint, targetAttacker: evt.TargetAttacker, warningMarker: evt.WarningMarker);
                    break;
                case ExecuteEvent.EvasionAidStart:
                    RequestSwitch(SwitchType.EvasionAid, evt.SourceEntity, evt.TargetSlotHint, targetAttacker: evt.TargetAttacker, warningMarker: evt.WarningMarker);
                    break;
                case ExecuteEvent.ChainAttackStart:
                    RequestSwitch(SwitchType.ChainAttack, evt.SourceEntity, evt.TargetSlotHint, targetAttacker: evt.TargetAttacker, warningMarker: evt.WarningMarker);
                    break;
                case ExecuteEvent.QuickAidStart:
                    RequestSwitch(SwitchType.QuickAid, evt.SourceEntity, evt.TargetSlotHint, targetAttacker: evt.TargetAttacker, warningMarker: evt.WarningMarker);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 发起切人请求（委托至 SwitchPipeline 流水线统一裁决调度）
        /// </summary>
        public void RequestSwitch(SwitchType type, RoleEntity sourceEntity, int slotHint = -1, CharacterEntity targetAttacker = null, AttackWarningMarker warningMarker = null)
        {
            if (sourceEntity == null) return;
            PartyMember outgoing = _manager.FindPartyMember(sourceEntity);
            if (outgoing == null) return;

            var ctx = _pipeline.AllocateContext();
            ctx.Manager = _manager;
            ctx.Type = type;
            ctx.OutgoingMember = outgoing;
            ctx.TargetSlotHint = slotHint;
            ctx.TargetAttacker = targetAttacker;
            ctx.WarningMarker = warningMarker;

            _pipeline.Execute(ctx);
            _pipeline.ReleaseContext(ctx);
        }

        /// <summary>
        /// Timeline 事件的回调入口（向下兼容存量配置了 SwitchOutDisableLogic / HideOutgoingRole 的动作）。
        /// 由 TeamManager.HandleTimelineEvent 转发。
        /// </summary>
        public bool HandleTimelineEvent(RoleEntity sourceEntity, string eventName)
        {
            if (sourceEntity == null || string.IsNullOrEmpty(eventName)) return false;

            SwitchOutTask task = FindActiveTask(sourceEntity);
            if (task == null) return false;

            switch (eventName)
            {
                case EventSwitchOutDisableLogic:
                    return HandleSwitchOutDisableLogic(task);

                case EventHideOutgoingRole:
                    return HandleHideOutgoingRole(task);

                default:
                    return false;
            }
        }

        // ═══════════════════════════════════════════
        //  切出任务队列管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 将角色加入切出队列。
        /// 设置 IsSwitchOutPending 标志，使角色自身路由系统
        /// （ConditionCommand.SwitchOutPending）能检测到并触发切出动作。
        /// </summary>
        public void EnqueueSwitchOut(PartyMember member, OutgoingExitPolicy policy = OutgoingExitPolicy.AutoByRoute)
        {
            // 防止同一角色重复入队
            for (int i = 0; i < _switchOutQueue.Count; i++)
            {
                SwitchOutTask existing = _switchOutQueue[i];
                if (existing.Member == member &&
                    existing.Phase != SwitchOutPhase.Completed &&
                    existing.Phase != SwitchOutPhase.Cancelled)
                {
                    return;
                }
            }

            if (member.Entity?.DataModule != null)
            {
                var switchData = member.Entity.DataModule.Get<SwitchRuntimeData>();
                if (switchData != null) switchData.Set(nameof(switchData.IsSwitchOutPending), true);
            }

            _switchOutQueue.Add(new SwitchOutTask
            {
                Member = member,
                Phase = SwitchOutPhase.Pending,
                ExitPolicy = policy,
                CreationVersion = member.ActivationVersion
            });

            GLog.Info(LogTags.Team, $"Enqueued switch-out: {member.Config?.Name}, Policy={policy}, Version={member.ActivationVersion}");
        }

        /// <summary>
        /// 尝试取消指定角色的切出任务（角色被重新切入时调用）。
        /// 根据当前阶段执行不同的恢复操作：
        ///   - Pending：直接取消，角色继续当前动作
        ///   - PlayingExit：中断切出动作，恢复碰撞体，播放 ActionRoot 重置
        /// </summary>
        public bool TryCancelSwitchOut(PartyMember member)
        {
            for (int i = 0; i < _switchOutQueue.Count; i++)
            {
                SwitchOutTask task = _switchOutQueue[i];
                if (task.Member != member) continue;
                if (task.Phase == SwitchOutPhase.Completed ||
                    task.Phase == SwitchOutPhase.Cancelled)
                    continue;

                bool wasPlayingExit = (task.Phase == SwitchOutPhase.PlayingExit);

                task.Phase = SwitchOutPhase.Cancelled;
                if (member.Entity.DataModule != null)
                {
                    var switchData = member.Entity.DataModule.Get<SwitchRuntimeData>();
                    if (switchData != null) switchData.Set(nameof(switchData.IsSwitchOutPending), false);
                }

                if (wasPlayingExit)
                {
                    // 恢复碰撞体（PlayingExit 阶段已禁用）
                    member.Entity.Presentation?.SetColliderActive(true);

                    // 中断切出动作，回到根动作，时序在切入动作裁决前
                    // 如果切出动作没配转窗口不同切入动作的窗口的话，需要先转到根动作保证切入动作能顺利切入
                    // 如果配了的话，此处可以不转到根动作后续管线也能顺利切换切入动作
                 if (member.Entity.Config?.ActionRoot != null)
                    {
                        member.Entity.ActionController?.PlayAction(member.Entity.Config.ActionRoot);
                    }
                }

                GLog.Info(LogTags.Team, $"Cancelled switch-out: {member.Config?.Name}, wasPlayingExit={wasPlayingExit}");
                return true;
            }
            return false;
        }

        /// <summary> 完成切出任务：隐藏渲染并转入 Standby 状态。 </summary>
        public void CompleteSwitchOut(SwitchOutTask task)
        {
            task.Phase = SwitchOutPhase.Completed;

            RoleEntity entity = task.Member?.Entity;
            if (entity == null) return;

            if (entity.DataModule != null)
            {
                var switchData = entity.DataModule.Get<SwitchRuntimeData>();
                if (switchData != null) switchData.Set(nameof(switchData.IsSwitchOutPending), false);
            }
            entity.Presentation?.SetPresentationVisible(false);
            entity.Presentation?.SetColliderActive(false);
            entity.SetControlActive(false);

            // 播放 ActionRoot 使角色回到待机循环（Standby 维护需要）
            if (entity.Config?.ActionRoot != null)
            {
                entity.ActionController?.PlayAction(entity.Config.ActionRoot);
            }

            GLog.Info(LogTags.Team, $"Completed switch-out: {task.Member.Config?.Name}");
        }

        // ═══════════════════════════════════════════
        //  Timeline 事件处理（存量兼容）
        // ═══════════════════════════════════════════

        private bool HandleSwitchOutDisableLogic(SwitchOutTask task)
        {
            if (task.Phase != SwitchOutPhase.Pending) return false;

            task.Phase = SwitchOutPhase.PlayingExit;
            task.Member.Entity?.Presentation?.SetColliderActive(false);

            GLog.Info(LogTags.Team, $"SwitchOutDisableLogic: {task.Member.Config?.Name} → PlayingExit");
            return true;
        }

        private bool HandleHideOutgoingRole(SwitchOutTask task)
        {
            if (task.Phase != SwitchOutPhase.PlayingExit) return false;

            CompleteSwitchOut(task);
            return true;
        }

        // ═══════════════════════════════════════════
        //  辅助方法
        // ═══════════════════════════════════════════

        private SwitchOutTask FindActiveTask(RoleEntity entity)
        {
            for (int i = 0; i < _switchOutQueue.Count; i++)
            {
                SwitchOutTask task = _switchOutQueue[i];
                if (ReferenceEquals(task.Member?.Entity, entity) &&
                    task.Phase != SwitchOutPhase.Completed &&
                    task.Phase != SwitchOutPhase.Cancelled)
                {
                    return task;
                }
            }
            return null;
        }
    }
}
