using System.Collections.Generic;
using SkillEditor;
using UnityEngine;
using Game.Pool;

namespace Game.Adapters
{
    /// <summary>
    /// 运行时音频适配器
    /// 实现 ISkillAudioHandler 接口，将请求转发给全局 AudioManager
    /// </summary>
    public class GameSkillAudioHandler : MonoBehaviour, ISkillAudioHandler
    {
        public int PlaySound(UnityEngine.AudioClip clip, AudioArgs args)
        {
            if (Game.Audio.AudioManager.Instance == null) return -1;

            var globalArgs = new Game.Audio.AudioArgs
            {
                volume = args.volume,
                pitch = args.pitch,
                loop = args.loop,
                spatialBlend = args.spatialBlend,
                startTime = args.startTime,
                position = args.position,
                parent = args.spatialBlend > 0.01f ? null : transform
            };

            return Game.Audio.AudioManager.Instance.PlayAudio(clip, Game.Audio.AudioChannel.SFX, globalArgs);
        }

        public void StopSound(int soundId)
        {
            Game.Audio.AudioManager.Instance?.StopAudio(soundId);
        }

        public void PauseSound(int soundId)
        {
            Game.Audio.AudioManager.Instance?.PauseAudio(soundId);
        }

        public void ResumeSound(int soundId)
        {
            Game.Audio.AudioManager.Instance?.ResumeAudio(soundId);
        }

        public void UpdateSound(int soundId, float volume, float pitch, float time)
        {
            Game.Audio.AudioManager.Instance?.UpdateAudio(soundId, volume, pitch, time);
        }

        public void StopAll()
        {
            // Note: StopAll on adapter could mean stop all SFX, or we rely on the specific IDs.
            // If we need to stop all SFX, we should ask AudioManager, or just let AudioManager handle it.
            // For now, adapter leaves global StopAll to the Manager, or we can use a local tracking list if needed.
        }
    }
}
