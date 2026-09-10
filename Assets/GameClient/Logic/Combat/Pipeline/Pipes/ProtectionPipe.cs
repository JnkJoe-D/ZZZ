using UnityEngine;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 受击保护与无敌状态拦截过滤器
    /// </summary>
    public class ProtectionPipe : IHitPipe
    {
        public string PipeName => "ProtectionPipe";
        public int Priority => 100;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.Victim == null || ctx.Victim.IsDead)
            {
                ctx.Abort("Victim is null or already dead");
                return;
            }

            // 1. 无敌状态检测（StatusModule 标签免疫）
            if (ctx.Victim.StatusModule != null && ctx.Victim.StatusModule.IsTagImmune("Invincible"))
            {
                ctx.Abort("Victim is Invincible", HitResultFlags.Invincible);
                return;
            }

            // 2. 受击保护内置 CD（防止同帧/短时间内连续被高频物理重复命中）
            float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;
            if (ctx.Victim.HitReactionModule != null)
            {
                if (!ctx.Victim.HitReactionModule.ValidateAndRecordHit(currentTime))
                {
                    ctx.Abort("Hit in Protection Interval", HitResultFlags.Protected);
                    return;
                }
            }
        }
    }
}
