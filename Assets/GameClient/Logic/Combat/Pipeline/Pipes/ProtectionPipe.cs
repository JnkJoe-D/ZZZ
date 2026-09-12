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

        private readonly System.Collections.Generic.List<(IHitDefenseModifier Modifier, BuffInstance Buff)> _defenseBuffer = new(8);

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.Victim == null || ctx.Victim.IsDead)
            {
                ctx.Abort("Victim is null or already dead");
                return;
            }

            // 0. 后台退场实体保护（防止已隐形退场的实体被范围判定误伤）
            if (!ctx.Victim.IsPresentationVisible)
            {
                ctx.Abort("Victim is invisible in background", HitResultFlags.Protected);
                return;
            }

            // 1. 统一多态防御策略自解析 (0 if-else)：
            // 调度受击者身上所有生效的防御拦截器（按 Priority 降序执行：极限闪避 300 > 招架 200 > 纯无敌 50）
            if (ctx.Victim.StatusModule?.Buffs != null)
            {
                ctx.Victim.StatusModule.Buffs.GetActiveDefenseModifiers(_defenseBuffer);
                for (int i = 0; i < _defenseBuffer.Count; i++)
                {
                    var (modifier, buff) = _defenseBuffer[i];
                    if (modifier.TryInterceptHit(ctx, buff))
                    {
                        // 拦截生效，直接退出受击管线（已被 Abort 短路）
                        return;
                    }
                }
            }

            // 兼容性保底：旧标签免疫系统 (若外部仅调用了 AddImmuneTag("Invincible"))
            if (ctx.Victim.StatusModule != null && ctx.Victim.StatusModule.IsTagImmune("Invincible"))
            {
                var parryData = ctx.Victim.DataModule?.Get<ParryRuntimeData>();
                if (parryData == null || !parryData.IsParrying)
                {
                    ctx.Abort("Victim is Invincible (Tag)", HitResultFlags.Invincible);
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
