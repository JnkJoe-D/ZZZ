using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 受击保护与无敌状态拦截过滤器
    /// </summary>
    public class ProtectionPipe : IHitPipe
    {
        public string PipeName => "ProtectionPipe";
        public int Priority => 200;

        private readonly System.Collections.Generic.List<(IHitDefenseModifier Modifier, BuffInstance Buff)> _defenseBuffer = new(8);

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.Victim == null || ctx.Victim.LifecycleComponent.IsDead)
            {
                ctx.Abort("Victim is null or already dead");
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
        }
    }
}
