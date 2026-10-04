using System;
using System.Collections.Generic;
using Game.Framework;
using UnityEngine;
using ATEditor;
using cfg.ZZZ;

namespace Game.GamePlay
{
    /// <summary>
    /// 统一招架防御窗口适配器：
    /// 驱动时间轴 ParryWindowClip 生命周期 (IParryWindowHandler)
    /// 并处理命中流水线拼刀反制 (IParryClashHandler)。
    /// </summary>
    public class ATParryWindowHandler : IParryWindowHandler, IParryClashHandler
    {
        private readonly CharacterEntity _entity;
        private ParryClashContract _currentContract;
        private readonly HashSet<ParryWindowClip> _activeWindows = new();
        private ParryWindowClip _currentClip;

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

        #region IParryWindowHandler (Unified Parry Window Lifecycle)

        /// <summary>
        /// 招架防御有效窗口进入 (ParryWindowClip 进入)
        /// </summary>
        public void OnParryWindowEnter(ParryWindowClip clip)
        {
            if (clip == null) return;

            bool isFirst = _activeWindows.Count == 0;
            _activeWindows.Add(clip);
            _currentClip = clip;

            if (isFirst)
            {
                SetIsParrying(true);
                EnsureClashContract();
                GLog.Info(LogTags.Combat, $"[ATParryWindowHandler] 实体 {_entity?.name} 开启招架防御窗口: {clip.clipName}, 时长: {clip.Duration}s");
            }
        }

        /// <summary>
        /// 招架防御有效窗口离开 (ParryWindowClip 退出)
        /// </summary>
        public void OnParryWindowExit(ParryWindowClip clip, bool isInterrupted)
        {
            if (clip == null) return;
            _activeWindows.Remove(clip);

            if (_activeWindows.Count == 0)
            {
                SetIsParrying(false);
                _currentClip = null;
                CleanContracts();
                GLog.Info(LogTags.Combat, $"[ATParryWindowHandler] 实体 {_entity?.name} 退出招架防御窗口 (被打断: {isInterrupted})");
            }
        }

        #endregion

        #region IParryClashHandler (Combat Pipeline Interaction)

        /// <summary>
        /// 战斗流水线 (ParryPipe) 捕获到命中时的入口。
        /// 严格遵循绝区零招架物理与视听时序：
        /// 1. 玩家进入招架成功动作（切入架刀/格挡姿态）；
        /// 2. 攻守双方同时进入顿帧（攻击者在挥刀攻击帧定格，玩家在格挡架刀姿态定格，怪物尚未被打断）；
        /// 3. 等双方顿帧结束后，触发回调：怪物的本次攻击才被打断并播放打断受击动作（若之前裁决为被打断）。
        /// </summary>
        public void OnHitCaptured(ParryClashContext ctx)
        {
            if (ctx == null)
            {
                GLog.Warning(LogTags.Combat, "[ATParryWindowHandler] OnHitCaptured 收到空的 clashContext，忽略");
                return;
            }

            // 1. 玩家进入招架成功动作（物理命中直接触发切入/重入正式招架轻/重反击动作，进入首帧格挡架刀姿态）
            _entity?.ActionController?.TryTriggerEvent(RouteEventType.ParryAid);

            // 2. 根据轻重招架读取配置的反制效果与顿帧时长
            var parryWeight = ctx.Marker != null ? ctx.Marker.ParryWeight : ParryWeight.Heavy;
            var parryEntry = (_entity as RoleEntity)?.Config?.AssistConfig?.GetParryEntry(parryWeight);
            if(parryEntry == null)
            {
                GLog.Warning(LogTags.Combat, $"[ATParryWindowHandler] 实体 {_entity?.name} 的招架配置中未找到 {parryWeight} 招架条目，无法执行反制");
                return;
            }
            int effectId = parryEntry.HitEffectId;
            float stopDur = parryEntry.HitStopDuration;

            if(effectId <=0 )
            {
                GLog.Warning(LogTags.Combat, $"[ATParryWindowHandler] 实体 {_entity?.name} 的招架条目 {parryWeight} 未配置有效的 HitEffectId，无法执行反制");
                return;
            }
            // 3. 定义顿帧结束后的打断与反震受击动作回调
            Action onHitStopComplete = () =>
            {
                // 等双方顿帧完全结束后，怪物的本次攻击才真正被打断并播放打断受击动作（如果之前裁决为被打断）
                ExecuteClash(ctx, effectId, 0f);

                // 兜底保障：若受击组件未生效，且裁决为打断，则下发受击动作打断
                var hitData = ctx.Attacker?.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData == null || !hitData.InHitReaction)
                {
                    ApplyAttackerParriedReaction(ctx);
                }

                // 连续招架支持：契约注销延后至 OnParryWindowExit 统一执行，确保招架窗口期内后续连续攻击仍能正确识别契约
            };

            // 4. 攻守双方同时进入顿帧，并在顿帧结束时触发打断回调
            if (TimeManager.Instance != null && (ctx.Attacker?.Clock != null || _entity?.Clock != null))
            {
                TimeManager.Instance.RegisterHitStop(ctx.Attacker?.Clock, _entity?.Clock, stopDur, 0f, onHitStopComplete);
            }
            else
            {
                // 无时钟环境（如纯逻辑单元测试）自愈保底：直接同步执行回调
                onHitStopComplete.Invoke();
            }
        }

        /// <summary>
        /// 招架成功反震攻击者打断动作下发（仅在受击组件未生效时兜底）
        /// </summary>
        private void ApplyAttackerParriedReaction(ParryClashContext clashCtx)
        {
            if (clashCtx == null || clashCtx.Attacker == null)
            {
                GLog.Warning(LogTags.Combat, "[ATParryWindowHandler] ApplyAttackerParriedReaction clashCtx 或 Attacker 为空");
                return;
            }

            var preData = clashCtx.PrecomputedData;
            if (preData == null || !preData.IsValid)
            {
                var contract = CombatWarningManager.GetActiveContract(clashCtx.Attacker);
                if (contract != null)
                {
                    if (contract.PrecomputedData == null || !contract.PrecomputedData.IsValid)
                    {
                        ParryPreArbitrator.Precompute(contract);
                    }
                    preData = contract.PrecomputedData;
                }
            }

            if (preData != null && preData.IsValid && preData.WillInterrupt && preData.TargetHitAction != null)
            {
                var hitCmd = CharacterCommandFactory.CreateDirectAssetCommand(preData.TargetHitAction);
                clashCtx.Attacker.ActionController?.OnInputAndResolveImmediately(hitCmd);

                // 兜底同步运行时硬直数据，防止行为树无感知抢占
                var hitData = clashCtx.Attacker.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.Set(nameof(hitData.InHitReaction), true);
                    hitData.Set(nameof(hitData.HitTriggerTimestamp), Time.frameCount);
                    hitData.Set(nameof(hitData.ResolvedHitAction), preData.TargetHitAction);
                }
            }
        }

        /// <summary>
        /// 招架反制效果核心执行方法
        /// </summary>
        public void ExecuteClash(ParryClashContext ctx, int hitEffectId, float hitStopDuration)
        {
            if (ctx == null || ctx.Attacker == null || _entity == null) return;
            ctx.IsConsumed = true;

            // 1. 获取反制配置 (由具体动作时间轴 ParryWindowClip 传入的 hitEffectId 驱动)
            var hitEffectCfg = ConfigManager.Instance?.Tables?.TbHitEffect?.GetOrDefault(hitEffectId);

            // 2. 纯数据驱动打断力：防守方招架反制动作的打断等级（必须取反击动作的打断力，而非起手架势动作）
            var parryWeight = ctx.Marker != null ? ctx.Marker.ParryWeight : ParryWeight.Heavy;
            var parryEntry = (_entity as RoleEntity)?.Config?.AssistConfig?.GetParryEntry(parryWeight);
            int parryInterruptLevel = 0;
            if (parryEntry?.Action != null)
            {
                parryInterruptLevel = ActionResilienceHelper.GetInterruptLevelById(parryEntry.Action.ID);
            }
            if (parryInterruptLevel <= 0)
            {
                parryInterruptLevel = ActionResilienceHelper.GetInterruptLevel(_entity);
            }
            if (parryInterruptLevel <= 0)
            {
                // 保底机制：招架反制必定具有顶级打断力（轻招架 2 / 重招架 5），压制怪物常规攻击
                parryInterruptLevel = parryWeight == ParryWeight.Heavy ? 5 : 2;
            }

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
            pipeCtx.EnableHitStop = hitStopDuration > 0f;
            pipeCtx.HitStopDuration = hitStopDuration;
            pipeCtx.HitStunDuration = hitStopDuration;
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
                var attackerContract = CombatWarningManager.GetActiveContract(marker.Attacker);
                if (attackerContract != null)
                {
                    attackerContract.ParryRole = _entity;
                    attackerContract.Marker = marker;
                    _currentContract = attackerContract;
                    ParryPreArbitrator.Precompute(_currentContract);
                    return;
                }

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
