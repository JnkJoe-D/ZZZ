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

            // 0. 查询针对该攻击者的拼刀契约（契约生命周期由 ParryWindowClip 统一控制，此处不注销以支持多段判定）
            var contract = ctx.Attacker != null ? CombatWarningManager.GetActiveContract(ctx.Attacker) : null;
            if (contract != null && contract.ParryRole == ctx.Victim)
            {
                contract.IsResolved = true;
            }

            // 1. 获取攻击者危险警示重量（优先从契约或全局查找，杜绝静默回退）
            var warningMarker = contract?.Marker ?? CombatWarningManager.GetWarningByAttacker(ctx.Attacker);
            if (warningMarker == null)
            {
                Debug.LogError($"[ParryPipe] 招架异常：攻击者 [{ctx.Attacker?.name}] 在招架结算时未找到有效的预警上下文 (WarningMarker)，无法判定攻击重量！绝不静默假定为轻攻击。");
                ctx.Abort("Parried but warning context missing", HitResultFlags.Parried);
                return;
            }

            var weight = warningMarker.Weight;

            victimParryData.ParrySucceeded = true;
            victimParryData.LastParriedAttacker = ctx.Attacker;
            victimParryData.LastParriedWeight = weight;
            if (ctx.Victim is RoleEntity roleVictim && ctx.Attacker != null)
            {
                roleVictim.SetCombatContextTarget(ctx.Attacker);
            }

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
