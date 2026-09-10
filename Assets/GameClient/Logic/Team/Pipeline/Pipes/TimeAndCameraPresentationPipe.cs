using UnityEngine;
using Game.Camera;

namespace Game.Logic.Team.Pipeline.Pipes
{
    /// <summary>
    /// 时钟调度（顿帧/子弹时间）与相机视听机位过滤器
    /// </summary>
    public class TimeAndCameraPresentationPipe : ISwitchPipe
    {
        public string PipeName => "TimeAndCameraPresentationPipe";
        public int Priority => 200;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.IsAborted) return;

            // 1. 时钟调度：招架顿帧与全局缓速
            if (ctx.TimeScaleDuration > 0f && TimeManager.Instance != null)
            {
                if (ctx.Type == SwitchType.ParryAid)
                {
                    TimeManager.Instance.RegisterHitStop(
                        ctx.OutgoingEntity?.ActionPlayer,
                        ctx.IncomingEntity?.ActionPlayer,
                        ctx.TimeScaleDuration,
                        ctx.TimeScale);
                }
                else if (ctx.Type == SwitchType.EvasionAid || ctx.Type == SwitchType.ChainAttack)
                {
                    // 闪避支援 / 连携技：局部顿帧与慢动作
                    ctx.OutgoingEntity?.ActionPlayer?.SetPlaySpeed(ctx.TimeScale);
                    ctx.IncomingEntity?.ActionPlayer?.SetPlaySpeed(1.0f); // 切入角色保持全速飒爽突入
                }
            }

            // 2. 相机策略处理
            if (ctx.IncomingEntity != null)
            {
                var cameraManager = GameCameraManager.Instance;
                if (cameraManager != null)
                {
                    switch (ctx.CamMode)
                    {
                        case CameraSwitchMode.InstantSnap:
                            cameraManager.SetTarget(ctx.IncomingEntity.transform);
                            break;

                        case CameraSwitchMode.CinematicQTE:
                            cameraManager.SetTarget(ctx.IncomingEntity.transform);
                            Debug.Log($"<color=cyan>[SwitchPipeline] 连携技特写镜头激活: {ctx.IncomingMember?.Config?.Name}</color>");
                            break;

                        case CameraSwitchMode.SmoothFollow:
                        default:
                            cameraManager.SetTarget(ctx.IncomingEntity.transform);
                            break;
                    }
                }
            }
        }
    }
}
