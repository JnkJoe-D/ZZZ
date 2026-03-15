using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 音频播放接口
    /// 用于解耦 SkillEditor 与具体游戏的音频系统
    /// </summary>
    public interface ISkillAudioHandler
    {
        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="args">播放参数</param>
        /// <returns>返回唯一的播放ID，用于后续控制</returns>
        int PlaySound(UnityEngine.AudioClip clip, AudioArgs args);

        /// <summary>
        /// 停止指定ID的音频
        /// </summary>
        void StopSound(int soundId);

        /// <summary>
        /// 更新音频状态（如时间、参数）
        /// </summary>
        void UpdateSound(int soundId, float volume, float pitch, float time);

        /// <summary>
        /// 暂停指定ID的音频
        /// </summary>
        void PauseSound(int soundId);

        /// <summary>
        /// 恢复指定ID的音频
        /// </summary>
        void ResumeSound(int soundId);

        /// <summary>
        /// 停止所有音频（通常用于清理或重置）
        /// </summary>
        void StopAll();
    }

    /// <summary>
    /// 音频播放参数
    /// </summary>
    public struct AudioArgs
    {
        public float volume;
        public float pitch;
        public bool loop;
        public float spatialBlend; // 0=2D, 1=3D
        public float startTime;    // 起始播放时间（秒）
        public Vector3 position;   // 3D音效位置
        public Transform parent;
    }
}
