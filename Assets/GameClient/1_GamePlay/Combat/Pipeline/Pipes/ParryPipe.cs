using UnityEngine;
using cfg.ZZZ;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 招架阻断与反制派发过滤器 (两段式解耦架构：负责伤害免疫短路与反制上下文捕获)
    /// </summary>
    public class ParryPipe : IHitPipe
    {
        public string PipeName => "ParryPipe";
        public int Priority => 100;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.Victim == null) return;

            var victimParryData = ctx.Victim.DataModule?.Get<ParryRuntimeData>();
            if (victimParryData == null || !victimParryData.IsParrying) return;

            // 0. 查询针对该攻击者的拼刀契约（契约生命周期由 ParryContractClip / ParryCaptureClip 统一控制）
            var contract = CombatWarningManager.GetActiveContractByRole(ctx.Victim);

            // ★ 核心目标过滤守卫：
            // 若当前防守方已持有拼刀契约，但本次打过来的攻击者不是契约目标（例如怪 A 连招间隙中被怪 B 偷袭）：
            // 坚决不作为招架捕获，直接放行给下游 ProtectionPipe 由动作时间轴配置的无敌 Buff 吸收！
            if (contract != null && ctx.Attacker != contract.Attacker)
            {
                return;
            }

            // 若防守方尚未直接缓存契约，检查攻击方是否持有针对本角色的有效契约
            if (contract == null && ctx.Attacker != null)
            {
                var attackerContract = CombatWarningManager.GetActiveContract(ctx.Attacker);
                if (attackerContract != null && (attackerContract.ParryRole == ctx.Victim || attackerContract.ParryRole == null))
                {
                    contract = attackerContract;
                }
            }

            // 若无任何有效契约，直接放行不触发招架
            if (contract == null) return;

            if (contract.ParryRole == ctx.Victim)
            {
                contract.IsResolved = true;
            }

            victimParryData.Set(nameof(victimParryData.ParrySucceeded), true);
            victimParryData.Set(nameof(victimParryData.LastParriedAttacker), ctx.Attacker);
            if (ctx.Victim != null && ctx.Attacker != null)
            {
                ctx.Victim.TargetFinder?.SetCombatContextTarget(ctx.Attacker);
            }

            ParryWeight parryWeight = contract?.Marker != null ? contract.Marker.ParryWeight : ParryWeight.Heavy;
            victimParryData.Set(nameof(victimParryData.LastParryWeight), parryWeight);

            // 1. 构造本次捕获到的招架命中上下文
            var clashCtx = new ParryClashContext
            {
                Attacker = ctx.Attacker,
                Victim = ctx.Victim,
                Marker = contract?.Marker,
                HitPoint = ctx.HitPoint,
                HitDirection = ctx.HitDirection
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
