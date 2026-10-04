using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Editor.ActionTransition
{
    public enum PreviewChannel
    {
        All,        // 全部 (双轨过渡混合)
        SourceOnly, // 仅源动作 Solo
        TargetOnly  // 仅目标动作 Solo
    }

    /// <summary>
    /// 动作过渡双轨求值播放器。
    /// 基于 Unity PlayableGraph 实现毫秒级精确的双动作平滑 Crossfade 混合与交互采样。
    /// 支持完整 ActionTimeline 内部多 AnimationClip 的动态无缝映射，脱机无副作用，
    /// 并在每次采样后显式触发 Animator 姿态刷新，保证角色骨骼动画 100% 正常播放，杜绝 T-Pose 假死。
    /// </summary>
    public sealed class ActionTransitionPreviewPlayer : IDisposable
    {
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _playableA;
        private AnimationClipPlayable _playableB;
        private Animator _animator;

        private ActionClipInfoExtractor.ExtractedClipData _sourceData;
        private ActionClipInfoExtractor.ExtractedClipData _targetData;

        private UnityEngine.AnimationClip _currentBoundClipA;
        private UnityEngine.AnimationClip _currentBoundClipB;

        private float _exitTime = 0.5f;
        public float ExitTime
        {
            get => _exitTime;
            set
            {
                _exitTime = Mathf.Max(0f, value);
                RecalculateTotalDuration();
            }
        }

        private float _crossfadeDuration = 0.2f;
        public float CrossfadeDuration
        {
            get => _crossfadeDuration;
            set
            {
                _crossfadeDuration = Mathf.Max(0f, value);
                RecalculateTotalDuration();
            }
        }

        private PreviewChannel _activeChannel = PreviewChannel.All;
        public PreviewChannel ActiveChannel
        {
            get => _activeChannel;
            set
            {
                _activeChannel = value;
                RecalculateTotalDuration();
                EvaluateAt(CurrentTime);
            }
        }

        public float TotalDuration { get; private set; } = 1.5f;
        public float SourceDuration => _sourceData.TimelineDuration;

        public void RecalculateTotalDuration()
        {
            float durA = _sourceData.TimelineDuration > 0.001f ? _sourceData.TimelineDuration : 1f;
            float durB = _targetData.TimelineDuration > 0.001f ? _targetData.TimelineDuration : 1f;

            if (_activeChannel == PreviewChannel.SourceOnly)
            {
                TotalDuration = durA;
            }
            else if (_activeChannel == PreviewChannel.TargetOnly)
            {
                TotalDuration = durB;
            }
            else if (IsSimulateInputMode && !HasSimulatedInputTriggered)
            {
                TotalDuration = durA;
            }
            else
            {
                // 常规双轨预览模式：时间指针范围永远完整覆盖源动作与后置目标动作，无论目标动作放置在多后面均不限制指针移动
                TotalDuration = Mathf.Max(durA, _exitTime + durB, _exitTime + _crossfadeDuration + 0.1f, 1.0f);
            }
        }

        public float CurrentTime { get; set; } = 0f;
        public bool IsPlaying { get; set; } = false;
        public float PlaybackSpeed { get; set; } = 1.0f;

        // 模拟输入模式 (Simulate Input Mode) 支持
        public bool IsSimulateInputMode { get; set; } = false;
        public bool HasSimulatedInputTriggered { get; set; } = false;

        // 循环播放设置 (默认为 true，false 则播完暂停)
        public bool IsLooping { get; set; } = true;
        public Action OnPlaybackEnded;

        // 局部聚焦循环（Loop Focus）设置
        public bool IsLoopFocus { get; set; } = false;
        public float FocusPreTime { get; set; } = 0.35f;
        public float FocusPostTime { get; set; } = 0.35f;

        public bool IsInitialized => _graph.IsValid();

        public float FocusStartTime => Mathf.Max(0f, ExitTime - FocusPreTime);
        public float FocusEndTime => Mathf.Min(TotalDuration, ExitTime + CrossfadeDuration + FocusPostTime);

        /// <summary>
        /// 初始化 PlayableGraph 并绑定到宿主 Animator
        /// </summary>
        public void Initialize(Animator animator)
        {
            Dispose();
            if (animator == null) return;

            _animator = animator;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create("ActionTransitionWorkbench_Graph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            var output = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _animator);
            output.SetSourcePlayable(_mixer);

            _graph.Play();
        }

        /// <summary>
        /// 配置源动作与目标动作的完整提取数据
        /// </summary>
        public void SetupClips(
            ActionClipInfoExtractor.ExtractedClipData sourceData,
            ActionClipInfoExtractor.ExtractedClipData targetData,
            float exitTime,
            float crossfadeDuration)
        {
            _sourceData = sourceData;
            _targetData = targetData;
            _exitTime = Mathf.Max(0f, exitTime);
            _crossfadeDuration = Mathf.Max(0f, crossfadeDuration);

            RecalculateTotalDuration();

            // 触发一次即时求值
            EvaluateAt(CurrentTime);
        }

        /// <summary>
        /// 模拟输入模式：接收视口鼠标单击输入，在当前时间点瞬时开始过渡衔接
        /// </summary>
        public void TriggerSimulatedInput(float clickTime)
        {
            ExitTime = Mathf.Max(0f, clickTime);
            CurrentTime = ExitTime; // 保持时间轴与左边界一致，在此刻自动开始衔接
            HasSimulatedInputTriggered = true;

            RecalculateTotalDuration();

            EvaluateAt(CurrentTime);
        }

        // 播完时间轴后是否复位角色位置
        public bool ResetPoseOnLoop { get; set; } = true;
        public Action OnLoopCycleReset;

        /// <summary>
        /// 推进播放时间
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying || !_graph.IsValid()) return;

            float dt = deltaTime * PlaybackSpeed;
            CurrentTime += dt;

            if (IsLoopFocus)
            {
                float start = FocusStartTime;
                float end = FocusEndTime;
                if (end - start < 0.1f) end = start + 0.1f;

                if (CurrentTime > end || CurrentTime < start)
                {
                    if (IsLooping)
                    {
                        CurrentTime = start;
                        if (ResetPoseOnLoop)
                        {
                            OnLoopCycleReset?.Invoke();
                        }
                    }
                    else
                    {
                        CurrentTime = end;
                        Pause();
                        OnPlaybackEnded?.Invoke();
                    }
                }
            }
            else
            {
                if (CurrentTime > TotalDuration)
                {
                    if (IsLooping)
                    {
                        CurrentTime = 0f;
                        if (ResetPoseOnLoop)
                        {
                            OnLoopCycleReset?.Invoke();
                        }

                        // 模拟输入模式下，整个衔接与目标动作播完回绕后，自动恢复为默认只播原动作状态
                        if (IsSimulateInputMode && HasSimulatedInputTriggered)
                        {
                            HasSimulatedInputTriggered = false;
                            RecalculateTotalDuration();
                        }
                    }
                    else
                    {
                        CurrentTime = TotalDuration;
                        Pause();
                        OnPlaybackEnded?.Invoke();
                    }
                }
            }

            EvaluateAt(CurrentTime);
        }

        /// <summary>
        /// 在特定时间点对双动作混合状态进行求值
        /// </summary>
        public void EvaluateAt(float time)
        {
            CurrentTime = Mathf.Clamp(time, 0f, TotalDuration);
            if (!_graph.IsValid() || _animator == null) return;

            if (_activeChannel == PreviewChannel.SourceOnly)
            {
                // 仅源动作独立预览：100% 播放源动作全程，无过渡截断与干扰
                var (clipA, localTimeA) = _sourceData.SampleAt(CurrentTime);
                BindClipPlayable(0, clipA, ref _playableA, ref _currentBoundClipA);
                if (_playableA.IsValid()) _playableA.SetTime(localTimeA);
                _mixer.SetInputWeight(0, 1.0f);
                _mixer.SetInputWeight(1, 0.0f);
            }
            else if (_activeChannel == PreviewChannel.TargetOnly)
            {
                // 仅目标动作独立预览：100% 从 0 秒开始播放目标动作全程，不受源动作影响
                var (clipB, localTimeB) = _targetData.SampleAt(CurrentTime);
                BindClipPlayable(1, clipB, ref _playableB, ref _currentBoundClipB);
                if (_playableB.IsValid()) _playableB.SetTime(localTimeB);
                _mixer.SetInputWeight(0, 0.0f);
                _mixer.SetInputWeight(1, 1.0f);
            }
            else
            {
                // 常规双轨过渡预览：根据 ExitTime 与 Crossfade 进行实时平滑插值
                var (clipA, localTimeA) = _sourceData.SampleAt(CurrentTime);
                float targetTimeOffset = Mathf.Max(0f, CurrentTime - ExitTime);
                var (clipB, localTimeB) = _targetData.SampleAt(targetTimeOffset);

                BindClipPlayable(0, clipA, ref _playableA, ref _currentBoundClipA);
                BindClipPlayable(1, clipB, ref _playableB, ref _currentBoundClipB);

                float weightA = 1.0f;
                float weightB = 0.0f;

                if (IsSimulateInputMode && !HasSimulatedInputTriggered)
                {
                    weightA = 1.0f;
                    weightB = 0.0f;
                }
                else if (CrossfadeDuration <= 0.0001f)
                {
                    if (CurrentTime < ExitTime)
                    {
                        weightA = 1.0f;
                        weightB = 0.0f;
                    }
                    else
                    {
                        weightA = 0.0f;
                        weightB = 1.0f;
                    }
                }
                else
                {
                    if (CurrentTime < ExitTime)
                    {
                        weightA = 1.0f;
                        weightB = 0.0f;
                    }
                    else if (CurrentTime < ExitTime + CrossfadeDuration)
                    {
                        float blend = Mathf.Clamp01((CurrentTime - ExitTime) / CrossfadeDuration);
                        weightA = 1.0f - blend;
                        weightB = blend;
                    }
                    else
                    {
                        weightA = 0.0f;
                        weightB = 1.0f;
                    }
                }

                if (clipB == null)
                {
                    weightA = 1f;
                    weightB = 0f;
                }

                if (_playableA.IsValid())
                {
                    _playableA.SetTime(localTimeA);
                    _mixer.SetInputWeight(0, weightA);
                }

                if (_playableB.IsValid())
                {
                    _playableB.SetTime(localTimeB);
                    _mixer.SetInputWeight(1, weightB);
                }
            }

            // 1. 手动推进 PlayableGraph
            _graph.Evaluate(0f);

            // 2. 关键：显式触发 Animator 姿态刷新，确保在编辑器非运行期骨骼姿态必定立即更新！
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.Update(0f);
            }
        }

        private void BindClipPlayable(int inputIndex, UnityEngine.AnimationClip clip, ref AnimationClipPlayable playable, ref UnityEngine.AnimationClip currentClip)
        {
            if (currentClip == clip && playable.IsValid()) return;

            if (playable.IsValid())
            {
                _graph.Disconnect(_mixer, inputIndex);
                _graph.DestroySubgraph(playable);
            }

            currentClip = clip;

            if (clip != null)
            {
                playable = AnimationClipPlayable.Create(_graph, clip);
                _graph.Connect(playable, 0, _mixer, inputIndex);
            }
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        /// 模拟输入模式状态重置：清除已触发状态并恢复为原动作时长
        /// </summary>
        public void ResetSimulatedInputState()
        {
            HasSimulatedInputTriggered = false;
            TotalDuration = _sourceData.TimelineDuration;
        }

        /// <summary>
        /// 仅将播放时间复位至起点，绝不篡改当前的播放/暂停状态；在模拟输入模式下重置目标动作播放中的等待回绕状态
        /// </summary>
        public void ResetToStart()
        {
            CurrentTime = IsLoopFocus ? FocusStartTime : 0f;
            if (IsSimulateInputMode)
            {
                ResetSimulatedInputState();
            }
            EvaluateAt(CurrentTime);
        }

        public void Dispose()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
            _currentBoundClipA = null;
            _currentBoundClipB = null;
            _animator = null;
        }
    }
}
