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

            // 1. 霸体检测
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

            // 2. 韧性比对与打断决策
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

                // 若上下文中已有明确指定的硬直级别（如招架反击预设的 HitReactionType.Parried），完整保留！
                if (ctx.SelectedReactionType == HitReactionType.None)
                {
                    // 仅当此前未设定反应类型时，默认保底为轻受击
                    ctx.SelectedReactionType = HitReactionType.Light;
                }

                // 同步运行时受击数据
                var hitData = ctx.Victim.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.CurrentHitStunDuration = ctx.HitStunDuration;
                    hitData.SetHitReactionAxis(ctx.ReactionAxis);
                    hitData.CurrentReactionType = ctx.SelectedReactionType;
                }
            }
            else
            {
                ctx.SelectedReactionType = HitReactionType.None;
            }
        }
    }
}
