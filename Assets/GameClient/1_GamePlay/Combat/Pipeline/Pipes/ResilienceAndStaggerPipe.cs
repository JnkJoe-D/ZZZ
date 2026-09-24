using UnityEngine;
using cfg.ZZZ;
using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 削韧、霸体与受击硬直判定过滤器 (基于 Luban 技能表数据驱动)
    /// </summary>
    public class ResilienceAndStaggerPipe : IHitPipe
    {
        public string PipeName => "ResilienceAndStaggerPipe";
        public int Priority => 400;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.Victim == null) return;

            // 1. 绝对霸体免疫检测（统一由 StatusModule 状态标签权威判定）
            bool isSuperArmor = ctx.Victim.StatusModule != null && ctx.Victim.StatusModule.IsTagImmune("SuperArmor");

            if (isSuperArmor)
            {
                ctx.ResultFlags |= HitResultFlags.SuperArmor;
                ctx.SelectedReactionType = HitReactionType.None;
                return;
            }

            // 2. 纯数据驱动打断力与总韧性裁决
            // 攻击方打断等级：严格由攻击方当前出招配置提供，未配表或找不到严格为 0，绝不擅自篡改为 1
            int interruptLevel = ctx.InterruptLevel;

            // 受击方总韧性：基础韧性 + Buff属性加成 + 当前出招动作Luban韧性加成
            int totalResilience = ActionResilienceHelper.GetTotalResilience(ctx.Victim);
            ctx.TargetResilience = totalResilience;

            // 只有打断等级 > 0 且大于等于受击方总韧性时，才产生动作打断
            bool isInterrupted = interruptLevel > 0 && interruptLevel >= totalResilience;
            if (isInterrupted)
            {
                ctx.ResultFlags |= HitResultFlags.Interrupted;

                // 同步运行时受击数据
                var hitData = ctx.Victim.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.Set(nameof(hitData.CurrentHitStunDuration), ctx.HitStunDuration);
                    hitData.SetHitReactionAxis(ctx.ReactionAxis);
                    ctx.SelectedReactionType = ResolveHitReactionType(ctx?.HitEffectConfig?.Effects, ctx);
                    hitData.Set(nameof(hitData.CurrentReactionType), ctx.SelectedReactionType);
                }
            }
            else
            {
                // 未被打断：霸体硬抗，只受伤害/顿帧，不进入受击动作/硬直
                ctx.ResultFlags |= HitResultFlags.SuperArmor;
                ctx.SelectedReactionType = HitReactionType.None;
            }
        }
        private HitReactionType ResolveHitReactionType(List<HitEffectData> hitEffects, HitPipelineContext ctx)
        {
            if (hitEffects == null || hitEffects.Count == 0) return HitReactionType.None;
            HitReactionType result = HitReactionType.None;

            foreach (var effect in hitEffects)
            {
                if (effect == null || effect.EffectTarget != EffectTarget.Victim) continue;
                // 只取第一个匹配的受击效果类型，后续的忽略（Luban表格设计原则：同一招式不允许多种受击效果同时生效）
                if(effect.HitReaction != HitReactionType.None)
                {
                    result = effect.HitReaction;
                    break;
                }
            }
            return result;
        }
    }
}
