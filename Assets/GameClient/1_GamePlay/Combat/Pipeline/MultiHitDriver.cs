using System.Collections;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 攻击端多段打击驱动器（生命周期脱离受击者，由攻击端或全局时钟驱动）
    /// </summary>
    public static class MultiHitDriver
    {
        /// <summary>
        /// 启动多段打击协程
        /// </summary>
        public static void RunMultiHit(
            MonoBehaviour runner,
            HitPipeline pipeline,
            HitPipelineContext ctx,
            float duration,
            int totalHits)
        {
            if (runner == null || !runner.gameObject.activeInHierarchy)
            {
                // 若无宿主则直接同步打单发并回收
                pipeline.Execute(ctx);
                pipeline.ReleaseContext(ctx);
                return;
            }

            runner.StartCoroutine(ExecuteMultiHitRoutine(pipeline, ctx, duration, totalHits));
        }

        private static IEnumerator ExecuteMultiHitRoutine(
            HitPipeline pipeline,
            HitPipelineContext ctx,
            float duration,
            int totalHits)
        {
            try
            {
                if (totalHits <= 1 || duration <= 0f)
                {
                    pipeline.Execute(ctx);
                    yield break;
                }

                float interval = duration / totalHits;
                for (int i = 0; i < totalHits; i++)
                {
                    // 若攻击者已被销毁或死亡，多段打击立即熔断终止
                    if (ctx.Attacker == null || ctx.Attacker.LifecycleComponent == null || ctx.Attacker.LifecycleComponent.IsDead)
                    {
                        yield break;
                    }

                    // 只要受击者还存在引用（即使已死亡），依然可执行鞭尸表现或打击特效
                    if (ctx.Victim == null)
                    {
                        yield break;
                    }

                    ctx.ResetPerHitState();
                    ctx.CurrentHitIndex = i;
                    ctx.TotalHitCount = totalHits;

                    pipeline.Execute(ctx);

                    if (i < totalHits - 1)
                    {
                        float elapsed = 0f;
                        while (elapsed < interval)
                        {
                            yield return null;
                            if (ctx.Attacker == null || ctx.Attacker.LifecycleComponent == null || ctx.Attacker.LifecycleComponent.IsDead)
                            {
                                yield break;
                            }

                            float dt = ctx.Attacker.Clock != null
                                ? ctx.Attacker.Clock.DeltaTime
                                : Time.deltaTime;
                            elapsed += dt;
                        }
                    }
                }
            }
            finally
            {
                pipeline.ReleaseContext(ctx);
            }
        }
    }
}
