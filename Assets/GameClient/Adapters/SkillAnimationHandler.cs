using Game.MAnimSystem;
using SkillEditor;
using UnityEngine;

namespace Game.Adapters
{
    /// <summary>
    /// AnimComponent 的适配器，实现 SkillEditor 的动画接口
    /// </summary>
    public class SkillAnimationHandler : ISkillAnimationHandler
    {
        private readonly AnimComponent _target;

        public SkillAnimationHandler(AnimComponent target)
        {
            _target = target;
        }

        public void Initialize()
        {
            _target?.Initialize();
            _target?.InitializeGraph();
        }

        public void SetLayerMask(int layerIndex, AvatarMask mask)
        {
            _target?.SetLayerMask(layerIndex, mask);
        }

        public AvatarMask GetLayerMask(int layerIndex)
        {
            return _target?.GetLayerMask(layerIndex);
        }

        public void PlayAnimation(AnimationClip clip, int layerIndex, float fadeDuration, float speed)
        {
            if (_target == null) return;
            _target.Play(clip, layerIndex, fadeDuration);
            _target.SetLayerSpeed(layerIndex, speed);
        }

        public void SetLayerSpeed(int layerIndex, float speed)
        {
            _target?.SetLayerSpeed(layerIndex, speed);
        }

    }
}
