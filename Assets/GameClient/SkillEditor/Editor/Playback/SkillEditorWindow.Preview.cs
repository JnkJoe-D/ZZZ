using UnityEngine;
using UnityEditor;

namespace SkillEditor.Editor
{
    /// <summary>
    /// SkillEditorWindow 的预览扩展（partial class）
    /// 负责 SkillRunner 驱动的编辑器预览播放
    /// </summary>
    public partial class SkillEditorWindow
    {
        // 预览播放器
        private SkillRunner previewRunner;
        public SkillRunner PreviewRunner => previewRunner;
        private double lastPreviewTime;
        private double accumulator; // 时间累积器（用于 Fixed 模式）

        /// <summary>
        /// 是否正在播放 (供 Toolbar 使用)
        /// </summary>
        public bool IsPlaying => previewRunner != null && previewRunner.CurrentState == SkillRunner.State.Playing;

        /// <summary>
        /// 初始化预览系统（在 OnEnable 和 previewTarget 变更时调用）
        /// </summary>
        public void InitPreview()
        {
            previewRunner = new SkillRunner(PlayMode.EditorPreview);
            if (state != null)
            {
                state.previewRunner = previewRunner;
                if (state.previewTarget != null)
                {
                    var factory = SkillEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
                    var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview, factory);
                    previewRunner.PrewarmContext(ctx);
                }
            }
        }

        /// <summary>
        /// 释放预览系统（在 OnDisable 中调用）
        /// </summary>
        private void DisposePreview()
        {
            StopPreview();
            previewRunner = null;
            EditorAudioManager.Instance.Dispose();
            EditorVFXManager.Instance.Dispose();
            
            // 向外部层抛出主动销毁指令，清理那些跨程序集缓存的重对象
            SkillEditorGlobalSettings.OnEditorDispose?.Invoke();
        }

        /// <summary>
        /// 开始预览播放
        /// </summary>
        public void StartPreview()
        {
            if (state.currentTimeline == null) return;

            var factory = SkillEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
            var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview, factory);
            
            // 手动注入不再需要，由 Factory 懒加载提供
            // ctx.AddService<ISkillActor>(new CharSkillActor(state.previewTarget)); 
            
            lastPreviewTime = EditorApplication.timeSinceStartup;
            previewRunner.Play(state.currentTimeline, ctx);
        }

        /// <summary>
        /// 停止预览播放
        /// </summary>
        public void StopPreview()
        {
            previewRunner?.Stop();
        }

        /// <summary>
        /// 暂停预览播放
        /// </summary>
        public void PausePreview()
        {
            previewRunner?.Pause();
        }

        /// <summary>
        /// 恢复预览播放
        /// </summary>
        public void ResumePreview()
        {
            previewRunner?.Resume();
            lastPreviewTime = EditorApplication.timeSinceStartup;
            accumulator = 0;
        }

        /// <summary>
        /// 确保 Runner 处于活跃状态 (Running or Paused)
        /// 如果是 Idle，则自动开始并暂停，以便进行 Seek 或步进
        /// </summary>
        private void EnsureRunnerActive()
        {
            if (previewRunner == null) InitPreview();
            if (previewRunner.CurrentState == SkillRunner.State.Idle)
            {
                StartPreview();
                PausePreview();
            }
        }

        /// <summary>
        /// 切换播放/暂停
        /// </summary>
        public void TogglePlay()
        {
            if (IsPlaying)
            {
                // Playing -> Pause
                PausePreview();
                // State 同步由 Runner 驱动，不再手动设置 state.isPlaying
            }
            else
            {
                // Pause/Stop -> Play
                if (previewRunner.CurrentState == SkillRunner.State.Idle || state.isStopped)
                {
                    // 如果当前在末尾，重置回开头
                    float duration = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
                    if (state.timeIndicator >= duration - 0.05f)
                    {
                        state.timeIndicator = 0f;
                    }

                    // 如果是 Idle，StartPreview 会重置时间到 0，所以需要先 Start 再 Seek 到当前 indicator
                    // 但 StartPreview 内部是用 state.previewTarget 和 currentTimeline
                    // Runner.Play 会重置 Time=0
                    StartPreview();
                    
                    // 如果 indicator > 0，则 Seek 过去
                    if (state.timeIndicator > 0)
                    {
                        previewRunner.Seek(state.timeIndicator,state.SnapInterval);
                    }
                }
                else
                {
                    ResumePreview();
                }

                state.isStopped = false;
            }
        }

        /// <summary>
        /// 停止播放并重置
        /// </summary>
        public void Stop()
        {
            StopPreview();
            state.isStopped = true;
            state.timeIndicator = 0f;
        }

        /// <summary>
        /// 单帧前进
        /// </summary>
        public void StepForward()
        {
            // 动作前先暂停
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            
            float dt = 1.0f / (state.frameRate > 0 ? state.frameRate : 30);
            float targetTime = previewRunner.CurrentTime + dt;
            float maxTime = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
            targetTime = Mathf.Clamp(targetTime, 0, maxTime);

            previewRunner.Seek(targetTime, state.SnapInterval);
            state.timeIndicator = targetTime;
            state.isStopped = false;
        }

        /// <summary>
        /// 单帧后退
        /// </summary>
        public void StepBackward()
        {
            // 动作前先暂停
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();

            float dt = 1.0f / (state.frameRate > 0 ? state.frameRate : 30);
            float targetTime = previewRunner.CurrentTime - dt;
            targetTime = Mathf.Max(0, targetTime);

            previewRunner.Seek(targetTime, state.SnapInterval);
            state.timeIndicator = targetTime;
            state.isStopped = false;
        }

        /// <summary>
        /// 跳转到开始
        /// </summary>
        public void JumpToStart()
        {
            // 动作前先暂停
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            previewRunner.Seek(0f, state.SnapInterval);
            state.timeIndicator = 0f;
            state.isStopped = false;
        }

        /// <summary>
        /// 跳转到结束
        /// </summary>
        public void JumpToEnd()
        {
            // 动作前先暂停
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            float duration = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
            previewRunner.Seek(duration, state.SnapInterval);
            state.timeIndicator = duration;
            state.isStopped = false;
        }

        /// <summary>
        /// 预览 Seek（拖动时间指针时调用）
        /// </summary>
        public void SeekPreview(float time)
        {
            // 如果播放中先暂停
            if (IsPlaying) TogglePlay();
            if (previewRunner.CurrentState == SkillRunner.State.Idle)
            {
               // 如果是停止状态下拖动，激活 Process 但保持暂停
               Debug.Log("[SkillEditorWindow] SeekPreview: Runner Idle -> EnsureRunnerActive");
               EnsureRunnerActive();
            }

            Debug.Log($"[SkillEditorWindow] Seek -> {time}");
            previewRunner?.Seek(time, state.SnapInterval);
            state.timeIndicator = previewRunner != null ? previewRunner.CurrentTime : time;
            Debug.Log($"[SkillEditorWindow] SeekPreview finished. RunnerTime={previewRunner?.CurrentTime}, Indicator={state.timeIndicator}");
            // Seek 后确保不是 Stopped 状态，使红线可见
            state.isStopped = false;
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 预览更新（在 Update 中调用）
        /// 根据 TimeStepMode 决定 deltaTime
        /// </summary>
        private void UpdatePreview()
        {
            if (previewRunner == null) return;
            if (previewRunner.CurrentState != SkillRunner.State.Playing) return;

            double now = EditorApplication.timeSinceStartup;
            float realDelta = Mathf.Min((float)(now - lastPreviewTime), 0.1f);
            lastPreviewTime = now;

            if (state.timeStepMode == TimeStepMode.Fixed && state.frameRate > 0)
            {
                // Fixed 模式：累积真实时间，按固定步长消耗
                float fixedStep = 1f / state.frameRate;
                accumulator += realDelta*state.previewSpeedMultiplier; // 预览速度倍率影响累积时间

                // 防止卡顿后的无限追赶（限制每帧最多追赶 5 步）
                int maxSteps = 5;
                int steps = 0;
                while (accumulator >= fixedStep && steps < maxSteps)
                {
                    previewRunner.Tick(fixedStep);
                    accumulator -= fixedStep;
                    steps++;
                }

                // 如果累积时间仍然过多，丢弃以防快进
                if (accumulator >= fixedStep) accumulator = 0;
            }
            else
            {
                // Variable 模式：实时 delta
                previewRunner.Tick(realDelta);
                accumulator = 0;
            }

            // 同步 Runner 的时间到 state（供 UI 时间指示器显示）
            state.timeIndicator = previewRunner.CurrentTime;

            // 检查播放器是否在 Tick 之后由于到达末尾而变回 Idle 状态
            if (previewRunner.CurrentState == SkillRunner.State.Idle)
            {
                state.isStopped = true;
                state.timeIndicator = 0f;
                Repaint();
                SceneView.RepaintAll();
            }
        }
    }
}
