using System.Collections;
using UnityEngine;

namespace Game.Logic.Team.Pipeline.Pipes
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

            // 1. 触发切入动作
            if (ctx.CustomIncomingAction != null)
            {
                inEntity.ActionController?.PlayAction(ctx.CustomIncomingAction);
                Debug.Log($"<color=green>[SwitchPipeline] 播放自定义切入动作: {ctx.CustomIncomingAction.name}</color>");
            }
            else
            {
                switch (ctx.Type)
                {
                    case SwitchType.NormalSwitch:
                        inEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchIn);
                        break;

                    case SwitchType.ParryAid:
                        inEntity.ActionController?.TryTriggerEvent(RouteEventType.ParryAidStart);
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
                Debug.Log($"<color=green>[SwitchPipeline] 触发切入事件: {ctx.Type}</color>");
            }

            // 2. 切入保护：赋予短暂起手无敌帧（防止刚切入同帧受击暴毙）
            if (ctx.InvincibleDuration > 0f && inEntity.StatusModule != null)
            {
                inEntity.StatusModule.AddImmuneTag("Invincible");
                inEntity.StartCoroutine(RemoveInvincibleRoutine(inEntity, ctx.InvincibleDuration));
            }
        }

        private IEnumerator RemoveInvincibleRoutine(RoleEntity entity, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (entity != null && entity.StatusModule != null)
            {
                entity.StatusModule.RemoveImmuneTag("Invincible");
            }
        }
    }
}
