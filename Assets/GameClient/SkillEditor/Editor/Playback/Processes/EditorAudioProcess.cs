using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器预览：音频片段 Process
    /// 通过 EditorAudioManager 获取/归还 AudioSource
    /// </summary>
    [ProcessBinding(typeof(SkillAudioClip), PlayMode.EditorPreview)]
    public class EditorAudioProcess : ProcessBase<SkillAudioClip>
    {
        private UnityEngine.AudioSource audioSource;
        private bool isScrubbing = false;
        private AudioClip _playingClip;

        public override void OnEnter()
        {
            audioSource = EditorAudioManager.Instance.Get();
            if (audioSource != null && clip.audioClips != null && clip.audioClips.Count > 0)
            {
                var validClips = clip.audioClips.FindAll(c => c != null);
                if (validClips.Count == 0) return;

                _playingClip = validClips[Random.Range(0, validClips.Count)];
                if (_playingClip == null) return;

                audioSource.clip = _playingClip;
                audioSource.volume = clip.volume;
                audioSource.pitch = clip.pitch * context.GlobalPlaySpeed; // 初始 Pitch
                audioSource.loop = clip.loop;
                audioSource.spatialBlend = clip.spatialBlend; // 支持 2D/3D 预览
                
                // 如果是 3D 音效，设置位置到预览角色位置
                if (clip.spatialBlend > 0.01f && context.Owner != null)
                {
                    audioSource.transform.position = context.Owner.transform.position;
                }

                audioSource.Play();
                // 如果当前时间已经过了一部分（例如 Seek 进入），需要同步时间
                // 注意：OnEnter 通常在 clip.startTime 触发，但如果是 Seek 导致的 Enter，需要计算 offset
                // ProcessBase 没有直接提供 "CurrentClipTime" 在 OnEnter，但可以通过 context 判断？
                // 简化起见，OnEnter 默认从头播放。如果需要精确 Seek，依靠 OnUpdate 的同步逻辑。
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (audioSource == null || _playingClip == null) return;

            // 1. 同步 Pitch (支持变速预览)
            float targetPitch = clip.pitch * context.GlobalPlaySpeed;
            if (Mathf.Abs(audioSource.pitch - targetPitch) > 0.01f)
            {
                audioSource.pitch = targetPitch;
            }

            // 2. 处理 Timeline Scrubbing (拖拽时间轴)
            // 计算当前片段内的理论播放时间
            float clipLocalTime = currentTime - clip.StartTime;
            float clipLength = _playingClip.length;

            if (clipLength <= 0.001f) return;
            
            // 循环模式下的时间映射 (使用 Repeat 处理负数和循环)
            if (clip.loop)
            {
                clipLocalTime = Mathf.Repeat(clipLocalTime, clipLength);
            }
            else
            {
                // 非循环模式：超出范围则不强行 Seek，或者停止
                // 允许微小的误差，但不能设置超出 length 的 time
                if (clipLocalTime >= clipLength)
                {
                    // 播放结束
                    if (audioSource.isPlaying) audioSource.Pause(); // 或者 Stop
                    return;
                }
                if (clipLocalTime < 0)
                {
                    clipLocalTime = 0;
                }
            }

            // 再次钳制，确保绝对安全 (AudioSource.time throw error if out of bounds)
            // 注意：time 不能等于 length，必须小于 length
            clipLocalTime = Mathf.Clamp(clipLocalTime, 0f, clipLength - 0.001f);
            
            // 如果时间偏差过大（说明发生了 Seek/Scrub），强制同步
            // 注意：正常播放时 AudioSource 时间与 Editor 时间会有微小漂移，阈值不能太小
            // 另外，当 GlobalPlaySpeed 为 0 (暂停) 时，AudioSource 应该暂停
            
            if (context.GlobalPlaySpeed == 0f)
            {
                if (audioSource.isPlaying) audioSource.Pause();
                audioSource.time = clipLocalTime;
            }
            else
            {
                if (!audioSource.isPlaying && clipLocalTime < clipLength) audioSource.Play();
                
                if (Mathf.Abs(audioSource.time - clipLocalTime) > 0.1f)
                {
                    audioSource.time = clipLocalTime;
                }
            }
        }

        public override void OnExit()
        {
            // 归还 AudioSource 到池
            if (audioSource != null)
            {
                EditorAudioManager.Instance.Return(audioSource);
                audioSource = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            audioSource = null;
            _playingClip = null;
        }
    }
}
