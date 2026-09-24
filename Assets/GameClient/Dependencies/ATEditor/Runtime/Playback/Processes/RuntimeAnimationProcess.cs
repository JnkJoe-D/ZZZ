using UnityEngine;
using System;
using System.Collections.Generic;
namespace ATEditor
{
    /// <summary>
    /// 运行时：动画片段 Process 骨架
    /// 仅控制播放状态和速度，不控制权重
    /// </summary>
    [ProcessBinding(typeof(AnimationClip), PlayMode.Runtime)]
    public class RuntimeAnimationProcess : ProcessBase<AnimationClip>
    {
        // 使用抽象接口替代具体实现
        private IAnimationHandler animHandler;
        
        public override void OnEnable()
        {
            animHandler = context.GetService<IAnimationHandler>(); // 懒加载
            animHandler?.Initialize();
        }

        public override void OnEnter()
        {
            if (clip.overrideMask != null)
            {
                context.PushLayerMask((int)clip.layer, clip.overrideMask);
            }
            
            float actualBlendIn = clip.BlendInDuration;
            if (context.TransitionCrossfadeOverride.HasValue && clip.StartTime <= 0.001f)
            {
                float overrideValue = context.TransitionCrossfadeOverride.Value;
                if (overrideValue >= 0f)
                {
                    actualBlendIn = overrideValue;
                }
                context.TransitionCrossfadeOverride = null;
            }

            // 调用接口播放控制 + 设置速度
            if (animHandler != null)
            {
                // timelineOffset：时间轴头进入片段后已推进的相对时间（0 ~ clip.Duration）
                float timelineOffset = context.CurrentTime - clip.StartTime;
                // animOffset：动画内部起始偏移（秒），由片段配置决定
                float animOffset = clip.GetResolvedAnimStartOffsetSeconds();
                animHandler.PlayAnimation(clip.animationClip, (int)clip.layer, actualBlendIn, clip.playbackSpeed * context.PresentationPlaySpeed, animOffset + timelineOffset);
            }
            //这里的update频率比monoupdate低，所以在onenter先同步一次播放速度，确保动画按预期速度开始播放
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.PresentationPlaySpeed);

            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
                context.OnPresentationSpeedChanged += HandlePresentationSpeedChanged;
            }
        }

        private void HandlePresentationSpeedChanged(float newSpeed)
        {
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * newSpeed);
        }

        public override void OnSeek(float targetTime)
        {
            if (animHandler != null)
            {
                float timelineOffset = targetTime - clip.StartTime;
                float animOffset = clip.GetResolvedAnimStartOffsetSeconds();
                animHandler.SetTime((int)clip.layer, animOffset + timelineOffset);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 仅控制播放状态和速度
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.PresentationPlaySpeed); // 叠加表现层播放速度
        }
        public override void OnPause()
        {
            animHandler?.SetLayerSpeed((int)clip.layer, 0);
        }
        public override void OnResume()
        {
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.PresentationPlaySpeed);
        }
        public override void OnExit()
        {
            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
            }

            if (clip.overrideMask != null)
            {
                context.PopLayerMask((int)clip.layer, clip.overrideMask);
            }
        }
        public override void OnStop()
        {
            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
            }

            if (clip.overrideMask != null)
            {
                context.PopLayerMask((int)clip.layer, clip.overrideMask);
            }
        }
        public override void Reset()
        {
            base.Reset();
            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
            }
            animHandler = null;
        }
    }
}
