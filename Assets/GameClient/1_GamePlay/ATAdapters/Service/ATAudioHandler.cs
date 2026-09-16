using ATEditor;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 运行时音频适配器
    /// 实现 ISkillAudioHandler 接口，将请求转发给全局 AudioManager
    /// </summary>
    public class ATAudioHandler :  Singleton<ATAudioHandler>,IAudioHandler
    {
        public int PlaySound(UnityEngine.AudioClip clip, ATEditor.AudioArgs args)
        {
            if (AudioManager.Instance == null) return -1;

            var globalArgs = new Game.Framework.AudioArgs
            {
                volume = args.volume,
                pitch = args.pitch,
                loop = args.loop,
                spatialBlend = args.spatialBlend,
                startTime = args.startTime,
                position = args.position,
                parent = args.parent
            };

            return AudioManager.Instance.PlayAudio(clip, AudioChannel.SFX, globalArgs);
        }

        public void StopSound(int soundId)
        {
            AudioManager.Instance?.StopAudio(soundId);
        }

        public void PauseSound(int soundId)
        {
            AudioManager.Instance?.PauseAudio(soundId);
        }

        public void ResumeSound(int soundId)
        {
            AudioManager.Instance?.ResumeAudio(soundId);
        }

        public void UpdateSound(int soundId, float volume, float pitch, float time)
        {
            AudioManager.Instance?.UpdateAudio(soundId, volume, pitch, time);
        }

        public void StopAll()
        {

        }
    }
}
