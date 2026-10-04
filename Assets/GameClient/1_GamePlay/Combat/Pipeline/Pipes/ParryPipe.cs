using UnityEngine;
using cfg.ZZZ;
using ATEditor;
using Game.Framework;

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
            if (ctx.Victim == null || ctx.Attacker == null)
            {
                GLog.Warning(LogTags.Combat, "[ParryPipe] Victim 或 Attacker 为空，跳过招架判定");
                return;
            }

            var victimParryData = ctx.Victim.DataModule?.Get<ParryRuntimeData>();
            if (victimParryData == null || !victimParryData.IsParrying) return;

            // 0. 查询针对该攻击者的拼刀契约或攻击预警
            var contract = CombatWarningManager.GetActiveContractByRole(ctx.Victim);

            // 目标过滤守卫：若防守方已持有契约，但本次打过来的不是契约目标，放行给下游
            if (contract != null && ctx.Attacker != contract.Attacker)
            {
                GLog.Warning(LogTags.Combat, $"[ParryPipe] 攻击者 {ctx.Attacker.name} 与契约目标 {contract.Attacker?.name} 不匹配，放行受击");
                return;
            }

            if (contract == null && ctx.Attacker != null)
            {
                var attackerContract = CombatWarningManager.GetActiveContract(ctx.Attacker);
                if (attackerContract != null && (attackerContract.ParryRole == ctx.Victim || attackerContract.ParryRole == null))
                {
                    contract = attackerContract;
                }
            }

            AttackWarningMarker marker = contract?.Marker;
            if (marker == null)
            {
                var actionData = ctx.Victim.DataModule?.Get<ActionRuntimeData>();
                marker = actionData?.MatchedWarningMarker ?? CombatWarningManager.GetWarningByAttacker(ctx.Attacker);
            }

            // 若无任何有效预警或契约，放行不触发招架
            if (marker == null)
            {
                GLog.Warning(LogTags.Combat, $"[ParryPipe] 未找到攻击者 {ctx.Attacker.name} 的预警标记，放行受击");
                return;
            }

            if (marker.Attacker != ctx.Attacker)
            {
                GLog.Warning(LogTags.Combat, $"[ParryPipe] 预警攻击者与实际攻击者不匹配，放行受击");
                return;
            }

            if (contract != null && contract.ParryRole == ctx.Victim)
            {
                contract.IsResolved = true;
            }

            victimParryData.Set(nameof(victimParryData.ParrySucceeded), true);
            victimParryData.Set(nameof(victimParryData.LastParriedAttacker), ctx.Attacker);
            // 连续招架支持：不再强制关闭 IsParrying，招架态生命周期由时间轴 ParryWindowClip 自治管理
            ctx.Victim.TargetFinder?.SetCombatContextTarget(ctx.Attacker);

            ParryWeight parryWeight = marker.ParryWeight != 0 ? marker.ParryWeight : ParryWeight.Heavy;
            victimParryData.Set(nameof(victimParryData.LastParryWeight), parryWeight);

            var preData = contract?.PrecomputedData;
            if (contract != null && (preData == null || !preData.IsValid))
            {
                ParryPreArbitrator.Precompute(contract);
                preData = contract.PrecomputedData;
            }

            // 1. 构造本次捕获到的招架命中上下文
            Vector3 clashPos = (preData != null && preData.IsValid) ? preData.ClashPosition : ctx.HitPoint;
            var clashCtx = new ParryClashContext
            {
                Attacker = ctx.Attacker,
                Victim = ctx.Victim,
                Marker = marker,
                HitPoint = clashPos,
                HitDirection = ctx.HitDirection,
                PrecomputedData = preData
            };

            // 2. 通知招架成功事件给防守方（移交 ClashHandler 统筹双方顿帧与受击打断回调）
            if (victimParryData.ClashHandler != null)
            {
                victimParryData.ClashHandler.OnHitCaptured(clashCtx);
            }
            else
            {
                GLog.Warning(LogTags.Combat, $"[ParryPipe] {ctx.Victim.name} 的 ClashHandler 为空，未派发反制");
            }

            // 3. 核心：立即短路退出当前命中流水线，确保防守方绝对不被扣除生命值！
            ctx.Abort("Parried by target", HitResultFlags.Parried);
        }
    }
}
