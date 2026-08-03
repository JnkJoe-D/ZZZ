using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 动画系统抽象接口
    /// 用于解耦 SkillEditor 与具体的动画组件 (AnimComponent)
    /// </summary>
    public interface IAnimationHandler
    {
        // 遮罩管理
        void SetLayerMask(int layerIndex, AvatarMask mask);
        AvatarMask GetLayerMask(int layerIndex);

        // 播放控制
        void PlayAnimation(UnityEngine.AnimationClip clip, int layerIndex, float fadeDuration, float speed);
        void SetLayerSpeed(int layerIndex, float speed);
        
        // 基础属性
        void Initialize();
    }
}
