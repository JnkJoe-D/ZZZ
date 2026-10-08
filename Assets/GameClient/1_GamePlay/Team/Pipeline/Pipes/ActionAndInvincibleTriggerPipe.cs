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

            // 0. 处于 Pending 待切出状态切回且非招架支援：角色保持正在执行的动作，不重新播放切入动作
            if (ctx.IsIncomingPendingSwitchOut && ctx.Type != SwitchType.ParryAid)
            {
                GLog.Info(LogTags.Team, $"切入角色 {inEntity.name} 处于 Pending 状态切回，保持当前动作，跳过触发 SwitchIn");
                return;
            }

            // 1. 确保战斗上下文目标注入与预警标记同步
            if (ctx.TargetAttacker != null)
            {
                inEntity.TargetFinder?.SetCombatContextTarget(ctx.TargetAttacker);
            }

            if (ctx.WarningMarker != null && inEntity.DataModule != null)
            {
                var actionData = inEntity.DataModule.Get<ActionRuntimeData>();
                actionData?.Set(nameof(actionData.MatchedWarningMarker), ctx.WarningMarker);

                var parryData = inEntity.DataModule.Get<ParryRuntimeData>();
                var weight = ctx.WarningMarker.ParryWeight != 0 ? ctx.WarningMarker.ParryWeight : ATEditor.ParryWeight.Light;
                parryData?.Set(nameof(parryData.LastParryWeight), weight);
            }

            // 1. 触发切入动作触发源路由事件
            switch (ctx.Type)
            {
                case SwitchType.NormalSwitch:
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchIn);
                    break;

                case SwitchType.ParryAid:
                    var parryData = inEntity.DataModule?.Get<ParryRuntimeData>();
                    if (parryData != null && ctx.WarningMarker != null)
                    {
                        var weight = ctx.WarningMarker.ParryWeight != 0 ? ctx.WarningMarker.ParryWeight : ATEditor.ParryWeight.Light;
                        parryData.Set(nameof(parryData.LastParryWeight), weight);

                        // 临界压哨接刀保护：若快进播放（说明攻击即将或已经到达），提前无缝激活 IsParrying，抹平时间轴与判定帧之间微秒级空窗期
                        if (ctx.CalculatedStartTime > 0f)
                        {
                            parryData.Set(nameof(parryData.IsParrying), true);
                        }
                    }

                    // 预先建立拼刀契约，确保受击管线在极早期或压哨命中时能立刻锁定招架目标
                    if (ctx.WarningMarker != null && ctx.TargetAttacker != null)
                    {
                        var contract = new ParryClashContract
                        {
                            Attacker = ctx.TargetAttacker,
                            ParryRole = inEntity,
                            Marker = ctx.WarningMarker,
                            IsResolved = false
                        };
                        CombatWarningManager.RegisterContract(contract);
                    }

                    // 纯数据裁决：统一触发招架起手架势 (ParryAidStart)，并携带动态推导的动作快进偏移
                    float startOffset = ctx.CalculatedStartTime;
                    inEntity.ActionController?.TryTriggerEvent(RouteEventType.ParryAidStart, startOffset);
                    GLog.Info(LogTags.Team, $"[Parry] 派发招架起手路由事件: ParryAidStart, StartTime={startOffset:F3}s");
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
