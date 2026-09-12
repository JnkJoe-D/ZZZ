using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 受击空间朝向对齐与动作状态机打断切入过滤器
    /// </summary>
    public class MotionAndActionPipe : IHitPipe
    {
        public string PipeName => "MotionAndActionPipe";
        public int Priority => 500;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.Victim == null || ctx.Victim.IsDead) return;
            if (!ctx.ResultFlags.HasFlag(HitResultFlags.Interrupted)) return;

            // 1. 受击转向：面朝攻击袭来方向（-HitDirection）
            Vector3 faceDir = -ctx.HitDirection;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude <= 0.0001f && ctx.Attacker != null)
            {
                faceDir = ctx.Attacker.transform.position - ctx.Victim.transform.position;
                faceDir.y = 0f;
            }

            if (faceDir.sqrMagnitude > 0.0001f)
            {
                if (ctx.Victim.CharacterMotor != null)
                {
                    ctx.Victim.CharacterMotor.FaceToImmediately(faceDir.normalized);
                }
                else
                {
                    ctx.Victim.transform.forward = faceDir.normalized;
                }
            }

            // 2. 纯领域驱动的时间膨胀打醒（零 is 判断）：
            // 只要实体处于子弹时间中，且本次受击确实导致其动作被打断（Interrupted），方才打醒！
            if (ctx.ResultFlags.HasFlag(HitResultFlags.Interrupted))
            {
                var timeData = ctx.Victim.DataModule?.Get<TimeDilationRuntimeData>();
                if (timeData != null && timeData.IsInBulletTime)
                {
                    timeData.ExitBulletTime();
                    ctx.Victim.ActionPlayer?.RestorePlaySpeed();
                }
            }

            // 3. 若动作未被打断（霸体生效 SuperArmor），绝不切入受击动作
            if (!ctx.ResultFlags.HasFlag(HitResultFlags.Interrupted)) return;

            // 4. 受击动作切入与反馈：统一委托给多态契约 HitReactionModule，彻底消灭类型探测！
            if (ctx.SelectedReactionType != HitReactionType.None)
            {
                var hitData = ctx.Victim.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.CurrentReactionType = ctx.SelectedReactionType;
                }

                ctx.Victim.HitReactionModule?.TriggerInterruptedHook(ctx);
            }
        }
    }
}
