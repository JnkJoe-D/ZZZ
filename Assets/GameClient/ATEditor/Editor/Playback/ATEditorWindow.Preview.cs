using UnityEngine;
using UnityEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditorWindow 的预览扩展。
    /// 负责 SkillRunner 驱动的编辑器预览播放。
    /// </summary>
    public partial class ATEditorWindow
    {
        // 预览播放器
        private SkillRunner previewRunner;
        public SkillRunner PreviewRunner => previewRunner;
        private double lastPreviewTime;
        private double accumulator; // 时间累积器，用于 Fixed ģʽ
        public GameObject prevoewTarget => state != null ? state.previewTarget : null;
        /// <summary>
        /// 记录预览开始前角色原始位姿
        /// </summary>
        private void CapturePreviewOriginPose()
        {
            if (state == null) return;
            GameObject target = state.previewTarget;
            if (target == null) return;

            if (state.hasPreviewOriginPose && state.previewOriginTarget == target) return;

            state.previewOriginTarget = target;
            state.previewOriginPos = target.transform.position;
            state.previewOriginRot = target.transform.rotation;
            state.hasPreviewOriginPose = true;
        }

        /// <summary>
        /// 恢复预览开始前位姿
        /// </summary>
        private void RestorePreviewOriginPose()
        {
            if (state == null || !state.hasPreviewOriginPose) return;

            GameObject target = state.previewOriginTarget != null ? state.previewOriginTarget : state.previewTarget;
            if (target != null)
            {
                target.transform.position = state.previewOriginPos;
                target.transform.rotation = state.previewOriginRot;
            }

            state.hasPreviewOriginPose = false;
            state.previewOriginTarget = null;
        }

        /// <summary>
        /// 是否正在播放，供 Toolbar 使用。
        /// </summary>
        public bool IsPlaying => previewRunner != null && previewRunner.CurrentState == SkillRunner.State.Playing;
        public bool IsInPlayMode => previewRunner != null && (previewRunner.CurrentState != SkillRunner.State.None);
        /// <summary>
        /// 初始化预览系统（圀OnEnable 咀previewTarget 变更时调用）
        /// </summary>
        public void InitPreview()
        {
            if (previewRunner != null)
            {
                StopPreview();
            }

            previewRunner = new SkillRunner(PlayMode.EditorPreview);
            if (state != null)
            {
                state.previewRunner = previewRunner;
                if (state.previewTarget != null)
                {
                    var provider = ATEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
                    var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview, provider);
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
            EditorAnimationUtils.DisposeAll();
            previewRunner = null;
            EditorVFXManager.Instance.Dispose();
            
            // 删除打开时记录的初始预览目标
            if (state != null && state.initialAutoPreviewTarget != null)
            {
                Object.DestroyImmediate(state.initialAutoPreviewTarget);
                state.initialAutoPreviewTarget = null;
            }

            // 向外部层抛出主动销毁指令，清理那些跨程序集缓存的重对象
            ATEditorGlobalSettings.OnEditorDispose?.Invoke();
        }

        /// <summary>
        /// 开始预览播放        /// </summary>
        public void StartPreview(float progress = 0f)
        {
            if (state.currentTimeline == null) return;
            if (previewRunner == null)
            {
                InitPreview();
            }

            state.currentTimeline.RecalculateDuration();
            float duration = state.currentTimeline.Duration;
            float safeProgress = duration > Mathf.Epsilon ? Mathf.Clamp01(progress) : 0f;

            CapturePreviewOriginPose();
            EditorAnimationUtils.SetTimeline(state.previewTarget, state.currentTimeline);
            EditorAnimationUtils.SetSamplingMode(state.previewTarget, false);
            EditorAnimationUtils.ApplyTrackBasePose(state.previewTarget);

            var provider = ATEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
            var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview, provider);

            lastPreviewTime = EditorApplication.timeSinceStartup;
            accumulator = 0;
            previewRunner.Play(state.currentTimeline, ctx, safeProgress);
        }

        /// <summary>
        /// 停止预览播放
        /// </summary>
        public void StopPreview()
        {
            previewRunner?.Stop();
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.Dispose(state.previewTarget);
            }
            RestorePreviewOriginPose();
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
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, false);
            }
            previewRunner?.Resume();
            lastPreviewTime = EditorApplication.timeSinceStartup;
            accumulator = 0;
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
                // Fixed 模式：累积真实时间，按固定步长推进
                float fixedStep = 1f / state.frameRate;
                accumulator += realDelta * Mathf.Abs(state.previewSpeedMultiplier); // 预览速度倍率影响累积时间,但不受步进方向影哀
                //步进符号
                int stepSign = state.previewSpeedMultiplier >= 0 ? 1 : -1;
                // 防止卡顿后的无限追赶（限制每帧最多追赀5 步）
                int maxSteps = 5;
                int steps = 0;
                while (accumulator >= fixedStep && steps < maxSteps)
                {
                    previewRunner.Tick(fixedStep * stepSign);
                    accumulator -= fixedStep;
                    steps++;
                }

                // 如果累积时间仍然过多，则丢弃以避免追帧过量
                if (accumulator >= fixedStep) accumulator = 0;
            }
            else
            {
                // Variable 模式：实旀delta
                previewRunner.Tick(realDelta * state.previewSpeedMultiplier);
                accumulator = 0;
            }

            // 同步 Runner 的时间到 state（供 UI 时间指示器显示）
            state.timeIndicator = previewRunner.CurrentTime;

            // 检查播放器是否在 Tick 后因到达末尾而回到 Idle
            if (previewRunner.CurrentState == SkillRunner.State.None)
            {
                RestorePreviewOriginPose();
                state.isStopped = true;
                state.timeIndicator = 0f;
                Repaint();
                SceneView.RepaintAll();
            }
        }
        /// <summary>
        /// 预览 Seek（拖动时间指针时调用＀        /// </summary>
        public void SeekPreview(float time)
        {
            if (IsPlaying) PausePreview();
            if (previewRunner.CurrentState == SkillRunner.State.None)
            {
                // 如果是停止状态下拖动，激洀Process 但保持暂偀                EnsureRunnerActive();
            }

            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, true);
            }
            previewRunner?.Seek(time, state.SnapInterval);
            state.timeIndicator = previewRunner != null ? previewRunner.CurrentTime : time;
            state.isStopped = false;
            SceneView.RepaintAll();
        }
        /// <summary>
        /// 确保 Runner 处于活跃状态（Running or Paused锛?        /// 如果昀Idle，则自动开始并暂停，以便进血Seek 或步迀        /// </summary>
        private void EnsureRunnerActive()
        {
            if (previewRunner == null) InitPreview();
            if (previewRunner.CurrentState == SkillRunner.State.None)
            {
                StartPreview();
                PausePreview();
            }
        }

        /// <summary>
        /// 切换播放/暂停
        public void TogglePlay()
        {
            if (state?.currentTimeline == null)
            {
                return;
            }

            if (previewRunner == null)
            {
                InitPreview();
            }

            state.currentTimeline.RecalculateDuration();

            if (IsPlaying)
            {
                PausePreview();
                return;
            }

            if (previewRunner.CurrentState == SkillRunner.State.None || state.isStopped)
            {
                float duration = state.currentTimeline.Duration;
                float startPreviewTime;

                if (state.previewSpeedMultiplier >= 0f)
                {
                    startPreviewTime = state.timeIndicator >= duration ? 0f : state.timeIndicator;
                }
                else
                {
                    startPreviewTime = state.timeIndicator >= duration ? duration : state.timeIndicator;
                }

                float startProgress = duration > Mathf.Epsilon ? startPreviewTime / duration : 0f;
                StartPreview(startProgress);
            }
            else if (previewRunner.CurrentState == SkillRunner.State.Paused)
            {
                ResumePreview();
            }

            state.isStopped = false;
        }

        /// <summary>
        /// 停止播放并重罀        /// </summary>
        public void Stop()
        {
            StopPreview();
            state.isStopped = true;
            state.timeIndicator = 0f;
            accumulator = 0;
        }

        /// <summary>
        /// 单帧前进
        /// </summary>
        public void StepForward()
        {
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, true);
            }

            float dt = 1.0f / (state.frameRate > 0 ? state.frameRate : 30);
            float targetTime = previewRunner.CurrentTime + dt;
            float maxTime = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
            targetTime = Mathf.Clamp(targetTime, 0f, maxTime);

            previewRunner?.Seek(targetTime, state.SnapInterval);
            state.timeIndicator = targetTime;
            state.isStopped = false;
        }

        /// <summary>
        /// 单帧后退
        /// </summary>
        public void StepBackward()
        {
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, true);
            }

            float dt = 1.0f / (state.frameRate > 0 ? state.frameRate : 30);
            float targetTime = previewRunner.CurrentTime - dt;
            targetTime = Mathf.Max(0f, targetTime);

            previewRunner?.Seek(targetTime, state.SnapInterval);
            state.timeIndicator = targetTime;
            state.isStopped = false;

        }

        /// <summary>
        /// 跳转到开姀        /// </summary>
        public void JumpToStart()
        {
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, true);
            }
            previewRunner?.Seek(0f, state.SnapInterval);
            state.timeIndicator = 0f;
            state.isStopped = false;
        }

        /// <summary>
        /// 跳转到结杀        /// </summary>
        public void JumpToEnd()
        {
            if (IsPlaying) TogglePlay();

            EnsureRunnerActive();
            if (state != null && state.previewTarget != null)
            {
                EditorAnimationUtils.SetSamplingMode(state.previewTarget, true);
            }
            float duration = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
            previewRunner?.Seek(duration, state.SnapInterval);
            state.timeIndicator = duration;
            state.isStopped = false;
        }

        

        
    }
}
