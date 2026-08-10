using Game.MAnimSystem;
using ATEditor;
using UnityEngine;

namespace Game.Adapters
{
    /// <summary>
    /// AnimComponent 的适配器，实现 SkillEditor 的动画接口
    /// </summary>
    public class ATAnimationHandler : IAnimationHandler
    {
        private readonly AnimComponent _target;

        public ATAnimationHandler(AnimComponent target)
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

        public void PlayAnimation(UnityEngine.AnimationClip clip, int layerIndex, float fadeDuration, float speed, float startTime = 0f)
        {
            if (_target == null) return;
            var state = _target.Play(clip, layerIndex, fadeDuration);
            if (state != null)
            {
                state.Time = startTime;
            }
            _target.SetLayerSpeed(layerIndex, speed);
        }

        public void SetLayerSpeed(int layerIndex, float speed)
        {
            _target?.SetLayerSpeed(layerIndex, speed);
        }

        public void SetTime(int layerIndex, float time)
        {
            _target?.SetTime(layerIndex, time);
        }

    }
}
