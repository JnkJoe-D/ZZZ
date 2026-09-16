using UnityEngine;
using Game.Framework;
 

namespace Game.GamePlay
{
    /// <summary>
    /// 视听打击感表现过滤器（顿帧、特效、音效、相机微震）
    /// </summary>
    public class FeedbackPresentationPipe : IHitPipe
    {
        public string PipeName => "FeedbackPresentationPipe";
        public int Priority => 600;

        public void Process(HitPipelineContext ctx)
        {
            // 如果非招架且被短路中断，不产生受击视听
            if (ctx.IsAborted && !ctx.ResultFlags.HasFlag(HitResultFlags.Parried))
                return;

            if (ctx.Victim == null) return;

            // 1. 受击顿帧（通过全局主时钟调度）
            if (ctx.EnableHitStop)
            {
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.RegisterHitStop(
                        ctx.Attacker?.ActionPlayer,
                        ctx.Victim?.ActionPlayer,
                        ctx.HitStopDuration,
                        ctx.HitStopScale);
                    ctx.ResultFlags |= HitResultFlags.HitStopApplied;
                }
                else
                {
                    ctx.Attacker?.ActionPlayer?.SetPlaySpeed(ctx.HitStopScale);
                    ctx.Victim?.ActionPlayer?.SetPlaySpeed(ctx.HitStopScale);
                }
            }

            // 2. 打击火花特效（面向主相机平面广告牌对齐）
            if (ctx.HitVFXPrefab != null && VFXManager.Instance != null)
            {
                Vector3 spawnPos = ctx.HitPoint;
                spawnPos.y = ctx.Victim.transform.position.y + ctx.HitVFXHeight;

                Quaternion spawnRot = Quaternion.identity;
                var mainCam = GameCameraManager.Instance?.MainCamera ?? UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    spawnRot = Quaternion.LookRotation(-mainCam.transform.forward);
                }

                Transform parent = ctx.HitVFXFollowTarget ? ctx.Victim.transform : null;
                var vfx = VFXManager.Instance.Spawn(ctx.HitVFXPrefab, spawnPos, spawnRot, parent);
                if (vfx != null)
                {
                    vfx.transform.localScale = ctx.HitVFXScale;
                    VFXManager.Instance.ReturnWhenDone(vfx);
                }
            }

            // 3. 空间打击音效
            if (ctx.HitAudioClip != null && AudioManager.Instance != null)
            {
                Vector3 soundPos = ctx.HitPoint;
                soundPos.y = ctx.Victim.transform.position.y + ctx.HitVFXHeight;

                var args = new AudioArgs
                {
                    position = soundPos,
                    spatialBlend = 1f,
                    volume = 1f,
                    pitch = 1f
                };

                AudioManager.Instance.PlayAudio(
                    ctx.HitAudioClip,
                    AudioChannel.SFX,
                    args);
            }
        }
    }
}
