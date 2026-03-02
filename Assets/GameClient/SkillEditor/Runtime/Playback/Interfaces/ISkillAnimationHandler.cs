using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 动画系统抽象接口
    /// 用于解耦 SkillEditor 与具体的动画组件 (AnimComponent)
    /// </summary>
    public interface ISkillAnimationHandler
    {
        // 遮罩管理
        void SetLayerMask(int layerIndex, AvatarMask mask);
        AvatarMask GetLayerMask(int layerIndex);

        // 播放控制
        void PlayAnimation(AnimationClip clip, int layerIndex, float fadeDuration, float speed);
        void SetLayerSpeed(int layerIndex, float speed);
        
        // 基础属性
        void Initialize();
        void ClearPlayGraph();

        // 采样与手动更新（编辑器预览用）
        void Evaluate(AnimationClip clip, int layerIndex, float time);
        void ManualUpdate(float deltaTime);
    }
}
