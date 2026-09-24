using System;
using UnityEngine;

namespace ATEditor
{
    // 注意：Unity 也有 UnityEngine.AnimationClip，这里需要避免命名冲突
    // 但根据原文件，类名确实是 AnimationClip。建议重命名为 SkillAnimationClip 以避免混淆。
    // 不过为了保持兼容性，先保留原名，但在使用时需全名引用 UnityEngine.AnimationClip
    
    [Serializable]
    [ClipDefinition(typeof(AnimationTrack), "动画")]
    public class AnimationClip : ClipBase
    {
        // ── 动画起始偏移模式枚举（嵌套在 AnimationClip 内，避免污染全局命名空间）────
        public enum EAnimStartOffsetMode
        {
            /// <summary>不偏移，从动画第 0 帧开始（默认，零额外开销）</summary>
            Disabled   = 0,
            /// <summary>绝对秒数偏移，Inspector 限制范围 [0, animationClip.length]</summary>
            Seconds    = 1,
            /// <summary>归一化百分比偏移 [0, 1]，运行时换算为秒</summary>
            Normalized = 2,
        }

        [ActionProperty("动画资源")]
        public UnityEngine.AnimationClip animationClip;
        
        [ActionProperty("播放速度")]
        public float playbackSpeed = 1.0f;
        [ActionProperty("目标动画层")]
        public EAnimLayer layer = EAnimLayer.Locomotion;
        [ActionProperty("目标动画遮罩")]
        public AvatarMask overrideMask;
        [ActionAssetReference("animationClip")]
        public ActionAssetReference clipRef = new ActionAssetReference();

        [ActionAssetReference("overrideMask")]
        public ActionAssetReference maskRef = new ActionAssetReference();
        public override bool SupportsBlending => true;

        // ── 动画起始偏移配置 ──────────────────────────────────────────────────────
        [ActionProperty("起始偏移模式")]
        public EAnimStartOffsetMode animStartOffsetMode = EAnimStartOffsetMode.Disabled;

        /// <summary>
        /// 绝对偏移量（秒）。仅在 mode == Seconds 时生效。
        /// Inspector 负责将其钳位到 [0, animationClip.length]。
        /// </summary>
        [ActionProperty("起始偏移(秒)")]
        public float animStartOffsetSeconds = 0f;

        /// <summary>
        /// 归一化偏移量（0~1）。仅在 mode == Normalized 时生效。
        /// </summary>
        [ActionProperty("起始偏移(%)")]
        [Range(0f, 1f)]
        public float animStartOffsetNormalized = 0f;

        /// <summary>
        /// 根据配置计算实际需要传给底层动画系统的起始时间偏移（秒）。
        /// 当 animationClip 为 null 或 mode == Disabled 时返回 0f（安全退化，零开销）。
        /// </summary>
        public float GetResolvedAnimStartOffsetSeconds()
        {
            if (animStartOffsetMode == EAnimStartOffsetMode.Disabled) return 0f;

            float clipLength = animationClip != null ? animationClip.length : 0f;
            if (clipLength <= 0f) return 0f;

            float offset = animStartOffsetMode == EAnimStartOffsetMode.Seconds
                ? animStartOffsetSeconds
                : Mathf.Clamp01(animStartOffsetNormalized) * clipLength;

            // A 方案兜底：确保偏移不超出动画总长（Inspector 已做约束，此处为最终安全防线）
            return Mathf.Clamp(offset, 0f, clipLength);
        }

        public AnimationClip()
        {
            clipName = "动画片段";
            duration = 1.0f;
        }

        public override ClipBase Clone()
        {
            return new AnimationClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                animationClip = this.animationClip,
                playbackSpeed = this.playbackSpeed,
                clipRef = new ActionAssetReference(this.clipRef.guid, this.clipRef.assetName, this.clipRef.assetPath),
                maskRef = new ActionAssetReference(this.maskRef.guid, this.maskRef.assetName, this.maskRef.assetPath),
                layer = this.layer,
                overrideMask = this.overrideMask,
                blendInDuration = this.blendInDuration,
                blendOutDuration = this.blendOutDuration,
                // ── 偏移字段拷贝 ──
                animStartOffsetMode       = this.animStartOffsetMode,
                animStartOffsetSeconds    = this.animStartOffsetSeconds,
                animStartOffsetNormalized = this.animStartOffsetNormalized,
            };
        }
    }
}
