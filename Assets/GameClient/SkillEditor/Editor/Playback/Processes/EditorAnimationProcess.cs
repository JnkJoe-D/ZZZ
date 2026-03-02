using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器预览：动画片段 Process
    /// 调用 AnimComponent 的播放控制与采样方法
    /// </summary>
    [ProcessBinding(typeof(SkillAnimationClip), PlayMode.EditorPreview)]
    public class EditorAnimationProcess : ProcessBase<SkillAnimationClip>
    {
        // 使用抽象接口替代具体实现
        private ISkillAnimationHandler animHandler;
        public override void OnEnable()
        {
            animHandler = context.GetService<ISkillAnimationHandler>(); // 懒加载
            animHandler?.Initialize();
            // 注册系统级清理（多个动画 Process 共享同一个 key，仅执行一次）
            context.RegisterCleanup("ClearPlaygraph",
             () =>
             {
                 animHandler?.ClearPlayGraph();
                 context.Owner.transform.position = Vector3.zero;
             }); 
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
                animHandler.PlayAnimation(clip.animationClip, (int)clip.layer, clip.blendInDuration, clip.playbackSpeed * context.GlobalPlaySpeed);
            }
            animHandler?.SetLayerSpeed((int)clip.layer,0f); // 先暂停，等待 OnUpdate 采样
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 仅控制播放状态
            // 编辑器预览：手动 Sample 时间
            animHandler?.Evaluate(clip.animationClip, (int)clip.layer, currentTime - clip.startTime);
            animHandler?.ManualUpdate(deltaTime);
        }

        public override void OnExit()
        {
            if (clip.overrideMask != null)
            {
                context.PopLayerMask((int)clip.layer, clip.overrideMask); 
            }
            Debug.Log($"[OnExit] OnExit at time: {UnityEngine.Time.time}");
        }
        public override void OnDisable()
        {
            // 额外的清理（如果需要）
        }
        public override void Reset()
        {
            base.Reset();
            animHandler = null;
        }
    }
}
