using System;
using System.Collections.Generic;

namespace ATEditor
{
    /// <summary>
    /// 动作播放器核心：驱动 Process 生命周期的状态机
    /// 纯 C# 类（不继承 MonoBehaviour），支持区间扫描、事件订阅、Seek、三层清理
    /// </summary>
    public sealed class ActionRunner
    {
        /// <summary>
        /// 播放状态
        /// </summary>
        public enum State
        {
            None,
            Playing,
            Paused
        }

        // ─── 公开状态 ───

        /// <summary>
        /// 当前播放状态
        /// </summary>
        public State CurrentState { get; private set; } = State.None;
        
        /// <summary>
        /// 获取当前所有 Process 实例（供编辑器查看状态）
        /// </summary>
        public IReadOnlyList<ProcessInstance> ActiveProcesses => processes;

        /// <summary>
        /// 当前播放时间（秒）
        /// </summary>
        public float CurrentTime { get; private set; }
        private float previousTime;

        /// <summary>
        /// 当前播放的时间轴
        /// </summary>
        public ActionTimeline Timeline { get; private set; }

        /// <summary>
        /// 动画完成/循环时的残差时间（当前时间 - 总时长），供切招时传递时间补偿
        /// </summary>
        public float OvershootTime { get; private set; }

        // ─── 事件 ───

        /// <summary>
        /// 播放开始时触发
        /// </summary>
        public event Action OnStart;

        /// <summary>
        /// 自然播放完毕或 Stop() 时触发
        /// </summary>
        public event Action OnComplete;

        /// <summary>
        /// 被新技能打断时触发（在旧技能清理后、新技能开始前）
        /// </summary>
        public event Action OnInterrupt;

        /// <summary>
        /// 暂停时触发
        /// </summary>
        public event Action OnPause;

        /// <summary>
        /// 恢复时触发
        /// </summary>
        public event Action OnResume;

        /// <summary>
        /// 循环播放一轮完成时触发
        /// </summary>
        public event Action OnLoopComplete;

        /// <summary>
        /// 每帧触发，参数为当前时间
        /// </summary>
        public event Action<float> OnTick;

        // ─── 内部 ───

        private PlayMode playMode;
        private ProcessContext context;
        public ProcessContext Context => context;
        private List<ProcessInstance> processes = new List<ProcessInstance>();
        private List<ProcessInstance> activeProcesses = new List<ProcessInstance>();
        
        private bool _isTicking = false;
        private float? _pendingSeekTime = null;
        private float _pendingSeekDelta = 0f;
        /// <summary>
        /// Process 实例与其运行状态的绑定
        /// </summary>
        /// <summary>
        /// Process 实例与其运行状态的绑定
        /// </summary>
        public struct ProcessInstance
        {
            public IProcess process;
            public ClipBase clip;
            public bool isActive;
        }

        public ActionRunner(PlayMode mode)
        {
            playMode = mode;
        }

        /// <summary>
        /// 预热 Context（供编辑器静态预览等非播放状态使用，clipsDrawer能静态访问context的自定义服务以实时预览部分效果）
        /// </summary>
        public void PrewarmContext(ProcessContext initialContext)
        {
            this.context = initialContext;
        }

        // ─── 播放控制 ───

        /// <summary>
        /// 开始播放（如正在播放或暂停则先打断旧技能）
        /// </summary>
        public void Play(ActionTimeline timeline, ProcessContext context,float startTime = 0f)
        {
            if (CurrentState != State.None)
            {
                InterruptInternal();
            }
            this.Timeline = timeline;
            this.context = context;
            this.context.IsInterrupted = false; // 重置打断状态
            this.context.SetSkillId(timeline.Id); // 注入技能标识
            CurrentTime = startTime < 0f ? 0f : (startTime > Timeline.Duration ? Timeline.Duration : startTime);
            CurrentState = State.Playing;

            BuildProcesses();

            foreach (var inst in processes)
            {
                inst.process.OnEnable();
            }

            this.context.ExecuteStartActionsOnce();

            DispatchTimelineEvents(Timeline.beforePlayEvents);
            if (CurrentState != State.Playing || Timeline != timeline)
            {
                return;
            }

            OnStart?.Invoke();

            Tick(0f); //保证开始时间为0的片段能第一时间进入
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (CurrentState != State.Playing) return;
            CurrentState = State.Paused;
            for(int i = 0; i < activeProcesses.Count; i++)
            {
                var inst = activeProcesses[i];
                inst.process.OnPause();
            }
            OnPause?.Invoke();
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (CurrentState != State.Paused) return;
            CurrentState = State.Playing;
            for (int i = 0; i < activeProcesses.Count; i++)
            {
                var inst = activeProcesses[i];
                inst.process.OnResume();
            }
            OnResume?.Invoke();
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            if (CurrentState == State.None) return;
            FullCleanup();
            CurrentState = State.None;
            ClearEvents();
        }

        /// <summary>
        /// 跳转到指定时间点（编辑器 Seek）
        /// 直接跳转：Exit 脱离区间的 Process，Enter 进入新区间的 Process
        /// </summary>
        public void Seek(float targetTime,float deltaTime)
        {
            if(Timeline == null || targetTime < 0f)
            {
                return;
            }
            
            if (_isTicking)
            {
                _pendingSeekTime = targetTime;
                _pendingSeekDelta = deltaTime;
                return;
            }
            if (targetTime > Timeline.Duration)
            {
                targetTime = Timeline.Duration;
            }
            CurrentTime = targetTime;
            if (context != null) context.CurrentTime = targetTime;

            UnityEngine.Debug.Log($"[SkillRunner] Seek called! targetTime={targetTime}, dt={deltaTime}, processes={processes.Count}");

            for (int i = 0; i < processes.Count; i++)
            {
                var inst = processes[i];
                bool willBeActive = targetTime >= inst.clip.StartTime
                                 && targetTime <= inst.clip.EndTime;

                if (inst.isActive && !willBeActive)
                {
                    inst.process.OnExit();
                    inst.isActive = false;
                    activeProcesses.Remove(inst);
                }
                else if (inst.isActive && willBeActive)
                {
                    inst.process.OnSeek(targetTime);
                }

                if (!inst.isActive && willBeActive)
                {
                    inst.process.OnEnter();
                    inst.isActive = true;
                    activeProcesses.Add(inst);
                }

                processes[i] = inst;
            }

            //刷新当前帧画面
            foreach (var inst in processes)
            {
                if (inst.isActive)
                {
                    inst.process.OnUpdate(CurrentTime, deltaTime); 
                }
            }

            context?.ExecuteTickActions(CurrentTime, deltaTime);
            context?.ExecuteLateTickActions(CurrentTime, deltaTime);
        }

        // ─── 每帧驱动 ───

        /// <summary>
        /// 每帧驱动（由外部调用）
        /// 编辑器预览：由 EditorApplication.update 传入 editorDelta 或 1/frameRate
        /// 运行时：由 SkillLifecycleManager.Update 传入 Time.deltaTime
        /// 帧同步：由逻辑帧回调传入 fixedDelta
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState != State.Playing) return;

            _isTicking = true;
            try
            {
                var actingTimeline = Timeline; // 保存当前正在运行的时间轴，以检测运行中是否切招

            float speed = context.GlobalPlaySpeed;
            CurrentTime += deltaTime * speed;
            context.CurrentTime = CurrentTime;
            bool isReversing = CurrentTime - previousTime < 0 && speed < 0;
            previousTime = CurrentTime;
            // 区间扫描
            for (int i = 0; i < processes.Count; i++)
            {
                var inst = processes[i];
                bool shouldBeActive =  CurrentTime >= inst.clip.StartTime && CurrentTime <= inst.clip.EndTime;
                // 进入区间
                if (shouldBeActive && !inst.isActive)
                {
                    inst.process.OnEnter();
                    // 如果 OnEnter() 中的事件触发了外界强切技能，则 Runner 会重建，当前 foreach 需要直接打断
                    if (this.Timeline != actingTimeline || this.CurrentState != State.Playing) return;
                    
                    inst.isActive = true;
                    activeProcesses.Add(inst);
                }

                // 区间内更新
                if (shouldBeActive && inst.isActive)
                {
                    inst.process.OnUpdate(CurrentTime, deltaTime);
                    if (this.Timeline != actingTimeline || this.CurrentState != State.Playing) return;
                }

                // 离开区间
                if (!shouldBeActive && inst.isActive)
                {
                    // 如果因为正向或反向步进刚好越界，在退出前强制做一次边界插值保证终态
                    if (CurrentTime >= inst.clip.EndTime)
                    {
                        inst.process.OnUpdate(inst.clip.EndTime, deltaTime);
                    }
                    else if (CurrentTime < inst.clip.StartTime)
                    {
                        inst.process.OnUpdate(inst.clip.StartTime, deltaTime);
                    }
                    inst.process.OnExit();
                    if (this.Timeline != actingTimeline || this.CurrentState != State.Playing) return;
                    
                    inst.isActive = false;
                    activeProcesses.Remove(inst);
                }

                processes[i] = inst;
            }

            context?.ExecuteTickActions(CurrentTime, deltaTime);
            context?.ExecuteLateTickActions(CurrentTime, deltaTime);

            OnTick?.Invoke(CurrentTime);

            // 播放结束检测
            if (Timeline != null && !_pendingSeekTime.HasValue && 
            ((!isReversing && CurrentTime >= Timeline.Duration)||(isReversing && CurrentTime <= 0f)))
            {
                OvershootTime = !isReversing ? CurrentTime - Timeline.Duration : CurrentTime;

                if (Timeline.isLoop)
                {
                    ResetActiveProcesses();
                    CurrentTime = !isReversing ? OvershootTime : Timeline.Duration + OvershootTime;
                    context.CurrentTime = CurrentTime;
                    OnLoopComplete?.Invoke();

                    // 防止 Timeline.Duration 为 0 导致的无限死循环
                    if (Timeline.Duration > 0.001f)
                    {
                        Tick(0f); // 结束时间作为下轮循环的开始时间，需保证开始时间为0的片段能第一时间进入
                    }
                }
                else
                {
                    ActionTimeline completedTimeline = Timeline;
                    FullCleanup();
                    CurrentState = State.None;
                    DispatchTimelineEvents(completedTimeline?.onCompleteEvents);
                    if (CurrentState != State.None || Timeline != completedTimeline)
                    {
                        return;
                    }

                    OnComplete?.Invoke();
                }
            }
            }
            finally
            {
                _isTicking = false;
            }
            
            if (_pendingSeekTime.HasValue)
            {
                float target = _pendingSeekTime.Value;
                float dt = _pendingSeekDelta;
                _pendingSeekTime = null;
                Seek(target, dt);
            }
        }

        // ─── 私有方法 ───

        /// <summary>
        /// 内部打断：触发事件 → 清理 → 重置
        /// </summary>
        private void InterruptInternal()
        {
            if (context != null)
            {
                context.IsInterrupted = true;
            }
            OnInterrupt?.Invoke();
            FullCleanup();
            ClearEvents();
            CurrentState = State.None;
        }

        /// <summary>
        /// 为当前 Timeline 中所有启用的 Clip 创建 Process
        /// </summary>
        private void BuildProcesses()
        {
            processes.Clear();

            if (Timeline == null) return;

            foreach (var track in Timeline.AllTracks)
            {
                if (!track.isEnabled || !track.CanPlay) continue;

                foreach (var clip in track.clips)
                {
                    if (!clip.isEnabled) continue;

                    var process = ProcessFactory.Create(clip, playMode);
                    if (process == null) continue;

                    process.Initialize(clip, context);
                    processes.Add(new ProcessInstance
                    {
                        process = process,
                        clip = clip,
                        isActive = false
                    });
                }
            }
        }

        /// <summary>
        /// 完整清理（三层 + 池归还）
        /// 级别 1: OnExit（实例级）→ 级别 2: OnDisable（进程级）→ 归还池 → 级别 3: SystemCleanup
        /// </summary>
        private void FullCleanup()
        {
            // 级别 1: 实例级清理 (仅在自然退出时触发，此处为硬清理，直接跳过 OnExit)
            // foreach (var inst in processes)
            // {
            //     if (inst.isActive)
            //     {
            //         inst.process.OnExit();
            //     }
            // }

            // 级别 2: 进程级清理
            foreach (var inst in processes)
            {
                inst.process.OnDisable();
            }

            // 归还对象池
            foreach (var inst in processes)
            {
                ProcessFactory.Return(inst.process);
            }

            processes.Clear();
            activeProcesses.Clear();
            // 级别 3: 系统级清理
            context?.ExecuteCleanups();
        }

        /// <summary>
        /// 重置所有活跃 Process（循环播放时调用）
        /// </summary>
        private void ResetActiveProcesses()
        {
            for (int i = 0; i < processes.Count; i++)
            {
                var inst = processes[i];
                if (inst.isActive)
                {
                    inst.process.OnExit();
                    inst.isActive = false;
                    processes[i] = inst;
                }
            }
        }

        private void DispatchTimelineEvents(IReadOnlyList<ActionTimelineEvent> timelineEvents)
        {
            if (timelineEvents == null || timelineEvents.Count == 0 || context == null)
            {
                return;
            }

            IEventHandler eventHandler = context.GetService<IEventHandler>();
            if (eventHandler == null)
            {
                return;
            }

            for (int i = 0; i < timelineEvents.Count; i++)
            {
                ActionTimelineEvent timelineEvent = timelineEvents[i];
                if (timelineEvent == null || string.IsNullOrEmpty(timelineEvent.eventName))
                {
                    continue;
                }

                eventHandler.OnActionTimelineEvent(timelineEvent.eventName, timelineEvent.parameters);
                if (CurrentState != State.Playing || Timeline == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 清除所有事件订阅（Interrupt / Stop 后调用）
        /// </summary>
        private void ClearEvents()
        {
            OnStart = null;
            OnComplete = null;
            OnInterrupt = null;
            OnPause = null;
            OnResume = null;
            OnLoopComplete = null;
            OnTick = null;
        }
    }
}
