using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(AudioTrack), "音频")]
    public class AudioClip : ClipBase
    {
        [Header("音频配置")]
        [ActionProperty("音频资源池(随机选用)")]
        [HideInInspector]
        [SerializeField]
        public System.Collections.Generic.List<UnityEngine.AudioClip> audioClips = new System.Collections.Generic.List<UnityEngine.AudioClip>();
        
        [ActionProperty("音量")]
        [Range(0f, 1f)]
        public float volume = 1.0f;

        [ActionProperty("音调")]
        [Range(0.1f, 3f)]
        public float pitch = 1.0f;

        [ActionProperty("循环播放")]
        public bool loop = false;

        [ActionProperty("速度同步")]
        public bool isAffectSpeed = false;

        [ActionProperty("空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f;

        [ActionAssetReference("audioClips")]
        public System.Collections.Generic.List<ActionAssetReference> audioRefs = new System.Collections.Generic.List<ActionAssetReference>();
        
        public override bool SupportsBlending => true;

        public AudioClip()
        {
            clipName = "Audio Clip";
            duration = 1.0f;
            volume = 1.0f;
            pitch = 1.0f;
            spatialBlend = 0.0f;
        }

        public override ClipBase Clone()
        {
            var clone = new AudioClip
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
            foreach (var r in this.audioRefs) clone.audioRefs.Add(new ActionAssetReference(r.guid, r.assetName, r.assetPath));

            return clone;
        }
    }
}
