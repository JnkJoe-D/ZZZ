using UnityEngine;

namespace SkillEditor{
    /// <summary>
    /// 运行时：音频片段 Process
    /// 通过 ISkillAudioHandler 播放音频
    /// </summary>
    [ProcessBinding(typeof(SkillAudioClip), PlayMode.Runtime)]
    public class RuntimeAudioProcess : ProcessBase<SkillAudioClip>
    {
        private ISkillAudioHandler audioHandler;
        private int playingSoundId = -1;

        public override void OnEnable()
        {
            audioHandler = context.GetService<ISkillAudioHandler>();
        }

        public override void OnEnter()
        {
            if (audioHandler != null && clip.audioClips != null && clip.audioClips.Count > 0)
            {
                var validClips = clip.audioClips.FindAll(c => c != null);
                if (validClips.Count == 0) return;

                var selectedClip = validClips[Random.Range(0, validClips.Count)];
                if (selectedClip == null) return;
                var args = new AudioArgs
                {
                    volume = clip.volume,
                    pitch = !clip.isAffectSpeed? clip.pitch:clip.pitch * context.GlobalPlaySpeed, // 叠加全局变速
                    loop = clip.loop,
                    spatialBlend = clip.spatialBlend,
                    startTime = 0f, // 总是从头播放，除非实现了 Resume 逻辑
                    position = context.Owner != null ? context.Owner.transform.position : Vector3.zero
                };
                
                playingSoundId = audioHandler.PlaySound(selectedClip, args);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 如果需要支持动态参数（如时间轴内修改音量或音调曲线），可在此调用 UpdateSound
            // 目前仅更新各种变速后的 Pitch
            if (playingSoundId != -1 && audioHandler != null && clip.isAffectSpeed)
            {
                float targetPitch = clip.pitch * context.GlobalPlaySpeed;
                audioHandler.UpdateSound(playingSoundId, clip.volume, targetPitch, -1f); // -1 time 表示不强制同步时间
            }
        }

        public override void OnPause()
        {
            if (playingSoundId != -1 && audioHandler != null)
            {
                audioHandler.PauseSound(playingSoundId);
            }
        }

        public override void OnResume()
        {
            if (playingSoundId != -1 && audioHandler != null)
            {
                audioHandler.ResumeSound(playingSoundId);
            }
        }

        public override void OnExit()
        {
            // if (playingSoundId != -1 && audioHandler != null)
            // {
            //     audioHandler.StopSound(playingSoundId);
            //     playingSoundId = -1;
            // }
        }

        public override void Reset()
        {
            base.Reset();
            audioHandler = null;
            playingSoundId = -1;
        }
    }
}
