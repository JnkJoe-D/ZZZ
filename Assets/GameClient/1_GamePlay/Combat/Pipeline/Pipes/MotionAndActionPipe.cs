using Game.Framework;
using UnityEngine;
using cfg.ZZZ;

namespace Game.GamePlay
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
            if (ctx.IsAborted || ctx.Victim == null || ctx.Victim.LifecycleComponent.IsDead) return;
            if (!ctx.ResultFlags.HasFlag(HitResultFlags.Interrupted)) return;

            // 1. 攻击袭来方向向量（面向受击方向上下文，水平投射）
            Vector3 faceDir = -ctx.HitDirection;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude <= 0.0001f && ctx.Attacker != null)
            {
                faceDir = ctx.Attacker.transform.position - ctx.Victim.transform.position;
                faceDir.y = 0f;
            }

            // 2. 空间受击角度计算（以受击者局部朝向为基准）
            float signedHorizontalAngle = 0f;
            if (faceDir.sqrMagnitude > 0.0001f)
            {
                signedHorizontalAngle = Vector3.SignedAngle(ctx.Victim.transform.forward, faceDir.normalized, Vector3.up);
            }

            float verticalAngle = 0f;
            if (ctx.HitDirection.sqrMagnitude > 0.0001f)
            {
                verticalAngle = Vector3.Angle(Vector3.up, -ctx.HitDirection) - 90f;
            }

            // 3. 受击动作多向细分与自适应转向决策
            ActionConfigAsset resolvedAction = null;
            bool needFaceAttacker = false;

            var hitReactionConfig = ctx.Victim.Config?.hitReactionConfig;
            if (hitReactionConfig != null && ctx.SelectedReactionType != HitReactionType.None)
            {
                hitReactionConfig.TryResolveHitAction(
                    ctx.SelectedReactionType,
                    signedHorizontalAngle,
                    verticalAngle,
                    out resolvedAction,
                    out needFaceAttacker);
            }

            ctx.ResolvedHitAction = resolvedAction;
            ctx.RequireFaceAttacker = needFaceAttacker;

            // 4. 受控物理转向：仅在裁决明确需要转向时，精准面向受击方向上下文（绝不硬编码 180°）
            if (needFaceAttacker && faceDir.sqrMagnitude > 0.0001f)
            {
                if (ctx.Victim.MovementComponent != null)
                {
                    ctx.Victim.MovementComponent.FaceToImmediately(faceDir.normalized);
                }
                else
                {
                    ctx.Victim.transform.forward = faceDir.normalized;
                }
            }

            // 5. 状态数据同步、领域事件发布与打断钩子切入（仅在被打断裁决通过且确实存在受击表现时触发）
            if (ctx.SelectedReactionType != HitReactionType.None)
            {
                // 受击打断事实发布 (Domain Event)：通知事件总线实体产生打断，由时间管理器等系统内部内聚自闭合响应（如打醒并解除子弹时间）
                EventCenter.Publish(new EntityHitInterruptedEvent(ctx.Attacker, ctx.Victim, ctx.SelectedReactionType));

                var hitData = ctx.Victim.DataModule?.Get<HitReactionRuntimeData>();
                if (hitData != null)
                {
                    hitData.Set(nameof(hitData.CurrentReactionType), ctx.SelectedReactionType);
                    hitData.Set(nameof(hitData.ResolvedHitAction), resolvedAction);
                    hitData.Set(nameof(hitData.RequireFaceAttacker), needFaceAttacker);
                }

                GLog.Info(LogTags.Combat, $"受击表现: {ctx.Victim.name} | 类型: {ctx.SelectedReactionType} | 动作: {resolvedAction?.name ?? "None"} | 转向: {needFaceAttacker} (角:{signedHorizontalAngle:F1}°) | 攻击来源: {ctx.Attacker?.name}");

                ctx.Victim.HitReactionComponent?.TriggerInterruptedHook(ctx);
            }
        }
    }
}
