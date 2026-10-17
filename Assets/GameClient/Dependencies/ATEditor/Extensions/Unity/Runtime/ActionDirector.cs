using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 独立时间轴播放宿主。默认后端独占本对象的 MAnim 图；外部注入服务由调用方管理。
    /// 动作选择和 ZZZ 战斗规则由更高层负责。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ATEditor/Action Director")]
    public sealed class ActionDirector : MonoBehaviour, IActionRunnerProvider, IAttackObservationHandler
    {
        [SerializeField] private ActionTimeline _timeline;
        [SerializeField] private bool _playOnEnable;
        [SerializeField] private bool _updateAutomatically = true;
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField, Min(0f)] private float _speed = 1f;
        [SerializeField] private bool _applyRootMotion = true;

        private ActionRunner _runner;
        private ProcessContext _context;
        private DirectorDefaultServices _defaults;
        private ActionObjectPool _pool;
        private int _generation;
        private bool _isDriving;
        private bool _isReleasing;
        private bool _stopRequested;
        private ActionTimeline _pendingCompletion;
        private float _presentationTimeScale = 1f;

        public ActionTimeline Timeline { get => _timeline; set => _timeline = value; }
        public bool UpdateAutomatically { get => _updateAutomatically; set => _updateAutomatically = value; }
        public bool IsPlaying => _runner != null && _runner.CurrentState == ActionRunner.State.Playing;
        public bool IsPaused => _runner != null && _runner.CurrentState == ActionRunner.State.Paused;
        public float CurrentTime => _runner?.CurrentTime ?? 0f;
        public bool ApplyRootMotion { get => _applyRootMotion; set => _applyRootMotion = value; }
        /// <summary>传入逻辑 dt 所属时间域相对真实秒的速率，不再次乘到逻辑 Tick。无需 Tick 即可同步暂停/慢动作。</summary>
        public float PresentationTimeScale
        {
            get => _presentationTimeScale;
            set
            {
                ValidateDelta(value);
                _presentationTimeScale = value;
                _defaults?.SetPlayback(value * _speed, IsPaused);
            }
        }
        public float Speed
        {
            get => _speed;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _speed = value;
                _defaults?.SetPlayback(_presentationTimeScale * _speed, IsPaused);
            }
        }

        /// <summary>播放前设置；返回 null 时采用默认服务。注入对象的更新与释放由调用方负责。</summary>
        public Func<Type, GameObject, object> ServiceProvider { get; set; }
        public event Action<ActionTimeline> Started;
        public event Action<ActionTimeline> Completed;
        public event Action<ActionTimeline> Interrupted;
        public event Action<AttackObservation> AttackObserved;

        void IAttackObservationHandler.OnObserved(AttackObservation observation) => AttackObserved?.Invoke(observation);

        private void OnEnable()
        {
            if (_playOnEnable && _timeline != null) Play(_timeline);
        }

        private void OnDisable() => Shutdown();
        private void OnDestroy() => Shutdown();

        private void Shutdown()
        {
            _pool?.BeginShutdown();
            try { Stop(); }
            finally { _pool?.Dispose(); _pool = null; }
        }

        private ActionObjectPool GetPool() => _pool ??= new ActionObjectPool(gameObject);

        private void Update()
        {
            if (_updateAutomatically)
            {
                PresentationTimeScale = _useUnscaledTime ? 1f : Time.timeScale;
                Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }

        /// <summary>表现层更新留空，由动画与视听系统自驱动。</summary>
        public void UpdatePresentation(float realDeltaTime)
        {
            ValidateDelta(realDeltaTime);
        }
        private static void ValidateDelta(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value));
        }

        public bool Play() => Play(_timeline);

        /// <summary>生命周期通知中可切换播放；片段执行或资源清理期间返回 false，避免嵌套修改运行容器。</summary>
        public bool Play(ActionTimeline timeline, float startTime = 0f)
        {
            using (var prepared = Prepare(timeline, startTime))
                return prepared != null && prepared.Start();
        }

        /// <summary>准备期间允许创建后端，但不执行开始事件/片段。调用方提交业务后显式 Start；未开始的句柄 Dispose 会取消。</summary>
        public PreparedPlayback Prepare(ActionTimeline timeline, float startTime = 0f, object metadata = null)
        {
            if (timeline == null || !isActiveAndEnabled || _isDriving || _isReleasing) return null;
            if (float.IsNaN(startTime) || float.IsInfinity(startTime))
                throw new ArgumentOutOfRangeException(nameof(startTime));

            int generation = unchecked(++_generation);
            ReleaseCurrent(interrupted: true);
            // 旧播放结束的回调可以发起新播放，外层请求不再覆盖它。
            if (_generation != generation || !isActiveAndEnabled) return null;

            var runner = new ActionRunner(PlayMode.Runtime);
            var defaults = new DirectorDefaultServices(this, GetPool);
            defaults.SetPlayback(_presentationTimeScale * _speed, false);
            var provider = ServiceProvider;
            var context = new ProcessContext(gameObject, PlayMode.Runtime,
                (type, owner) => provider?.Invoke(type, owner) ?? defaults.GetService(type));
            context.UserData = this;
            context.PlaybackMetadata = metadata;
            defaults.Context = context;
            // 后端接收真实秒到动作秒的速率；逻辑采样后端接收片段局部速度，避免重复缩放。
            context.SetPresentationPlayback(_presentationTimeScale * _speed, false);
            _timeline = timeline;
            _runner = runner;
            _context = context;
            _defaults = defaults;
            runner.OnComplete += () => Finish(runner, timeline);

            try
            {
                Drive(() => runner.Prepare(timeline, context, startTime));
            }
            catch
            {
                if (_runner == runner) ReleaseCurrent(interrupted: false);
                throw;
            }

            return _runner == runner ? new PreparedPlayback(this, runner, context, timeline) : null;
        }

        public sealed class PreparedPlayback : IDisposable
        {
            private readonly ActionDirector _owner;
            private readonly ActionRunner _runner;
            private readonly ActionTimeline _timeline;
            public ProcessContext Context { get; }
            public long PlaybackId => Context.PlaybackId;
            public bool HasStarted { get; private set; }
            public bool IsReady => _owner != null && _owner._runner == _runner && _runner.CurrentState == ActionRunner.State.Prepared;
            internal PreparedPlayback(ActionDirector owner, ActionRunner runner, ProcessContext context, ActionTimeline timeline)
            { _owner = owner; _runner = runner; Context = context; _timeline = timeline; }
            public bool Start()
            {
                if (!IsReady || _owner._isDriving || _owner._isReleasing) return false;
                HasStarted = true;
                try { _owner.Drive(_runner.BeginPrepared); }
                catch
                {
                    if (_owner._runner == _runner) _owner.ReleaseCurrent(false);
                    throw;
                }
                if (_owner._runner != _runner) return false;
                _owner.Started?.Invoke(_timeline);
                return _owner._runner == _runner;
            }
            public void Dispose() { if (IsReady) _owner.Stop(); }
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsPlaying || _isDriving || _isReleasing) return;
            var runner = _runner;
            var defaults = _defaults;
            float scaledDelta = deltaTime * _speed;
            if (float.IsInfinity(scaledDelta)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            // 分发本帧新进入的片段并推进时间轴。
            _isDriving = true;
            try
            {
                defaults?.SetPlayback(_presentationTimeScale * _speed, false);
                float remaining = scaledDelta;
                do
                {
                    float step = NextStep(runner, remaining);
                    if (step > 0f && remaining - step == remaining)
                        throw new ArgumentOutOfRangeException(nameof(deltaTime), "步长过大，无法在浮点精度内分段推进。");
                    runner.Tick(step);
                    remaining -= step;
                } while (remaining > 0f && _runner == runner && runner.CurrentState == ActionRunner.State.Playing && !_stopRequested);
            }
            finally { EndDrive(); }
        }

        public void Pause()
        {
            if (_isDriving || _isReleasing) return;
            Drive(() => _runner?.Pause());
            _defaults?.SetPlayback(_presentationTimeScale * _speed, IsPaused);
        }
        public void Resume()
        {
            if (_isDriving || _isReleasing) return;
            Drive(() => _runner?.Resume());
            _defaults?.SetPlayback(_presentationTimeScale * _speed, IsPaused);
        }

        public void Seek(float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time) || time < 0f)
                throw new ArgumentOutOfRangeException(nameof(time));
            var runner = _runner;
            if (_isDriving || _isReleasing) return;
            var context = _context;
            if (context != null) context.IsSeeking = true;
            try { Drive(() => runner?.Seek(time, 0f)); }
            finally
            {
                if (context != null) context.IsSeeking = false;
            }
        }

        private static float NextStep(ActionRunner runner, float remaining)
        {
            float step = remaining;
            float current = runner.CurrentTime;
            float end = runner.Timeline.Duration - current;
            if (end > 0f) step = Mathf.Min(step, end);
            var processes = runner.ActiveProcesses;
            for (int i = 0; i < processes.Count; i++)
            {
                var clip = processes[i].clip;
                float startDistance = clip.StartTime - current;
                float endDistance = clip.EndTime - current;
                if (startDistance > 0f) step = Mathf.Min(step, startDistance);
                if (endDistance > 0f) step = Mathf.Min(step, endDistance);
                if (processes[i].process is IProcessTickBoundary boundary)
                {
                    float sampleDistance = boundary.NextBoundary - current;
                    if (sampleDistance > 0f) step = Mathf.Min(step, sampleDistance);
                }
            }
            return step;
        }

        public void Stop()
        {
            if (_isDriving)
            {
                _stopRequested = true;
                if (_context != null) _context.IsInterrupted = true;
                return;
            }
            unchecked { ++_generation; }
            ReleaseCurrent(interrupted: true);
        }

        private void Finish(ActionRunner runner, ActionTimeline timeline)
        {
            if (_runner != runner) return;
            ReleaseCurrent(interrupted: false);
            if (_isDriving) _pendingCompletion = timeline;
            else Completed?.Invoke(timeline);
        }

        private void Drive(Action operation)
        {
            _isDriving = true;
            try { operation(); }
            finally { EndDrive(); }
        }

        private void EndDrive()
        {
            _isDriving = false;
            var completed = _pendingCompletion;
            _pendingCompletion = null;
            if (_stopRequested)
            {
                _stopRequested = false;
                Stop();
            }
            if (completed != null) Completed?.Invoke(completed);
        }

        private void ReleaseCurrent(bool interrupted)
        {
            var runner = _runner;
            if (runner == null) return;
            var context = _context;
            var defaults = _defaults;
            var timeline = runner.Timeline;
            _runner = null;
            _context = null;
            _defaults = null;
            _isReleasing = true;
            try
            {
                runner.Stop();
            }
            finally
            {
                try
                {
                    try { context.Clear(); }
                    finally { defaults.Dispose(); }
                }
                finally { _isReleasing = false; }
            }
            if (interrupted) Interrupted?.Invoke(timeline);
        }

        public ActionRunner GetRunner() => _runner;
    }
}
