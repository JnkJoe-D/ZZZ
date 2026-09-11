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

            // 0. 后台退场角色保护（防止已隐形退场的角色被范围判定误伤）
            if (ctx.Victim is RoleEntity roleVictim && !roleVictim.IsPresentationVisible)
            {
                ctx.Abort("Victim is a retired background role", HitResultFlags.Protected);
                return;
            }

            // 1. 无敌状态检测（StatusModule 标签免疫）
            if (ctx.Victim.StatusModule != null && ctx.Victim.StatusModule.IsTagImmune("Invincible"))
            {
                // 若受击者当前处于招架窗口中，放行给 ParryPipe 判定，不因被动无敌吞噬主动招架
                var parryData = ctx.Victim.DataModule?.Get<ParryRuntimeData>();
                if (parryData == null || !parryData.IsParrying)
                {
                    ctx.Abort("Victim is Invincible", HitResultFlags.Invincible);
                    return;
                }
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
