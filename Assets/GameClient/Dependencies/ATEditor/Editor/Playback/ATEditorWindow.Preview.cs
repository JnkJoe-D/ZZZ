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
        private ActionRunner previewRunner;
        public ActionRunner PreviewRunner => previewRunner;
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
        public bool IsPlaying => previewRunner != null && previewRunner.CurrentState == ActionRunner.State.Playing;
        public bool IsInPlayMode => previewRunner != null && (previewRunner.CurrentState != ActionRunner.State.None);
        /// <summary>
        /// 初始化预览系统（在 OnEnable 及 previewTarget 变更时调用）
        /// </summary>
        public void InitPreview()
        {
            if (previewRunner != null)
            {
                StopPreview();
            }

            previewRunner = new ActionRunner(PlayMode.EditorPreview);
            if (state != null)
            {
                state.previewRunner = previewRunner;
                if (state.previewTarget != null)
                {
                    var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview);
                    previewRunner.PrewarmContext(ctx);
                }
            }
        }

        /// <summary>
        /// 彻底清理场景中所有由 ATEditor 创建的预览对象实例（包括游离/非活动场景/隐藏在内存中的实例）
        /// </summary>
        public void DestroyAllPreviewTargets()
        {
            if (state != null)
            {
                if (state.previewTarget != null)
                {
                    Object.DestroyImmediate(state.previewTarget);
                    state.previewTarget = null;
                }
                state.initialAutoPreviewTarget = null;
                state.hasPreviewOriginPose = false;
                state.previewOriginTarget = null;
            }

            // 全局强力清理：扫描内存中所有非持久化的预览对象（无论在哪个场景或是否已脱离场景）
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allObjects)
            {
                if (go != null && !EditorUtility.IsPersistent(go) && go.name.StartsWith("[ATEditor_Preview]_"))
                {
                    Object.DestroyImmediate(go);
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
            
            // 彻底销毁场景中的所有预览实例
            DestroyAllPreviewTargets();

            // 向外部层抛出主动销毁指令，清理那些跨程序集缓存的重对象
            ATEditorGlobalSettings.OnEditorDispose?.Invoke();
        }

        /// <summary>
        /// 确保当前工作区的预览对象已生成在场景中（T-Pose）
        /// </summary>
        public void EnsureWorkspacePreviewTarget(bool forceRecreate = false)
        {
            if (state == null) return;

            var ws = state.ActiveWorkspace;
            if (ws == null)
            {
                DestroyAllPreviewTargets();
                return;
            }

            string expectedName = $"[ATEditor_Preview]_{ws.Id}";

            // 如果需要强制重新创建（例如修改了 Prefab 或坐标旋转配置）
            if (forceRecreate)
            {
                DestroyAllPreviewTargets();
            }

            // 检查当前已有引用是否合法
            if (state.previewTarget != null)
            {
                if (state.previewTarget.name == expectedName)
                {
                    // 已是匹配的预览对象，预热上下文
                    if (previewRunner != null)
                    {
                        var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview);
                        previewRunner.PrewarmContext(ctx);
                    }
                    return;
                }
                else
                {
                    // 引用的是旧工作区对象，清理之
                    DestroyAllPreviewTargets();
                }
            }

            // 检查场景中是否已存在同名对象
            GameObject existing = GameObject.Find(expectedName);
            if (existing != null)
            {
                state.previewTarget = existing;
                state.initialAutoPreviewTarget = existing;
                if (previewRunner != null)
                {
                    var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview);
                    previewRunner.PrewarmContext(ctx);
                }
                return;
            }

            // 若场景中没有，且工作区配置了 Prefab，则实例化新对象
            if (ws.PreviewPrefab != null)
            {
                // 先清理可能残留在场景中的其他预览对象
                DestroyAllPreviewTargets();

                GameObject instance = Object.Instantiate(ws.PreviewPrefab, ws.SpawnPosition, Quaternion.Euler(ws.SpawnRotationEuler));
                instance.name = expectedName;
                instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                state.previewTarget = instance;
                state.initialAutoPreviewTarget = instance;

                if (previewRunner != null)
                {
                    var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview);
                    previewRunner.PrewarmContext(ctx);
                }
                SceneView.RepaintAll();
            }
            else
            {
                // 工作区未配置 Prefab，清理预览引用
                state.previewTarget = null;
                state.initialAutoPreviewTarget = null;
            }
        }

        /// <summary>
        /// 切换角色工作区（毫秒级热替换，确定性生命周期闭环）
        /// </summary>
        public void SwitchWorkspace(string newWorkspaceId, bool promptSave = true)
        {
            if (state == null) return;

            var db = ATEditorWorkspaceDatabase.Instance;
            var targetWs = db.GetWorkspaceById(newWorkspaceId);
            if (targetWs == null) return;

            // 1. 未保存修改提示与校验
            if (promptSave && state.currentTimeline != null && EditorUtility.IsDirty(state.currentTimeline))
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "保存当前动作",
                    $"动作 '{state.currentTimeline.name}' 存在未保存的修改，是否在切换工作区前保存？",
                    "保存",
                    "不保存",
                    "取消切换");

                if (choice == 0) // 保存
                {
                    toolbarView?.SaveCurrentTimeline();
                }
                else if (choice == 2) // 取消切换
                {
                    return;
                }
            }

            // 2. 彻底停止播放并关闭/卸载当前动作资产
            Stop();
            state.isStopped = true;
            state.timeIndicator = 0f;
            state.currentFilePath = null;
            ResetToBlankTimeline();

            // 3. 更新工作区 ID
            state.ActiveWorkspaceId = newWorkspaceId;

            // 4. 清理旧实例与动效缓存
            EditorAnimationUtils.DisposeAll();
            DestroyAllPreviewTargets();

            // 5. 实例化新工作区的 PreviewPrefab (默认 T-Pose)
            EnsureWorkspacePreviewTarget(forceRecreate: true);

            // 6. 重建预览系统与上下文
            InitPreview();
            Repaint();
            SceneView.RepaintAll();
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

            var ctx = new ProcessContext(state.previewTarget, PlayMode.EditorPreview);

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
            if (previewRunner.CurrentState != ActionRunner.State.Playing) return;

            double now = EditorApplication.timeSinceStartup;
            float realDelta = Mathf.Min((float)(now - lastPreviewTime), 0.1f);
            lastPreviewTime = now;

            if (state.timeStepMode == TimeStepMode.Fixed && state.frameRate > 0)
            {
                // Fixed 模式：累积真实时间，按固定步长推进
                float fixedStep = 1f / state.frameRate;
                accumulator += realDelta * Mathf.Abs(state.previewSpeedMultiplier); // 预览速度倍率影响累积时间，但不受步进方向影响
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
            if (previewRunner.CurrentState == ActionRunner.State.None)
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
            if (previewRunner == null || previewRunner.CurrentState == ActionRunner.State.None)
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
        /// 确保 Runner 处于活跃状态（Running 或 Paused）。
        /// 如果处于 Idle，则自动开始并暂停，以便进行 Seek 或单帧步进。
        /// </summary>
        private void EnsureRunnerActive()
        {
            if (previewRunner == null) InitPreview();
            if (previewRunner.CurrentState == ActionRunner.State.None)
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

            if (previewRunner.CurrentState == ActionRunner.State.None || state.isStopped)
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
            else if (previewRunner.CurrentState == ActionRunner.State.Paused)
            {
                ResumePreview();
            }

            state.isStopped = false;
        }

        /// <summary>
        /// 停止播放并重置。
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
        /// 跳转到开头。
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
        /// 跳转到结尾。
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
