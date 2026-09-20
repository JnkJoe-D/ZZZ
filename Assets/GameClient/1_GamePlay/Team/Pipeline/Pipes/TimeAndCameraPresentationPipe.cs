using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
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
            // 相机策略处理
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
                            GLog.Info(LogTags.Team, $"连携技特写镜头激活: {ctx.IncomingMember?.Config?.Name}");
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
