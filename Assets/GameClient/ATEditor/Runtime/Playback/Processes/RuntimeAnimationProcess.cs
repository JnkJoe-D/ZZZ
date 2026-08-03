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
            // 调用接口播放控制 + 设置速度
            if (animHandler != null)
            {
                animHandler.PlayAnimation(clip.animationClip, (int)clip.layer, clip.BlendInDuration, clip.playbackSpeed * context.GlobalPlaySpeed);
            }
            //这里的update频率比monoupdate低，所以在onenter先同步一次播放速度，确保动画按预期速度开始播放
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.GlobalPlaySpeed);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 仅控制播放状态和速度
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.GlobalPlaySpeed); // 叠加全局播放速度
        }
        public override void OnPause()
        {
            animHandler?.SetLayerSpeed((int)clip.layer, 0);
        }
        public override void OnResume()
        {
            animHandler?.SetLayerSpeed((int)clip.layer, clip.playbackSpeed * context.GlobalPlaySpeed);
        }
        public override void OnExit()
        {
            if (clip.overrideMask != null)
            {
                context.PopLayerMask((int)clip.layer, clip.overrideMask);
            }
        }
        public override void OnDisable()
        {
            if (clip.overrideMask != null)
            {
                context.PopLayerMask((int)clip.layer, clip.overrideMask);
            }
        }
        public override void Reset()
        {
            base.Reset();
            animHandler = null;
        }
    }
}
