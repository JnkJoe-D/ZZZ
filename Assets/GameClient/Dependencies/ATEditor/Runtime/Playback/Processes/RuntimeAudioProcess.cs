using UnityEngine;

namespace ATEditor{
    /// <summary>
    /// 运行时：音频片段 Process
    /// 通过 ISkillAudioHandler 播放音频
    /// </summary>
    [ProcessBinding(typeof(AudioClip), PlayMode.Runtime)]
    public class RuntimeAudioProcess : ProcessBase<AudioClip>
    {
        private IAudioHandler audioHandler;
        private int playingSoundId = -1;
        private float PlaybackPitch => clip.pitch * (clip.isAffectSpeed ? context.PresentationPlaySpeed : 1f);

        public override void OnEnable()
        {
            audioHandler = context.GetService<IAudioHandler>();
        }

        public override void OnEnter()
        {
            if (audioHandler != null && clip.audioClips != null && clip.audioClips.Count > 0)
            {
                // 允许包含空片段（起到概率不播放的效果）
                var selectedClip = clip.audioClips[Random.Range(0, clip.audioClips.Count)];
                if (selectedClip == null) return;
                var args = new AudioArgs
                {
                    volume = clip.volume,
                    pitch = PlaybackPitch,
                    loop = clip.loop,
                    spatialBlend = clip.spatialBlend,
                    startTime = Mathf.Max(0f, context.CurrentTime - clip.StartTime) * clip.pitch,
                    position = context.Owner != null ? context.Owner.transform.position : Vector3.zero,
                    parent = context.OwnerTransform,
                    affectedByPlaybackSpeed = clip.isAffectSpeed
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
                float targetPitch = PlaybackPitch;
                audioHandler.UpdateSound(playingSoundId, clip.volume, targetPitch, -1f); // -1 time 表示不强制同步时间
            }
        }

        public override void OnPause()
        {
            if (playingSoundId != -1 && audioHandler != null && clip != null && clip.lifeControlByClip)
            {
                audioHandler.PauseSound(playingSoundId);
            }
        }

        public override void OnSeek(float targetTime)
        {
            if (playingSoundId == -1 || audioHandler == null) return;
            float pitch = PlaybackPitch;
            audioHandler.UpdateSound(playingSoundId, clip.volume, pitch,
                Mathf.Max(0f, targetTime - clip.StartTime) * clip.pitch);
        }

        public override void OnResume()
        {
            if (playingSoundId != -1 && audioHandler != null && clip != null && clip.lifeControlByClip)
            {
                audioHandler.ResumeSound(playingSoundId);
            }
        }

        public override void OnExit()
        {
            if (playingSoundId != -1 && audioHandler != null && clip != null && clip.lifeControlByClip)
            {
                audioHandler.StopSound(playingSoundId);
                playingSoundId = -1;
            }
        }

        public override void OnStop()
        {
            if (playingSoundId != -1 && audioHandler != null && clip != null && clip.lifeControlByClip)
            {
                audioHandler.StopSound(playingSoundId);
                playingSoundId = -1;
            }
        }

        public override void Reset()
        {
            if (playingSoundId != -1 && audioHandler != null && clip != null && clip.lifeControlByClip)
            {
                audioHandler.StopSound(playingSoundId);
            }
            base.Reset();
            audioHandler = null;
            playingSoundId = -1;
        }
    }
}
