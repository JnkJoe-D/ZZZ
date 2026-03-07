using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class SkillAudioClip : ClipBase
    {
        [Header("Audio Settings")]
        [SkillProperty("音频资源池(随机选用)")]
        [HideInInspector]
        [SerializeField]
        public System.Collections.Generic.List<AudioClip> audioClips = new System.Collections.Generic.List<AudioClip>();
        
        [SkillProperty("音量")]
        [Range(0f, 1f)]
        public float volume = 1.0f;

        [SkillProperty("音调")]
        [Range(0.1f, 3f)]
        public float pitch = 1.0f;

        [SkillProperty("循环播放")]
        public bool loop = false;

        [SkillProperty("速度同步")]
        public bool isAffectSpeed = false;

        [SkillProperty("空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float spatialBlend = 0.0f;

        [SerializeField][HideInInspector]
        public System.Collections.Generic.List<string> clipGuids = new System.Collections.Generic.List<string>();
        [SerializeField][HideInInspector]
        public System.Collections.Generic.List<string> clipAssetNames = new System.Collections.Generic.List<string>();
        [SerializeField][HideInInspector]
        public System.Collections.Generic.List<string> clipAssetPaths = new System.Collections.Generic.List<string>();
        
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
            var clone = new SkillAudioClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                volume = this.volume,
                pitch = this.pitch,
                loop = this.loop,
                isAffectSpeed = this.isAffectSpeed,
                spatialBlend = this.spatialBlend,
                blendInDuration = this.blendInDuration,
                blendOutDuration = this.blendOutDuration
            };

            foreach (var clip in this.audioClips) clone.audioClips.Add(clip);
            foreach (var guid in this.clipGuids) clone.clipGuids.Add(guid);
            foreach (var name in this.clipAssetNames) clone.clipAssetNames.Add(name);
            foreach (var path in this.clipAssetPaths) clone.clipAssetPaths.Add(path);

            return clone;
        }
    }
}
