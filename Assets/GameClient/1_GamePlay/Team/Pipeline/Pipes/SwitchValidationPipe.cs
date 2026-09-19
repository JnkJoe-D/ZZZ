using Game.Framework;
using System.Collections.Generic;
using Game.GamePlay;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 换人准入校验与切入目标解析过滤器
    /// </summary>
    public class SwitchValidationPipe : ISwitchPipe
    {
        public string PipeName => "SwitchValidationPipe";
        public int Priority => 100;

        /// <summary>
        /// 换人请求最小防抖冷却间隔（秒），防止物理按键抖动或极端微秒级连跳切人。
        /// </summary>
        public const float SwitchDebounceCooldown = 0.2f;

        private float _lastSwitchTime = -1f;

        public void ResetCooldown() => _lastSwitchTime = -1f;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.Manager == null)
            {
                ctx.Abort("TeamManager is null");
                return;
            }

            // 0. 换人防抖冷却校验
            if (_lastSwitchTime >= 0f && Time.time - _lastSwitchTime < SwitchDebounceCooldown)
            {
                ctx.Abort($"切人请求过于频繁，处于防抖冷却中 (冷却间隔: {SwitchDebounceCooldown}s)");
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

            if (ctx.IncomingEntity.LifecycleComponent.IsDead)
            {
                ctx.Abort("IncomingEntity is already dead");
                return;
            }

            // 1.5 招架合法性校验（底层安全防线：若前端请求了 ParryAid 但当前预警实际已为红光不可招架或失效）
            if (ctx.Type == SwitchType.ParryAid)
            {
                bool isParryInvalid = ctx.WarningMarker == null || ctx.WarningMarker.SignalType == ATEditor.WarningSignalType.Red_Unparryable;
                if (isParryInvalid)
                {
                    // 检查切出角色是否真正处于怪物的挥刀威胁覆盖域内
                    bool inDanger = false;
                    if (ctx.WarningMarker != null && ctx.OutgoingEntity != null)
                    {
                        if (ctx.WarningMarker.CoverageShape != null)
                        {
                            inDanger = ctx.WarningMarker.IsPositionInCoverage(ctx.OutgoingEntity.transform.position);
                        }
                        else if (ctx.WarningMarker.Attacker != null)
                        {
                            float fallbackDist = ctx.WarningMarker.DetectionRadius > 0 ? ctx.WarningMarker.DetectionRadius * 0.5f : CombatWarningManager.DefaultDetectionRadius * 0.5f;
                            Vector3 toActor = ctx.OutgoingEntity.transform.position - ctx.WarningMarker.Attacker.transform.position;
                            toActor.y = 0;
                            inDanger = toActor.sqrMagnitude <= fallbackDist * fallbackDist;
                        }
                    }

                    if (inDanger)
                    {
                        GLog.Info(LogTags.Team, $"当前预警不可招架且处于危险区，切人类型由 ParryAid 降级为 FallbackEvasion (避险切人)");
                        ctx.Type = SwitchType.FallbackEvasion;
                    }
                    else
                    {
                        GLog.Info(LogTags.Team, $"当前预警不可招架但处于安全区，切人类型由 ParryAid 降级为 NormalSwitch (普通切人)");
                        ctx.Type = SwitchType.NormalSwitch;
                    }
                }
            }

            // 2. 校验完全通过，记录本次切换成功时间戳
            _lastSwitchTime = Time.time;

            // 3. 根据切人类型初始化默认策略与时空参数（若外部未特化指定）
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
                    ctx.InvincibleDuration = 0f;
                    break;

                case SwitchType.FallbackEvasion:
                    ctx.ExitPolicy = OutgoingExitPolicy.Immediate;
                    ctx.CamMode = CameraSwitchMode.InstantSnap;
                    ctx.TimeScale = 1.0f;
                    ctx.TimeScaleDuration = 0f;
                    ctx.InvincibleDuration = 0.4f;
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
