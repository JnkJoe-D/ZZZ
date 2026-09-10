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

            // 2. 动作切入与行为树通知
            if (ctx.SelectedReactionType != HitReactionType.None)
            {
                // 主角实体：通过状态机注入受击动作指令
                if (ctx.Victim is RoleEntity role)
                {
                    var hitAction = role.Config?.hitReactionConfig?.GetHitAction(ctx.SelectedReactionType);
                    if (hitAction != null && role.ActionController != null)
                    {
                        var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                        role.ActionController.OnInput(hitCommand);
                    }
                }
                // 怪物实体：通过黑板时间戳驱动行为树受击分支（MonsterHitTask）
                else if (ctx.Victim is MonsterEntity monster)
                {
                    var hitData = monster.DataModule?.Get<HitReactionRuntimeData>();
                    if (hitData != null)
                    {
                        hitData.HitTriggerTimestamp = Time.frameCount;
                        hitData.CurrentReactionType = ctx.SelectedReactionType;
                    }
                }

                // 兼容外部派生钩子
                ctx.Victim.HitReactionModule?.TriggerInterruptedHook(ctx);
            }
        }
    }
}
