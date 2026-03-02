using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class SkillAudioClip : ClipBase
    {
        [Header("Audio Settings")]
        [SkillProperty("音频资源")]
        public AudioClip audioClip;
        
        [SkillProperty("音量")]
        [Range(0f, 1f)]
        public float volume = 1.0f;

        [SkillProperty("音调")]
        [Range(0.1f, 3f)]
        public float pitch = 1.0f;

        [SkillProperty("循环播放")]
        public bool loop = false;

        [SkillProperty("空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float spatialBlend = 0.0f;

        [SerializeField]
        public string clipGuid;
        [SerializeField]
        public string clipAssetName;
        
        public override bool SupportsBlending => true;

        public SkillAudioClip()
        {
            clipName = "Audio Clip";
            duration = 1.0f;
            volume = 1.0f;
            pitch = 1.0f;
            spatialBlend = 0.0f;
        }

        public override ClipBase Clone()
        {
            return new SkillAudioClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                audioClip = this.audioClip,
                clipGuid = this.clipGuid,
                clipAssetName = this.clipAssetName,

                volume = this.volume,
                loop = this.loop,
                spatialBlend = this.spatialBlend,
                blendInDuration = this.blendInDuration,
                blendOutDuration = this.blendOutDuration
            };
        }
    }
}
