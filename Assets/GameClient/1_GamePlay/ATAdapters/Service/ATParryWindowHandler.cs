using System.Collections.Generic;
using Game.Framework;
using UnityEngine;
using ATEditor;
using cfg.ZZZ;

namespace Game.GamePlay
{
    /// <summary>
    /// 统一招架适配器：同时实现时间轴 Clip 引擎生命周期驱动 (IParryWindowHandler) 
    /// 与战斗流水线拼刀反制流转 (IParryClashHandler)，构建捕获-执行两段式解耦架构。
    /// </summary>
    public class ATParryWindowHandler : IParryWindowHandler, IParryClashHandler
    {
        private readonly CharacterEntity _entity;
        private ParryClashContract _currentContract;

        private ParryClashContext _pendingClashContext;
        private readonly HashSet<ParryCaptureData> _activeCaptureWindows = new();

        public ATParryWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
            InitParryData();
        }

        private void InitParryData()
        {
            if (_entity?.DataModule != null)
            {
                var parryData = _entity.DataModule.Get<ParryRuntimeData>();
                if (parryData != null)
                {
                    parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)this);
                }
            }
        }

        #region IParryWindowHandler (New Decoupled Lifecycle)

        /// <summary>
        /// 捕获窗口激活 (ParryCaptureClip 进入)
        /// </summary>
        public void OnCaptureWindowEnter(ParryCaptureData data)
        {
            if (data == null) return;

            bool isFirst = _activeCaptureWindows.Count == 0;
            _activeCaptureWindows.Add(data);

            // 仅在首个招架窗口进入时初始化状态与契约；后续相邻或重叠片段进入时绝不重置 _pendingClashContext
            if (isFirst)
            {
                _pendingClashContext = null;
                SetIsParrying(true);
                EnsureClashContract();
            }
        }

        /// <summary>
        /// 捕获窗口退出 (ParryCaptureClip 离开)
        /// </summary>
        public void OnCaptureWindowExit(ParryCaptureData data, bool isInterrupted)
        {
            if (data == null) return;
            _activeCaptureWindows.Remove(data);

            bool triggeredSucceed = false;
            // 若本片段为 OnExit 触发时机且未被打断，且存在捕获到的未消费命中上下文，则在退出时发布招架成功路由
            if (!isInterrupted && data.triggerTiming == ParryCaptureTriggerTiming.OnExit)
            {
                if (_pendingClashContext != null && !_pendingClashContext.IsConsumed)
                {
                    triggeredSucceed = _entity.ActionController != null &&
                                       _entity.ActionController.TryTriggerEvent(RouteEventType.ParryAidSucceed);
                }
            }

            // 只有当所有捕获窗口全部退出后，才关闭招架状态
            if (_activeCaptureWindows.Count == 0)
            {
                SetIsParrying(false);

                // 打断或成功触发招架派生反击时不注销契约，保持动作交接保护；仅在自然无招架结束时注销契约
                if (!isInterrupted && !triggeredSucceed)
                {
                    CleanContracts();
                }
            }
        }

        /// <summary>
        /// 执行窗口激活 (ParryExecuteClip 进入)
        /// </summary>
        public void OnExecuteWindowEnter(int hitEffectId, float hitStopDuration)
        {
            // 若有暂存的未消费招架命中上下文 (从 招架_Start 遗留)，同帧立即消费执行！
            if (_pendingClashContext != null && !_pendingClashContext.IsConsumed)
            {
                var ctx = _pendingClashContext;
                _pendingClashContext = null;
                ExecuteClash(ctx, hitEffectId, hitStopDuration);
            }
        }

        /// <summary>
        /// 执行窗口退出 (ParryExecuteClip 离开)
        /// </summary>
        public void OnExecuteWindowExit()
        {
            CleanContracts();
        }

        #endregion

        #region IParryClashHandler (Combat Pipeline Interaction)

        /// <summary>
        /// 战斗流水线 (ParryPipe) 捕获到命中时的入口
        /// </summary>
        public void OnHitCaptured(ParryClashContext ctx)
        {
            if (ctx == null) return;

            // 1. 暂存最新的招架命中上下文（供切入的新动作执行窗消费）
            _pendingClashContext = ctx;

            // 2. 检查当前处于活跃状态的捕获窗口中是否存在 Instant 模式
            bool hasInstantWindow = false;
            foreach (var win in _activeCaptureWindows)
            {
                if (win.triggerTiming == ParryCaptureTriggerTiming.Instant)
                {
                    hasInstantWindow = true;
                    break;
                }
            }

            // 3. 若存在 Instant 模式窗口，瞬时触发招架成功路由；若当前全为 OnExit 窗口，则保持格挡架势等待 OnExit
            if (hasInstantWindow)
            {
                _entity.ActionController?.TryTriggerEvent(RouteEventType.ParryAidSucceed);
            }
        }

        /// <summary>
        /// 招架反制效果核心执行方法
        /// </summary>
        public void ExecuteClash(ParryClashContext ctx, int hitEffectId, float hitStopDuration)
        {
            if (ctx == null || ctx.Attacker == null || _entity == null) return;
            ctx.IsConsumed = true;

            // 1. 获取反制配置 (由具体动作时间轴 ParryExecuteClip 传入的 hitEffectId 驱动)
            var hitEffectCfg = ConfigManager.Instance?.Tables?.TbHitEffect?.GetOrDefault(hitEffectId);

            // 2. 纯数据驱动打断力：防守方当前真实招架动作的打断等级
            int parryInterruptLevel = ActionResilienceHelper.GetInterruptLevel(_entity);

            // 3. 构建标准受击管线上下文，由 HitPipeline 权威统筹执行全部数值属性、Buff、动作打断、转向与顿帧
            var pipeline = HitPipeline.Default;
            var pipeCtx = pipeline.AllocateContext();
            pipeCtx.Attacker = _entity;
            pipeCtx.Victim = ctx.Attacker;
            pipeCtx.HitEffectConfig = hitEffectCfg;
            pipeCtx.HitPoint = ctx.Attacker.transform.position;
            Vector3 hitDir = (ctx.Attacker.transform.position - _entity.transform.position).normalized;
            pipeCtx.HitDirection = hitDir;
            pipeCtx.ReactionAxis = -hitDir;
            pipeCtx.InterruptLevel = parryInterruptLevel;
            pipeCtx.EnableHitStop = true;
            pipeCtx.HitStopDuration = hitStopDuration;
            pipeCtx.HitStopScale = 0f;
            pipeCtx.ResultFlags |= HitResultFlags.Parried;

            pipeline.Execute(pipeCtx);
            pipeline.ReleaseContext(pipeCtx);
        }

        #endregion

        #region Helper Methods

        private void SetIsParrying(bool isParrying)
        {
            if (_entity?.DataModule != null)
            {
                var parryData = _entity.DataModule.Get<ParryRuntimeData>();
                if (parryData != null)
                {
                    parryData.Set(nameof(parryData.IsParrying), isParrying);
                    parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)this);
                }
            }
        }

        private void EnsureClashContract()
        {
            AttackWarningMarker marker = null;
            var actionData = _entity?.DataModule?.Get<ActionRuntimeData>();
            if (actionData != null && actionData.MatchedWarningMarker != null)
            {
                marker = actionData.MatchedWarningMarker;
            }

            var existingContract = CombatWarningManager.GetActiveContractByRole(_entity);
            if (existingContract != null && existingContract.IsValid)
            {
                _currentContract = existingContract;
                return;
            }

            if (marker != null && marker.Attacker != null && marker.Attacker.gameObject.activeInHierarchy)
            {
                _currentContract = new ParryClashContract
                {
                    Attacker = marker.Attacker,
                    ParryRole = _entity,
                    Marker = marker,
                    IsResolved = false
                };
                CombatWarningManager.RegisterContract(_currentContract);
            }
        }

        private void CleanContracts()
        {
            if (_currentContract != null)
            {
                CombatWarningManager.UnregisterContract(_currentContract);
                _currentContract = null;
            }
            if (_entity != null)
            {
                CombatWarningManager.UnregisterContractsByRole(_entity);
            }
        }

        #endregion
    }
}
