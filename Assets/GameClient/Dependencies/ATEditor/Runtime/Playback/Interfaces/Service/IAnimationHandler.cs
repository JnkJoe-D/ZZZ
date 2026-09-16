using UnityEngine;

namespace ATEditor
{
    public interface IAnimationHandler:IService
    {
        // 遮罩管理
        void SetLayerMask(int layerIndex, AvatarMask mask);
        AvatarMask GetLayerMask(int layerIndex);

        // 播放控制
        void PlayAnimation(UnityEngine.AnimationClip clip, int layerIndex, float fadeDuration, float speed, float startTime = 0f);
        void SetLayerSpeed(int layerIndex, float speed);
        void SetTime(int layerIndex, float time);
        
        // 基础属性
        void Initialize();
    }
}
