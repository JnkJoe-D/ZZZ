using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 削韧、霸体与受击硬直判定过滤器
    /// </summary>
    public class ResilienceAndStaggerPipe : IHitPipe
    {
        public string PipeName => "ResilienceAndStaggerPipe";
        public int Priority => 400;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.Victim == null) return;

            // 1. 评估配置中的最大受击反应级别
            HitReactionType maxReaction = HitReactionType.None;
            if (ctx.HitEffectConfig?.Effects != null)
            {
                foreach (var effect in ctx.HitEffectConfig.Effects)
                {
                    if (effect != null && effect.HitReaction > maxReaction)
                    {
                        maxReaction = effect.HitReaction;
                    }
                }
            }

            // 2. 霸体检测
            bool isSuperArmor = false;
            if (ctx.Victim.HitReactionModule != null && ctx.Victim.HitReactionModule.isSuperArmor)
            {
                isSuperArmor = true;
            }
            if (ctx.Victim.StatusModule != null && ctx.Victim.StatusModule.IsTagImmune("SuperArmor"))
            {
                isSuperArmor = true;
            }

            if (isSuperArmor)
            {
                ctx.ResultFlags |= HitResultFlags.SuperArmor;
                ctx.SelectedReactionType = HitReactionType.None;
                return;
            }

            // 3. 韧性比对与打断决策
            int interruptLevel = ctx.InterruptLevel;
            int resilience = 1;
            if (ctx.Victim.StatusModule?.Attributes != null && ctx.Victim.StatusModule.Attributes.Has(AttributeId.BaseResilience))
            {
                resilience = Mathf.RoundToInt(ctx.Victim.StatusModule.Attributes.GetCurrent(AttributeId.BaseResilience));
            }
            ctx.TargetResilience = resilience;

            bool isInterrupted = interruptLevel >= resilience;
            if (isInterrupted)
            {
                ctx.ResultFlags |= HitResultFlags.Interrupted;
                ctx.SelectedReactionType = maxReaction;

                // 同步运行时受击数据
                var hitData = ctx.Victim.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.CurrentHitStunDuration = ctx.HitStunDuration;
                    hitData.SetHitReactionAxis(ctx.ReactionAxis);
                    hitData.CurrentReactionType = maxReaction;
                }
            }
            else
            {
                ctx.SelectedReactionType = HitReactionType.None;
            }
        }
    }
}
