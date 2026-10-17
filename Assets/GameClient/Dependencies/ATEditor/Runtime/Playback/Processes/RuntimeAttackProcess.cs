using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(AttackClip), PlayMode.Runtime)]
    public sealed class RuntimeAttackProcess : ProcessBase<AttackClip>, IProcessTickBoundary
    {
        private AttackSamplingSession _session;
        private string _tickKey;
        private bool _enterPending;
        public float NextBoundary => _session?.NextBoundary ?? float.PositiveInfinity;
        public override void OnEnable()
        {
            _tickKey = "AttackSample:" + Guid.NewGuid();
            context.RegisterLateTickAction(_tickKey, SampleAfterPose);
        }
        public override void OnEnter()
        {
            OnStop();
            _enterPending = true;
        }
        // 先让所有运动/动画 Process 更新姿态，再统一采样，避免依赖轨道排列顺序。
        public override void OnUpdate(float currentTime, float deltaTime) { }
        private void SampleAfterPose(float time, float deltaTime)
        {
            if (!_enterPending)
            {
                if (deltaTime > 0f) _session?.Update(time);
                return;
            }
            _enterPending = false;
            var handler = context.GetService<IAttackObservationHandler>();
            _session = new AttackSamplingSession(context, clip, observations =>
            {
                for (int i = 0; i < observations.Count && !context.IsInterrupted; i++)
                    handler?.OnObserved(observations[i]);
            });
            _session.Enter();
        }
        public override void OnSeek(float targetTime) => _session?.SkipTo(targetTime);
        public override void OnExit() => OnStop();
        public override void OnStop() { _session?.Dispose(); _session = null; _enterPending = false; }
        public override void OnDisable()
        {
            if (_tickKey != null) context?.UnregisterLateTickAction(_tickKey);
            _tickKey = null; OnStop();
        }
        public override void Reset() { OnDisable(); base.Reset(); }
    }

    /// <summary>不回填历史物理姿态。Director 在 NextBoundary 分段；其他宿主跳过的轮次不会补打。</summary>
    internal sealed class AttackSamplingSession : IDisposable
    {
        private readonly ProcessContext _context;
        private readonly AttackClip _clip;
        private readonly Action<IReadOnlyList<AttackObservation>> _publish;
        private IAttackDetectionWindow _window;
        private int _next;
        private readonly int _count;
        public float NextBoundary => _window != null && _next < _count
            ? _clip.StartTime + _next * (_clip.Duration / _count) : float.PositiveInfinity;

        public AttackSamplingSession(ProcessContext context, AttackClip clip, Action<IReadOnlyList<AttackObservation>> publish)
        {
            _context = context; _clip = clip; _publish = publish;
            _count = Mathf.Max(1, clip.sampleCount);
            _window = context.GetService<IAttackDetectionHandler>()?.OpenWindow(context, clip);
            if (_window == null) ATLog.Warning("[Attack] 缺少 IAttackDetectionHandler，当前窗口不执行检测。");
        }

        public void Enter()
        {
            if (_context.IsSeeking) SkipTo(_context.CurrentTime);
            else Update(_context.CurrentTime);
        }

        public void SkipTo(float time)
        {
            // 向后 Seek 不重置去重/预算，也不在已播放的窗口重放旧采样。
            _next = Mathf.Max(_next, IndexAt(time) + 1);
        }

        private int IndexAt(float time) => Mathf.FloorToInt((time - _clip.StartTime) / (_clip.Duration / _count) + 0.00001f);

        public void Update(float time)
        {
            if (_window == null || _context.IsInterrupted || _context.IsSeeking || _next >= _count) return;
            int index = IndexAt(time);
            if (index < _next || index < 0 || time >= _clip.EndTime) return;
            // 先更新游标，再调用消费者，避免观察回调重入导致重复采样。
            _next = index + 1;
            var results = _window.Sample(time, index);
            if (results.Count > 0) _publish(results);
        }

        public void Dispose() { _window?.Dispose(); _window = null; }
    }
}
