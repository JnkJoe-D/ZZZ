using UnityEngine;
using cfg.ZZZ;
using ATEditor;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 招架阻断与弹刀反击过滤器 (基于 Luban 技能表数据驱动裁决)
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

            // 0. 查询针对该攻击者的拼刀契约（契约生命周期由 ParryWindowClip 统一控制）
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

            // 1. 状态标记与深度顿帧配置（双方必定获得顿帧，并触发招架火花与音效）
            ctx.ResultFlags |= HitResultFlags.Parried;
            ctx.EnableHitStop = true;
            ctx.HitStopDuration = ctx.RawHitData.hitStopDuration > 0 ? ctx.RawHitData.hitStopDuration : 0.12f;
            ctx.HitStopScale = 0f;

            // 2. 纯数据驱动打断裁决：防守方当前招架动作打断力 vs 攻击方（怪物）出招总韧性
            int parryInterruptLevel = ActionResilienceHelper.GetInterruptLevel(ctx.Victim);
            int monsterTotalResilience = ActionResilienceHelper.GetTotalResilience(ctx.Attacker);

            bool canInterruptMonster = parryInterruptLevel > 0 && parryInterruptLevel >= monsterTotalResilience;

            if (canInterruptMonster)
            {
                // 裁决成功（如普通轻攻击小怪）：攻击方出招被打断，进入弹刀受击踉跄动画
                ctx.SelectedReactionType = HitReactionType.Parried;
                ctx.ResultFlags |= HitResultFlags.Interrupted;
            }
            else
            {
                // 裁决失败（如霸体重攻击精英/Boss）：攻击方出招绝不被打断，顿帧后继续坚决完成挥砍
                ctx.SelectedReactionType = HitReactionType.None;
            }

            // 3. 双方视听表现派发（攻击者承受深度顿帧并播放招架火花特效与音效）
            if (ctx.Attacker != null)
            {
                var parryCtx = new HitContext
                {
                    attacker = ctx.Victim,
                    victim = ctx.Attacker,
                    IsParry = true,
                    interruptLevel = canInterruptMonster ? parryInterruptLevel : 0,
                    reactionType = canInterruptMonster ? HitReactionType.Parried : HitReactionType.None,
                    hitDirection = (ctx.Attacker.transform.position - ctx.Victim.transform.position).normalized,
                    enableHitStop = true,
                    hitStopDuration = ctx.HitStopDuration,
                    hitStopScale = 0f
                };
                parryCtx.reactionAxis = -parryCtx.hitDirection;
                ctx.Attacker.HitReactionModule?.ApplyVisualFeedback(parryCtx);
            }

            // 4. 触发防守方招架支援成功事件，顺畅切入招架反击
            ctx.Victim.ActionController?.TryTriggerEvent(RouteEventType.ParryAidSucceed);

            // 5. 阻断后续伤害与受击打断逻辑（短路）
            ctx.Abort("Parried by target", HitResultFlags.Parried);
        }
    }
}
