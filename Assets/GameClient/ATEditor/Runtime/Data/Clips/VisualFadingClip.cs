using System;
using UnityEngine;

namespace ATEditor
{
    public enum RendererTargetType
    {
        SkinnedMeshRenderer,
        MeshRenderer,
        Both
    }

    [Serializable]
    [ClipDefinition(typeof(VisualTrack), "视觉隐现")]
    public class VisualFadingClip : ClipBase
    {
        [SkillProperty("渲染器类型")]
        [Tooltip("指定要控制的渲染器类型")]
        public RendererTargetType TargetType = RendererTargetType.SkinnedMeshRenderer;

        [SkillProperty("反转逻辑(入显出隐)")]
        [Tooltip("默认行为是进入片段时隐藏，离开时显示。勾选此项则反转行为。")]
        public bool Inverse = false;

        /*
        [SkillProperty("渐入曲线")]
        public AnimationCurve BlendInCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [SkillProperty("渐出曲线")]
        public AnimationCurve BlendOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public override bool SupportsBlending => true;
        */

        public VisualFadingClip()
        {
            clipName = "Visual Fading Clip";
            duration = 1.0f;
            blendInDuration = 0.2f;
            blendOutDuration = 0.2f;
        }

        public override ClipBase Clone()
        {
            return new VisualFadingClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                TargetType = this.TargetType,
                Inverse = this.Inverse
                /*
                BlendInCurve = new AnimationCurve(this.BlendInCurve.keys),
                BlendOutCurve = new AnimationCurve(this.BlendOutCurve.keys),
                blendInDuration = this.blendInDuration,
                blendOutDuration = this.blendOutDuration
                */
            };
        }
    }
}
