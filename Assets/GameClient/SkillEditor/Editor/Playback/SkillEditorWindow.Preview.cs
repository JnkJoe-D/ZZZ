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
        /// 是否正在播放（供 Toolbar 使用）
        /// </summary>
        public bool IsPlaying => previewRunner != null && previewRunner.CurrentState == SkillRunner.State.Playing;
        public bool IsInPlayMode => previewRunner != null && (previewRunner.CurrentState != SkillRunner.State.Idle);
        /// <summary>
        /// 初始化预览系统（在 OnEnable 和 previewTarget 变更时调用）
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
                    var provider = SkillEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
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
            EditorAudioManager.Instance.Dispose();
            EditorVFXManager.Instance.Dispose();

            // 向外部层抛出主动销毁指令，清理那些跨程序集缓存的重对象
            SkillEditorGlobalSettings.OnEditorDispose?.Invoke();
        }

        /// <summary>
        /// 开始预览播放
        /// </summary>
        public void StartPreview(float progress = 0f)
        {
            if (state.currentTimeline == null) return;

            CapturePreviewOriginPose();
            EditorAnimationUtils.SetTimeline(state.previewTarget, state.currentTimeline);
            EditorAnimationUtils.SetSamplingMode(state.previewTarget, false);
            EditorAnimationUtils.ApplyTrackBasePose(state.previewTarget);

            var provider = SkillEditorGlobalSettings.DefaultServiceFactoryCreator?.Invoke(state.previewTarget);
            var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview, provider);

            lastPreviewTime = EditorApplication.timeSinceStartup;
            previewRunner.Play(state.currentTimeline, ctx, progress);
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
                // Fixed 模式：累积真实时间，按固定步长消耗
                float fixedStep = 1f / state.frameRate;
                accumulator += realDelta * Mathf.Abs(state.previewSpeedMultiplier); // 预览速度倍率影响累积时间,但不受步进方向影响

                //步进符号
                int stepSign = state.previewSpeedMultiplier >= 0 ? 1 : -1;
                // 防止卡顿后的无限追赶（限制每帧最多追赶 5 步）
                int maxSteps = 5;
                int steps = 0;
                while (accumulator >= fixedStep && steps < maxSteps)
                {
                    previewRunner.Tick(fixedStep * stepSign);
                    accumulator -= fixedStep;
                    steps++;
                }

                // 如果累积时间仍然过多，丢弃以防快进
                if (accumulator >= fixedStep) accumulator = 0;
            }
            else
            {
                // Variable 模式：实时 delta
                previewRunner.Tick(realDelta * state.previewSpeedMultiplier);
                accumulator = 0;
            }

            // 同步 Runner 的时间到 state（供 UI 时间指示器显示）
            state.timeIndicator = previewRunner.CurrentTime;

            // 检查播放器是否在 Tick 之后由于到达末尾而变回 Idle 状态
            if (previewRunner.CurrentState == SkillRunner.State.Idle)
            {
                RestorePreviewOriginPose();
                state.isStopped = true;
                state.timeIndicator = 0f;
                Repaint();
                SceneView.RepaintAll();
            }
        }
        /// <summary>
        /// 预览 Seek（拖动时间指针时调用）
        /// </summary>
        public void SeekPreview(float time)
        {
            if (IsPlaying) PausePreview();
            if (previewRunner.CurrentState == SkillRunner.State.Idle)
            {
                // 如果是停止状态下拖动，激活 Process 但保持暂停
                EnsureRunnerActive();
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
        /// 确保 Runner 处于活跃状态（Running or Paused）
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
            }
            else
            {
                // Stop -> Play
                if (previewRunner.CurrentState == SkillRunner.State.Idle || state.isStopped)
                {
                    float startPreviewTime = 0f;

                    float duration = state.currentTimeline != null ? state.currentTimeline.Duration : 0f;
                    // if (state.timeIndicator >= duration - 0.05f)
                    // {
                    //     state.timeIndicator = 0f;
                    // }
                    // 如果是正向播放，超过末尾重置；如果是反向播放，超过末尾保持在末尾
                    if (state.previewSpeedMultiplier>=0f)
                    {
                        state.timeIndicator= state.timeIndicator >= duration? 0f : state.timeIndicator;
                    }
                    else
                    {
                        state.timeIndicator = state.timeIndicator >= duration ? duration : state.timeIndicator;
                    }
                    startPreviewTime = state.timeIndicator;
                    // // 如果不是从零开始播放
                    // if (state.timeIndicator > 0)
                    // {
                    //     // previewRunner?.Seek(state.timeIndicator, state.SnapInterval);
                    // }
                    
                    // 传入相对播放起始点
                    StartPreview(startPreviewTime / duration); 

                    
                }
                // Pause -> Play
                else if(previewRunner.CurrentState == SkillRunner.State.Paused)
                {
                    ResumePreview();
                }
                
                //更新编辑器状态
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
        /// 跳转到开始
        /// </summary>
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
        /// 跳转到结束
        /// </summary>
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
