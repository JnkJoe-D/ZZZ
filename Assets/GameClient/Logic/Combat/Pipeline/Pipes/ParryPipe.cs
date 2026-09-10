using UnityEngine;
using cfg.ZZZ;
using ATEditor;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 招架阻断与弹刀反击过滤器
    /// </summary>
    public class ParryPipe : IHitPipe
    {
        public string PipeName => "ParryPipe";
        public int Priority => 200;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.Victim == null) return;

            var victimParryData = ctx.Victim.DataModule?.Get<ParryRuntimeData>();
            if (victimParryData == null || !victimParryData.IsParrying) return;

            // 1. 获取攻击者危险警示重量
            var warningMarker = CombatWarningManager.GetWarningByAttacker(ctx.Attacker);
            var weight = warningMarker?.Weight ?? AttackWeight.Light_Interruptible;

            victimParryData.ParrySucceeded = true;
            victimParryData.LastParriedAttacker = ctx.Attacker;
            victimParryData.LastParriedWeight = weight;

            // 2. 状态标记与顿帧配置
            ctx.ResultFlags |= HitResultFlags.Parried;
            ctx.SelectedReactionType = HitReactionType.Parried;
            ctx.EnableHitStop = true;
            ctx.HitStopDuration = ctx.RawHitData.hitStopDuration > 0 ? ctx.RawHitData.hitStopDuration : 0.12f;
            ctx.HitStopScale = 0f;

            // 3. 招架反馈：如果是轻量攻击，攻击者进入弹刀硬直
            if (weight == AttackWeight.Light_Interruptible && ctx.Attacker != null)
            {
                var parryCtx = new HitContext
                {
                    attacker = ctx.Victim,
                    victim = ctx.Attacker,
                    IsParry = true,
                    interruptLevel = 999,
                    reactionType = HitReactionType.Parried,
                    hitDirection = (ctx.Attacker.transform.position - ctx.Victim.transform.position).normalized,
                    enableHitStop = true,
                    hitStopDuration = ctx.HitStopDuration,
                    hitStopScale = 0f
                };
                parryCtx.reactionAxis = -parryCtx.hitDirection;
                ctx.Attacker.HitReactionModule?.ApplyVisualFeedback(parryCtx);
            }

            // 4. 触发防守方招架支援成功事件
            ctx.Victim.ActionController?.TryTriggerEvent(RouteEventType.ParryAidSucceed);

            // 5. 阻断后续伤害与受击打断逻辑（短路）
            ctx.Abort("Parried by target", HitResultFlags.Parried);
        }
    }
}
