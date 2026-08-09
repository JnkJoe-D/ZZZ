using System.Collections.Generic;
using UnityEngine;

namespace Game.MAnimSystem.Test
{
    /// <summary>
    /// 测试脚本：使用 MAnimSystem 自动循环进行 0 秒硬切播放，验证硬切导致的空间跳变/速度丢失现象。
    /// 依次从列表播放动画，每个动画播放到底后立刻以 0秒 过渡切到下一个。
    /// </summary>
    public class Test3 : MonoBehaviour
    {
        [Header("References")]
        public Animator targetAnimator;
        
        [Header("Animation Sequence")]
        public List<AnimationClip> animations = new List<AnimationClip>();
        
        [Header("Settings")]
        [Range(0.1f, 10f)]
        public float playSpeed = 1f;
        
        private AnimComponent _animComponent;
        private int _currentIndex = 0;
        private float _timer = 0f;

        private void Start()
        {
            if (targetAnimator == null)
            {
                targetAnimator = GetComponentInChildren<Animator>();
            }

            if (targetAnimator != null)
            {
                _animComponent = targetAnimator.gameObject.GetComponent<AnimComponent>();
                if (_animComponent == null)
                {
                    _animComponent = targetAnimator.gameObject.AddComponent<AnimComponent>();
                }
                
                // 初始化底层 PlayableGraph
                _animComponent.Initialize();
                _animComponent.InitializeGraph();
            }

            if (animations != null && animations.Count > 0)
            {
                PlayCurrent();
            }
            else
            {
                Debug.LogWarning("[Test3] 动画列表为空！");
            }
        }

        private void Update()
        {
            if (_animComponent == null || animations == null || animations.Count == 0) return;

            _animComponent.SetLayerSpeed(0, playSpeed);

            var currentClip = animations[_currentIndex];
            if (currentClip == null) return;

            _timer += Time.deltaTime * playSpeed;

            // 当当前动画播放到结尾时，触发 0秒硬切 切换到下一个动画
            if (_timer >= currentClip.length)
            {
                // 计算帧溢出（残差）时间
                float overshoot = _timer - currentClip.length;
                // 如果是第一次播放或者出错了，防止负数
                if (overshoot < 0f) overshoot = 0f;

                _currentIndex = (_currentIndex + 1) % animations.Count;
                PlayCurrent(overshoot);
            }
        }

        private void PlayCurrent(float overshoot = 0f)
        {
            var clip = animations[_currentIndex];
            if (clip != null)
            {
                // 保留残差时间，而不是归零
                _timer = overshoot;
                
                // fadeDuration = 0f 表示 0秒硬切
                // forceResetTime = false 表示不强制从头0秒播
                var state = _animComponent.Play(clip, 0f, false);
                if (state != null)
                {
                    // 把溢出的那几毫秒塞进底层 Playable 的进度里，完美同步！
                    state.Time = overshoot;
                }
                Debug.Log($"[Test3] 0秒硬切 -> {clip.name} (Length: {clip.length:F2}s, Overshoot: {overshoot:F4}s)");
            }
        }
    }
}
