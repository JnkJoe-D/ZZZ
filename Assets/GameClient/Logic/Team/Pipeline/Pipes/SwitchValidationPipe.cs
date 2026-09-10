using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.Team.Pipeline.Pipes
{
    /// <summary>
    /// 换人准入校验与切入目标解析过滤器
    /// </summary>
    public class SwitchValidationPipe : ISwitchPipe
    {
        public string PipeName => "SwitchValidationPipe";
        public int Priority => 100;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.Manager == null)
            {
                ctx.Abort("TeamManager is null");
                return;
            }

            if (ctx.OutgoingMember == null || ctx.OutgoingEntity == null)
            {
                ctx.Abort("OutgoingMember or OutgoingEntity is null");
                return;
            }

            // 1. 若外部未显式指定 IncomingMember，根据 TargetSlotHint 或轮转规则推导
            if (ctx.IncomingMember == null)
            {
                ctx.IncomingMember = ResolveIncoming(ctx.Manager, ctx.OutgoingMember, ctx.TargetSlotHint);
            }

            if (ctx.IncomingMember == null || ctx.IncomingMember == ctx.OutgoingMember)
            {
                ctx.Abort("No valid incoming member found in party");
                return;
            }

            if (ctx.IncomingEntity == null)
            {
                ctx.Abort("IncomingEntity is null");
                return;
            }

            if (ctx.IncomingEntity.IsDead)
            {
                ctx.Abort("IncomingEntity is already dead");
                return;
            }

            // 2. 根据切人类型初始化默认策略与时空参数（若外部未特化指定）
            ConfigureDefaults(ctx);
        }

        private void ConfigureDefaults(SwitchPipelineContext ctx)
        {
            switch (ctx.Type)
            {
                case SwitchType.NormalSwitch:
                    ctx.ExitPolicy = OutgoingExitPolicy.AutoFollowThrough;
                    ctx.CamMode = CameraSwitchMode.SmoothFollow;
                    ctx.InvincibleDuration = 0.3f;
                    break;

                case SwitchType.ParryAid:
                    ctx.ExitPolicy = OutgoingExitPolicy.Immediate;
                    ctx.CamMode = CameraSwitchMode.InstantSnap;
                    ctx.TimeScale = 0.05f;
                    ctx.TimeScaleDuration = 0.12f;
                    ctx.InvincibleDuration = 0.8f;
                    break;

                case SwitchType.EvasionAid:
                    ctx.ExitPolicy = OutgoingExitPolicy.Immediate;
                    ctx.CamMode = CameraSwitchMode.InstantSnap;
                    ctx.TimeScale = 0.1f;
                    ctx.TimeScaleDuration = 1.0f; // 全局子弹时间 1 秒
                    ctx.InvincibleDuration = 1.2f;
                    break;

                case SwitchType.ChainAttack:
                    ctx.ExitPolicy = OutgoingExitPolicy.AutoFollowThrough;
                    ctx.CamMode = CameraSwitchMode.CinematicQTE;
                    ctx.TimeScale = 0.0f;
                    ctx.TimeScaleDuration = 0.3f;
                    ctx.InvincibleDuration = 2.0f;
                    break;

                case SwitchType.QuickAid:
                    ctx.ExitPolicy = OutgoingExitPolicy.AutoFollowThrough;
                    ctx.CamMode = CameraSwitchMode.InstantSnap;
                    ctx.TimeScale = 0.2f;
                    ctx.TimeScaleDuration = 0.4f;
                    ctx.InvincibleDuration = 1.0f;
                    break;
            }
        }

        private PartyMember ResolveIncoming(TeamManager manager, PartyMember outgoing, int slotHint)
        {
            IReadOnlyList<PartyMember> members = manager.PartyMembers;
            if (members == null || members.Count <= 1) return null;

            // 优先使用提示插槽
            if (slotHint >= 0 && slotHint < members.Count)
            {
                PartyMember hinted = members[slotHint];
                if (hinted != outgoing && hinted.Entity != null)
                    return hinted;
            }

            // 默认轮转：下一个插槽
            int startIndex = (outgoing.SlotIndex + 1) % members.Count;
            for (int attempt = 0; attempt < members.Count; attempt++)
            {
                int index = (startIndex + attempt) % members.Count;
                PartyMember candidate = members[index];
                if (candidate != outgoing && candidate.Entity != null)
                    return candidate;
            }

            return null;
        }
    }
}
