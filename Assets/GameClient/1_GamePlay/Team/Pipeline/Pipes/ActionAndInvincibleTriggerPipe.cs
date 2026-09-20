using Game.Framework;
using System.Collections;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 切入专属动作派发与起手无敌保护过滤器
    /// </summary>
    public class ActionAndInvincibleTriggerPipe : ISwitchPipe
    {
        public string PipeName => "ActionAndInvincibleTriggerPipe";
        public int Priority => 600;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.IncomingEntity == null) return;

            RoleEntity inEntity = ctx.IncomingEntity;

            // 0. 确保战斗上下文目标注入
            if (ctx.TargetAttacker != null)
            {
                inEntity.TargetFinder?.SetCombatContextTarget(ctx.TargetAttacker);
            }

            // 1. 触发切入动作触发源路由事件
            switch (ctx.Type)
            {
                case SwitchType.NormalSwitch:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchIn);
                    break;

                case SwitchType.ParryAid:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.ParryAidStart);
                    break;

                case SwitchType.FallbackEvasion:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.FallbackEvasionIn);
                    break;

                case SwitchType.EvasionAid:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.EvasionAidStart);
                    break;

                case SwitchType.ChainAttack:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.ChainAttack);
                    break;

                case SwitchType.QuickAid:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.QuickAid);
                    break;
            }
            GLog.Info(LogTags.Team, $"触发切入事件: {ctx.Type}");

        }
    }
}
