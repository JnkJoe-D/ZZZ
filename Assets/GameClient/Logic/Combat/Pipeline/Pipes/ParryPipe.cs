using UnityEngine;
using cfg.ZZZ;
using ATEditor;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 招架阻断与反制派发过滤器 (两段式解耦架构：负责伤害免疫短路与反制上下文捕获)
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

            // 0. 查询针对该攻击者的拼刀契约（契约生命周期由 ParryCaptureClip / ParryWindowClip 统一控制）
            var contract = CombatWarningManager.GetActiveContractByRole(ctx.Victim)
                        ?? (ctx.Attacker != null ? CombatWarningManager.GetActiveContract(ctx.Attacker) : null);
            if (contract != null && contract.ParryRole == ctx.Victim)
            {
                contract.IsResolved = true;
            }

            victimParryData.ParrySucceeded = true;
            victimParryData.LastParriedAttacker = ctx.Attacker;
            if (ctx.Victim != null && ctx.Attacker != null)
            {
                ctx.Victim.SetCombatContextTarget(ctx.Attacker);
            }

            ParryWeight parryWeight = contract?.Marker != null ? contract.Marker.ParryWeight : ParryWeight.Heavy;
            victimParryData.LastParryWeight = parryWeight;

            // 1. 构造本次捕获到的招架命中上下文
            var clashCtx = new ParryClashContext
            {
                Attacker = ctx.Attacker,
                Victim = ctx.Victim,
                Marker = contract?.Marker,
                HitPoint = ctx.HitPoint,
                HitDirection = ctx.HitDirection,
                ParryHitEffectId = contract != null ? contract.ParryHitEffectId : 0,
                HitStopDuration = contract != null && contract.HitStopDuration > 0f ? contract.HitStopDuration : 0.1f
            };

            // 2. 移交 ClashHandler 进行分发（跨动作存取 + 秒切，或同动作内即时消费）
            if (victimParryData.ClashHandler != null)
            {
                victimParryData.ClashHandler.OnHitCaptured(clashCtx);
            }
            else
            {
                // 保底防错：若无 Handler，退化触发招架支援成功路由
                ctx.Victim.ActionController?.TryTriggerEvent(RouteEventType.ParryAidSucceed);
            }

            // 3. 核心契约：立即短路退出当前命中流水线，确保防守方绝对不被扣除生命值！
            ctx.Abort("Parried by target", HitResultFlags.Parried);
        }
    }
}
