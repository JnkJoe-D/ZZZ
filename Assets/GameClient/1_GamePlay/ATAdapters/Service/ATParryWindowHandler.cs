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

        private ParryExecuteClip _activeExecuteClip;
        private ParryClashContext _pendingClashContext;

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
        public void OnCaptureWindowEnter()
        {
            SetIsParrying(true);
            EnsureClashContract(hitEffectId: 0, hitStopDuration: 0.1f, heavyHitEffectId: 0, heavyHitStopDuration: 0f);
        }

        /// <summary>
        /// 捕获窗口退出 (ParryCaptureClip 离开)
        /// </summary>
        public void OnCaptureWindowExit(bool isInterrupted)
        {
            SetIsParrying(false);

            // 打断时不注销契约，保持多段连招交接保护；仅在自然结束且无活跃执行窗时注销契约
            if (!isInterrupted && _activeExecuteClip == null)
            {
                CleanContracts();
            }
        }

        /// <summary>
        /// 执行窗口激活 (ParryExecuteClip 进入)
        /// </summary>
        public void OnExecuteWindowEnter(ParryExecuteClip clip)
        {
            _activeExecuteClip = clip;

            // 若有暂存的未消费招架命中上下文 (从 招架_Start 遗留)，同帧立即消费执行！
            if (_pendingClashContext != null && !_pendingClashContext.IsConsumed)
            {
                var ctx = _pendingClashContext;
                _pendingClashContext = null;
                ExecuteClash(ctx, clip);
            }
        }

        /// <summary>
        /// 执行窗口退出 (ParryExecuteClip 离开)
        /// </summary>
        public void OnExecuteWindowExit()
        {
            _activeExecuteClip = null;
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

            // 1. 暂存最新的招架命中上下文（供切入的新动作或后续执行窗消费）
            _pendingClashContext = ctx;

            // 2. 优先尝试触发动作路由（无论是 招架_Start -> 招架_L/H，还是 招架_H -> 招架_H 连续重招架动作刷新）
            bool routed = _entity.ActionController != null 
                       && _entity.ActionController.TryTriggerEvent(RouteEventType.ParryAidSucceed);

            // 3. 若路由成功，新动作的 ParryExecuteClip.OnEnter 会立即消费 _pendingClashContext 并执行 ExecuteClash；
            // 若未能成功路由（如当前动作未配置招架路由、或不在路由窗口内），且当前已有活跃的 ExecuteClip，则作为保底原地执行
            if (!routed && _activeExecuteClip != null && _pendingClashContext != null && !_pendingClashContext.IsConsumed)
            {
                var pending = _pendingClashContext;
                _pendingClashContext = null;
                ExecuteClash(pending, _activeExecuteClip);
            }
        }

        /// <summary>
        /// 招架反制效果核心执行方法
        /// </summary>
        public void ExecuteClash(ParryClashContext ctx, ParryExecuteClip clip)
        {
            if (ctx == null || ctx.Attacker == null || _entity == null) return;
            ctx.IsConsumed = true;

            // 1. 获取反制参数：优先根据招架权重选择 L/H 特效与顿帧
            bool isHeavy = ctx.Marker != null && ctx.Marker.ParryWeight == ATEditor.ParryWeight.Heavy;
            int effectId = (isHeavy && clip.heavyHitEffectId > 0) ? clip.heavyHitEffectId : clip.hitEffectId;
            float hitStopDuration = (isHeavy && clip.heavyHitStopDuration > 0f) ? clip.heavyHitStopDuration : clip.hitStopDuration;

            var hitEffectCfg = ConfigManager.Instance?.Tables?.TbHitEffect?.GetOrDefault(effectId);
            var mainEffect = hitEffectCfg?.Effects != null && hitEffectCfg.Effects.Count > 0 ? hitEffectCfg.Effects[0] : null;

            // 2. 纯数据驱动打断裁决：防守方当前真实动作打断力 vs 攻击方 (怪物) 韧性
            int parryInterruptLevel = ActionResilienceHelper.GetInterruptLevel(_entity);
            int monsterTotalResilience = ActionResilienceHelper.GetTotalResilience(ctx.Attacker);
            bool canInterruptMonster = parryInterruptLevel > 0 && parryInterruptLevel >= monsterTotalResilience;

            HitReactionType reactionType = HitReactionType.None;
            if (canInterruptMonster)
            {
                reactionType = mainEffect != null && mainEffect.HitReaction != HitReactionType.None
                    ? mainEffect.HitReaction
                    : HitReactionType.Parried;
            }

            // 3. 通用分发配表中配置的反制效果 (削韧失衡 Daze、Buff 等)
            if (hitEffectCfg?.Effects != null)
            {
                foreach (var effect in hitEffectCfg.Effects)
                {
                    switch (effect.EffectType)
                    {
                        case cfg.ZZZ.HitEffectType.ModifyAttribute:
                            if (ctx.Attacker.AttributeResolver != null)
                            {
                                ctx.Attacker.AttributeResolver.ModifyAttribute((Game.GamePlay.AttributeId)effect.AttrId, effect.Value);
                            }
                            else if (ctx.Attacker.StatusModule?.Attributes != null)
                            {
                                var targetAttrId = (Game.GamePlay.AttributeId)effect.AttrId;
                                if (ctx.Attacker.StatusModule.Attributes.Has(targetAttrId))
                                {
                                    ctx.Attacker.StatusModule.Attributes.Modify(targetAttrId, effect.Value);
                                }
                            }
                            GLog.Info(LogTags.Combat, $"[ParryExecute] 招架反制生效通用属性修改: AttrId={effect.AttrId}, Value={effect.Value} -> 目标: {ctx.Attacker.name}");
                            break;

                        case cfg.ZZZ.HitEffectType.ApplyBuff:
                            if (effect.BuffId > 0)
                            {
                                ctx.Attacker.StatusModule?.Buffs?.AddBuff(effect.BuffId, new BuffApplyContext { Instigator = _entity });
                            }
                            break;
                    }
                }
            }

            // 4. 驱动攻击者受击动作与深度顿帧反馈 (直接通过标准命中受击管线 HitPipeline，不通过表现组件反向中继)
            var pipeline = HitPipeline.Default;
            var pipeCtx = pipeline.AllocateContext();
            pipeCtx.Attacker = _entity;
            pipeCtx.Victim = ctx.Attacker;
            pipeCtx.HitPoint = ctx.Attacker != null ? ctx.Attacker.transform.position : Vector3.zero;
            Vector3 hitDir = ctx.Attacker != null ? (ctx.Attacker.transform.position - _entity.transform.position).normalized : Vector3.forward;
            pipeCtx.HitDirection = hitDir;
            pipeCtx.ReactionAxis = -hitDir;
            pipeCtx.InterruptLevel = canInterruptMonster ? parryInterruptLevel : 0;
            pipeCtx.SelectedReactionType = reactionType;
            pipeCtx.EnableHitStop = true;
            pipeCtx.HitStopDuration = hitStopDuration;
            pipeCtx.HitStopScale = 0f;
            pipeCtx.ResultFlags |= HitResultFlags.Parried;

            pipeline.Execute(pipeCtx);
            pipeline.ReleaseContext(pipeCtx);

            // 5. 防守方自身顿帧 (通过领域事件请求时间系统调度)
            EventCenter.Publish(new HitStopRequestEvent(_entity?.Clock, null, hitStopDuration, 0f));
        }

        #endregion

        #region Backward Compatibility (Legacy ParryWindowClip)

        public void OnParryWindowEnter(
            int hitEffectId = 0, 
            float hitStopDuration = 0.1f, 
            int heavyHitEffectId = 0, 
            float heavyHitStopDuration = 0f)
        {
            SetIsParrying(true);
            EnsureClashContract(hitEffectId, hitStopDuration, heavyHitEffectId, heavyHitStopDuration);
        }

        public void OnParryWindowExit(bool isInterrupted)
        {
            OnCaptureWindowExit(isInterrupted);
        }

        public void SetParryWindowActive(bool active)
        {
            if (active) OnCaptureWindowEnter();
            else OnCaptureWindowExit(isInterrupted: false);
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

        private void EnsureClashContract(int hitEffectId, float hitStopDuration, int heavyHitEffectId, float heavyHitStopDuration)
        {
            AttackWarningMarker marker = null;
            var actionData = _entity?.DataModule?.Get<ActionRuntimeData>();
            if (actionData != null && actionData.MatchedWarningMarker != null)
            {
                marker = actionData.MatchedWarningMarker;
            }
            else if (_entity != null)
            {
                marker = CombatWarningManager.GetValidWarning(_entity, WarningSignalType.Yellow_Parryable);
            }

            var existingContract = CombatWarningManager.GetActiveContractByRole(_entity);
            if (existingContract != null && existingContract.IsValid)
            {
                _currentContract = existingContract;
                if (marker == null) marker = existingContract.Marker;

                bool isHeavy = marker != null && marker.ParryWeight == ATEditor.ParryWeight.Heavy;
                int finalEffectId = (isHeavy && heavyHitEffectId > 0) ? heavyHitEffectId : hitEffectId;
                float finalHitStop = (isHeavy && heavyHitStopDuration > 0f) ? heavyHitStopDuration : hitStopDuration;

                _currentContract.HitStopDuration = finalHitStop;
                if (finalEffectId > 0) _currentContract.ParryHitEffectId = finalEffectId;
                return;
            }

            if (marker != null && marker.Attacker != null && marker.Attacker.gameObject.activeInHierarchy)
            {
                bool isHeavy = marker.ParryWeight == ATEditor.ParryWeight.Heavy;
                int finalEffectId = (isHeavy && heavyHitEffectId > 0) ? heavyHitEffectId : hitEffectId;
                float finalHitStop = (isHeavy && heavyHitStopDuration > 0f) ? heavyHitStopDuration : hitStopDuration;

                _currentContract = new ParryClashContract
                {
                    Attacker = marker.Attacker,
                    ParryRole = _entity,
                    Marker = marker,
                    IsResolved = false,
                    ParryHitEffectId = finalEffectId,
                    HitStopDuration = finalHitStop
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
