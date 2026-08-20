using System.Collections.Generic;
using Game.Camera;
using Game.Framework;
using UnityEngine;

namespace Game.Logic
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

        /// <summary>
        /// 创建任务时的激活版本号。
        /// 用于防腐校验：如果 Member.ActivationVersion 已变化，
        /// 说明该角色已被重新激活过，此任务应视为过期。
        /// </summary>
        public int CreationVersion;
    }

    /// <summary>
    /// 切人执行器 — 纯 C# 类。
    ///
    /// 职责：
    ///   1. 订阅 ActionRouteExecuteEvent，响应 SwitchCaptureSucceed 事件
    ///   2. 执行切入流程：断输入 → 激活切入角色 → 切相机 → 切出角色入队
    ///   3. 管理切出任务队列：支持并行退场、取消/复活、状态机推进
    ///   4. 监听 CharacterTimelineEvent，驱动切出动作的阶段过渡
    ///
    /// 设计要点：
    ///   - 切出角色的退场由其自身 ActionController 的条件路由
    ///     （ConditionCommand.SwitchOutPending）自然驱动，本类不主动干预
    ///   - 切出动作的 Timeline 事件（SwitchOutDisableLogic / HideOutgoingRole）
    ///     通过已有的 CharacterTimelineEvent 机制传入本类
    /// </summary>
    public class SwitchExecutor
    {
        // ─── 常量：Timeline 事件名 ───
        private const string EventSwitchOutDisableLogic = "SwitchOutDisableLogic";
        private const string EventHideOutgoingRole = "HideOutgoingRole";

        // ─── 字段 ───
        private readonly TeamManager _manager;
        private readonly List<SwitchOutTask> _switchOutQueue = new();

        /// <summary> 是否有正在进行中的切出任务。 </summary>
        public bool IsSwitching => _switchOutQueue.Count > 0;

        // ─── 构造与生命周期 ───

        public SwitchExecutor(TeamManager manager)
        {
            _manager = manager;
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
        /// 每帧更新，清理已完成/已取消的任务。
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
        //  事件响应
        // ═══════════════════════════════════════════

        /// <summary>
        /// ActionRouteExecuteEvent 的回调入口。
        /// 仅处理 SwitchCaptureSucceed 事件。
        /// </summary>
        private void OnActionRouteEvent(ActionRouteExecuteEvent evt)
        {
            if (evt.Event != ExecuteEvent.SwitchCaptureSucceed) return;
            if (evt.SourceEntity == null) return;

            PartyMember outgoing = _manager.FindPartyMember(evt.SourceEntity);
            if (outgoing == null) return;

            PartyMember incoming = ResolveIncoming(outgoing, evt.TargetSlotHint);
            if (incoming == null || incoming == outgoing) return;

            ExecuteSwitch(outgoing, incoming);
        }

        /// <summary>
        /// Timeline 事件的回调入口。
        /// 由 TeamManager.HandleTimelineEvent 转发。
        /// 处理切出动作的阶段过渡事件。
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
        //  切入执行
        // ═══════════════════════════════════════════

        /// <summary>
        /// 执行一次完整的切人操作。
        ///
        /// 步骤：
        ///   1. 如果 incoming 在切出队列中 → 取消其切出任务（复活）
        ///   2. 切断切出角色的输入接收
        ///   3. 激活切入角色（位置/朝向/控制权/相机/DebugHud）
        ///   4. 将切出角色加入切出任务队列
        /// </summary>
        private void ExecuteSwitch(PartyMember outgoing, PartyMember incoming)
        {
            Debug.Log($"[SwitchExecutor] ExecuteSwitch: {outgoing.Config?.Name} → {incoming.Config?.Name}");

            RoleEntity outEntity = outgoing.Entity;
            RoleEntity inEntity = incoming.Entity;

            if (outEntity == null || inEntity == null) return;

            // 1. 如果 incoming 在切出队列中，取消其切出任务
            TryCancelSwitchOut(incoming);

            // 2. 切断切出角色输入（不禁用共享 InputProvider，仅解绑事件适配器）
            outEntity.SetControlActive(false, assignCameraTarget: false);

            // 3. 激活切入角色
            //    ActivatePartyMember 内部完成：
            //    - 位置/朝向同步
            //    - SetPresentationVisible(true)
            //    - SetControlActive(true) + BindInput
            //    - SetCameraRigActive(true)
            //    - GameCameraManager.SetTarget（通过 assignCameraTarget）
            //    - TeamContext.SetActiveRole
            //    - UpdatePartyDebugHudVisibility（同时隐藏切出角色的 HUD）
            //    - ActivationVersion++
            //    - LocalCharacter 赋值
            Vector3 switchPos = outEntity.transform.position;
            Quaternion switchRot = outEntity.transform.rotation;
            _manager.ActivatePartyMember(incoming, switchPos, switchRot, assignCameraTarget: true);

            // 4. 切出角色入队
            EnqueueSwitchOut(outgoing);

            // 5. 尝试立即触发切出（通过事件路由 RouteEventType.SwitchOut）
            //    如果切出角色当前动作有 SwitchOut 事件路由且窗口开放 → 立即播放切出动作
            //    如果失败 → 由条件路由（ConditionCommand.SwitchOutPending）在后续帧/窗口中自然驱动
            inEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchIn);
            bool resOut = outEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchOut) == true;
        }

        // ═══════════════════════════════════════════
        //  切出任务队列管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 将角色加入切出队列。
        /// 设置 IsSwitchOutPending 标志，使角色自身路由系统
        /// （ConditionCommand.SwitchOutPending）能检测到并触发切出动作。
        /// </summary>
        private void EnqueueSwitchOut(PartyMember member)
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

            if (member.Entity.DataModule != null)
            {
                var switchData = member.Entity.DataModule.Get<SwitchRuntimeData>();
                if (switchData != null) switchData.IsSwitchOutPending = true;
            }

            _switchOutQueue.Add(new SwitchOutTask
            {
                Member = member,
                Phase = SwitchOutPhase.Pending,
                CreationVersion = member.ActivationVersion
            });

            Debug.Log($"[SwitchExecutor] Enqueued switch-out: {member.Config?.Name}, Version={member.ActivationVersion}");
        }

        /// <summary>
        /// 尝试取消指定角色的切出任务（角色被重新切入时调用）。
        /// 根据当前阶段执行不同的恢复操作：
        ///   - Pending：直接取消，角色继续当前动作
        ///   - PlayingExit：中断切出动作，恢复碰撞体，播放 ActionRoot 重置
        /// </summary>
        private bool TryCancelSwitchOut(PartyMember member)
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
                    if (switchData != null) switchData.IsSwitchOutPending = false;
                }

                if (wasPlayingExit)
                {
                    // 恢复碰撞体（PlayingExit 阶段已禁用）
                    member.Entity.SetColliderActive(true);

                    // 中断切出动作，回到根动作
                    if (member.Entity.Config?.ActionRoot != null)
                    {
                        member.Entity.ActionController?.PlayAction(member.Entity.Config.ActionRoot);
                    }
                }

                Debug.Log($"[SwitchExecutor] Cancelled switch-out: {member.Config?.Name}, wasPlayingExit={wasPlayingExit}");
                return true;
            }
            return false;
        }

        /// <summary> 完成切出任务：隐藏渲染并转入 Standby 状态。 </summary>
        private void CompleteSwitchOut(SwitchOutTask task)
        {
            task.Phase = SwitchOutPhase.Completed;

            RoleEntity entity = task.Member?.Entity;
            if (entity == null) return;

            if (entity.DataModule != null)
            {
                var switchData = entity.DataModule.Get<SwitchRuntimeData>();
                if (switchData != null) switchData.IsSwitchOutPending = false;
            }
            entity.SetPresentationVisible(false);
            entity.SetControlActive(false, assignCameraTarget: false);

            // 播放 ActionRoot 使角色回到待机循环（Standby 维护需要）
            if (entity.Config?.ActionRoot != null)
            {
                entity.ActionController?.PlayAction(entity.Config.ActionRoot);
            }

            Debug.Log($"[SwitchExecutor] Completed switch-out: {task.Member.Config?.Name}");
        }

        // ═══════════════════════════════════════════
        //  Timeline 事件处理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 处理 "SwitchOutDisableLogic" 事件。
        /// 切出动作开始播放后发送，标志角色正式进入退场阶段：
        ///   - 阶段: Pending → PlayingExit
        ///   - 禁用碰撞体（不可碰撞）
        ///   - 角色仍然可见
        /// </summary>
        private bool HandleSwitchOutDisableLogic(SwitchOutTask task)
        {
            if (task.Phase != SwitchOutPhase.Pending) return false;

            task.Phase = SwitchOutPhase.PlayingExit;
            task.Member.Entity?.SetColliderActive(false);

            Debug.Log($"[SwitchExecutor] SwitchOutDisableLogic: {task.Member.Config?.Name} → PlayingExit");
            return true;
        }

        /// <summary>
        /// 处理 "HideOutgoingRole" 事件。
        /// 切出动作即将结束时发送，执行最终的隐藏操作：
        ///   - 隐藏渲染
        ///   - 标记任务完成
        /// </summary>
        private bool HandleHideOutgoingRole(SwitchOutTask task)
        {
            if (task.Phase != SwitchOutPhase.PlayingExit) return false;

            CompleteSwitchOut(task);
            return true;
        }

        // ═══════════════════════════════════════════
        //  辅助方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// 解析切入目标。
        /// 优先使用 slotHint 指定的插槽，否则使用轮转规则（下一个插槽）。
        /// 不会跳过在切出队列中的角色（切入时自动取消其切出任务）。
        /// </summary>
        private PartyMember ResolveIncoming(PartyMember outgoing, int slotHint)
        {
            IReadOnlyList<PartyMember> members = _manager.PartyMembers;
            if (members.Count <= 1) return null;

            // 优先使用提示插槽
            if (slotHint >= 0 && slotHint < members.Count)
            {
                PartyMember hinted = members[slotHint];
                if (hinted != outgoing && hinted.Entity != null)
                    return hinted;
            }

            // 默认轮转：下一个插槽
            int startIndex = (outgoing.SlotIndex + 1) % members.Count;
            for (int attempt = 0; attempt < members.Count; attempt++)
            {
                int index = (startIndex + attempt) % members.Count;
                PartyMember candidate = members[index];
                if (candidate != outgoing && candidate.Entity != null)
                    return candidate;
            }

            return null;
        }

        /// <summary> 根据角色实体查找其活跃的切出任务。 </summary>
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

        // ─── 兼容旧接口（将逐步废弃） ───

        /// <summary> [已废弃] 旧的准备切人接口，新架构由事件驱动。 </summary>
        public bool PrepareSwitch(RoleEntity outgoingEntity)
        {
            Debug.LogWarning("[SwitchExecutor] PrepareSwitch 已废弃，切人应通过 ActionRouteExecuteEvent 驱动。");
            return false;
        }
    }
}
