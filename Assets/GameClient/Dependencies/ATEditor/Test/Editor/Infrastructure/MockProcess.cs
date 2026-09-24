using System;
using System.Collections.Generic;

namespace ATEditor.Test
{
    /// <summary>
    /// 生命周期调用快照记录
    /// </summary>
    public struct LifecycleRecord
    {
        public string MethodName;
        public float Time;
        public float DeltaTime;
        public bool ContextIsInterrupted;

        public override string ToString()
        {
            return $"[{MethodName}] Time={Time:F3}, Dt={DeltaTime:F3}, IsInterrupted={ContextIsInterrupted}";
        }
    }

    /// <summary>
    /// 具备全生命周期调用追踪记账功能的测试进程
    /// 严格覆盖 IProcess 定义的所有 10 个生命周期钩子
    /// </summary>
    [ProcessBinding(typeof(MockClip), PlayMode.Runtime)]
    [ProcessBinding(typeof(MockClip), PlayMode.EditorPreview)]
    public class MockProcess : ProcessBase<MockClip>
    {
        public readonly List<LifecycleRecord> History = new List<LifecycleRecord>();

        // 各生命周期方法的调用计数器
        public int InitializeCount { get; private set; }
        public int ResetCount { get; private set; }
        public int OnEnableCount { get; private set; }
        public int OnEnterCount { get; private set; }
        public int OnUpdateCount { get; private set; }
        public int OnExitCount { get; private set; }
        public int OnStopCount { get; private set; }
        public int OnDisableCount { get; private set; }
        public int OnPauseCount { get; private set; }
        public int OnResumeCount { get; private set; }
        public int OnSeekCount { get; private set; }

        public float LastUpdateTime { get; private set; }
        public float LastSeekTime { get; private set; }

        // 回调钩子，供测试注入特定的抢占行为（如在 OnEnter/OnUpdate/OnExit 中切招）
        public Action<MockProcess> CustomOnEnterAction;
        public Action<MockProcess, float, float> CustomOnUpdateAction;
        public Action<MockProcess> CustomOnExitAction;
        public Action<MockProcess> CustomOnStopAction;
        public Action<MockProcess> CustomOnDisableAction;

        public override void Initialize(ClipBase clipData, ProcessContext context)
        {
            // 绑定新片段开启新一轮生命周期：清零所有单周期方法计数，重置历史
            InitializeCount = 1;
            OnEnableCount = 0;
            OnEnterCount = 0;
            OnUpdateCount = 0;
            OnExitCount = 0;
            OnStopCount = 0;
            OnDisableCount = 0;
            OnPauseCount = 0;
            OnResumeCount = 0;
            OnSeekCount = 0;
            LastUpdateTime = 0f;
            LastSeekTime = 0f;
            History.Clear();

            History.Add(new LifecycleRecord
            {
                MethodName = nameof(Initialize),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            base.Initialize(clipData, context);
        }

        public override void Reset()
        {
            ResetCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(Reset),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });

            // 清理子类外部引用与委托，切断强引用防内存泄漏
            CustomOnEnterAction = null;
            CustomOnUpdateAction = null;
            CustomOnExitAction = null;
            CustomOnStopAction = null;
            CustomOnDisableAction = null;
            LastUpdateTime = 0f;
            LastSeekTime = 0f;

            base.Reset();
        }

        public override void OnEnable()
        {
            OnEnableCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnEnable),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            base.OnEnable();
        }

        public override void OnEnter()
        {
            OnEnterCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnEnter),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            CustomOnEnterAction?.Invoke(this);
            base.OnEnter();
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            OnUpdateCount++;
            LastUpdateTime = currentTime;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnUpdate),
                Time = currentTime,
                DeltaTime = deltaTime,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            CustomOnUpdateAction?.Invoke(this, currentTime, deltaTime);
        }

        public override void OnExit()
        {
            OnExitCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnExit),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            CustomOnExitAction?.Invoke(this);
            base.OnExit();
        }

        public override void OnStop()
        {
            OnStopCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnStop),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            CustomOnStopAction?.Invoke(this);
            base.OnStop();
        }

        public override void OnDisable()
        {
            OnDisableCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnDisable),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            CustomOnDisableAction?.Invoke(this);
            base.OnDisable();
        }

        public override void OnPause()
        {
            OnPauseCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnPause),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            base.OnPause();
        }

        public override void OnResume()
        {
            OnResumeCount++;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnResume),
                Time = context?.CurrentTime ?? 0f,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            base.OnResume();
        }

        public override void OnSeek(float targetTime)
        {
            OnSeekCount++;
            LastSeekTime = targetTime;
            History.Add(new LifecycleRecord
            {
                MethodName = nameof(OnSeek),
                Time = targetTime,
                DeltaTime = 0f,
                ContextIsInterrupted = context?.IsInterrupted ?? false
            });
            base.OnSeek(targetTime);
        }

        /// <summary>
        /// 清空历史记录（测试用）
        /// </summary>
        public void ClearHistory()
        {
            History.Clear();
            InitializeCount = 0;
            ResetCount = 0;
            OnEnableCount = 0;
            OnEnterCount = 0;
            OnUpdateCount = 0;
            OnExitCount = 0;
            OnStopCount = 0;
            OnPauseCount = 0;
            OnResumeCount = 0;
            OnSeekCount = 0;
        }
    }
}
