using System.Collections.Generic;
using UnityEngine;
using Game.MAnimSystem;
using Game.Logic;
using ATEditor;
using Game.Adapters;

namespace Game.MAnimSystem.Test
{
    /// <summary>
    /// 测试脚本：使用 SkillEditor 核心组件 (SkillRunner) 自动循环播放 ActionConfigAsset。
    /// 作为引入“时间轴调度器”后的最小复现单元，验证其切招时的底层逻辑表现。
    /// </summary>
    public class Test4 : MonoBehaviour
    {
        [Header("References")]
        public Animator targetAnimator;
        
        [Header("Action Sequence")]
        public List<ActionConfigAsset> actions = new List<ActionConfigAsset>();
        
        [Header("Settings")]
        [Range(0.1f, 10f)]
        public float playSpeed = 1f;
        
        private AnimComponent _animComponent;
        private ActionRunner _runner;
        private ProcessContext _context;
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
                
                _animComponent.Initialize();
                _animComponent.InitializeGraph();
            }

            if (actions != null && actions.Count > 0)
            {
                // 初始化 SkillEditor 上下文，并绑定动画适配器
                _context = new ProcessContext(targetAnimator.gameObject, ATEditor.PlayMode.Runtime, (type, owner) => {
                    if (type == typeof(IAnimationHandler)) return new ATAnimationHandler(_animComponent);
                    return null;
                });
                
                PlayCurrent();
            }
            else
            {
                Debug.LogWarning("[Test4] 动作列表为空！");
            }
        }

        private void Update()
        {
            if (_runner == null || _animComponent == null || actions.Count == 0) return;

            // 应用播放速度
            _context.GlobalPlaySpeed = playSpeed;

            // 推进时间轴
            _runner.Tick(Time.deltaTime);
            _timer += Time.deltaTime * playSpeed;

            // 无论动作底层是不是勾选了 isLoop，只要播放满了一个周期的时间，就强制切下一个
            if (_runner.Timeline != null && _timer >= _runner.Timeline.Duration)
            {
                float overshoot = _timer - _runner.Timeline.Duration;
                _currentIndex = (_currentIndex + 1) % actions.Count;
                PlayCurrent(overshoot);
            }
        }

        private void PlayCurrent(float overshoot = 0f)
        {
            var config = actions[_currentIndex];
            if (config != null)
            {
                ActionTimeline timeline = config.actionTimelineSO;
                if (timeline == null && config.TimelineAsset != null)
                {
                    timeline = ATEditor.SerializationUtility.OpenFromJson(config.TimelineAsset);
                }

                if (timeline != null)
                {
                    _timer = overshoot;

                    if (_runner != null)
                    {
                        _runner.Stop();
                    }

                    _runner = new ActionRunner(ATEditor.PlayMode.Runtime);

                    // 如果动作配置了强制的过渡时间 (我们之前加的功能)，注给 Context
                    if (config.CompleteTransitCrossfade >= 0f)
                    {
                        _context.TransitionCrossfadeOverride = config.CompleteTransitCrossfade;
                    }

                    _runner.Play(timeline, _context, overshoot);
                    Debug.Log($"[Test4] SkillRunner 切换 -> {config.name}");
                }
            }
        }
    }
}
