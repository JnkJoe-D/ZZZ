using System;
using System.Collections.Generic;
using MAnimSystem;
using UnityEngine;

namespace ATEditor
{
    /// <summary>默认动画后端（基于 MAnimSystem）；可由独立宿主接管图的时间与释放。</summary>
    public class DefaultAnimHandler : IAnimationHandler, IDisposable
    {
        private readonly MAnimSystem.AnimComponent _target;
        private readonly bool _manuallyDriven;
        private readonly bool _ownsGraph;
        private bool _initialized;
        private bool _previousManualMode;
        private readonly Dictionary<int, float> _layerSpeeds = new Dictionary<int, float>();
        private readonly AnimationGraphClock _clock = new AnimationGraphClock();
        private float _scale = 1f;
        private bool _paused;
        private double _logicTime, _presentationTime;

        public DefaultAnimHandler(MAnimSystem.AnimComponent target, bool manuallyDriven = false, bool ownsGraph = false)
        {
            _target = target;
            _manuallyDriven = manuallyDriven;
            _ownsGraph = ownsGraph;
        }

        public void Initialize()
        {
            if (_initialized || _target == null) return;
            _target.Initialize();
            _previousManualMode = _target.IsManuallyDriven;
            if (_manuallyDriven) _target.SetManuallyDriven(true);
            _target.InitializeGraph();
            _initialized = true;
            if (_manuallyDriven) _clock.Capture(_target.Graph);
        }

        public void PlayAnimation(UnityEngine.AnimationClip clip, int layerIndex, float fadeDuration, float speed, float startTime = 0f)
        {
            if (_target == null) return;
            RestoreLogicPose();
            var state = _target.Play(clip, layerIndex, fadeDuration);
            if (state != null) state.Time = startTime;
            SetLayerSpeed(layerIndex, speed);
            CaptureLogicPose();
        }

        public void SetLayerMask(int layerIndex, AvatarMask mask)
        { RestoreLogicPose(); _target?.SetLayerMask(layerIndex, mask); CaptureLogicPose(); }
        public AvatarMask GetLayerMask(int layerIndex) => _target != null ? _target.GetLayerMask(layerIndex) : null;
        public void SetLayerSpeed(int layerIndex, float speed)
        {
            RestoreLogicPose();
            _layerSpeeds[layerIndex] = speed;
            _target?.SetLayerSpeed(layerIndex, _manuallyDriven ? speed : (_paused ? 0f : speed * _scale));
            CaptureLogicPose();
        }
        public void SetTime(int layerIndex, float time)
        {
            RestoreLogicPose();
            _target?.SetTime(layerIndex, time);
            _presentationTime = _logicTime;
            CaptureLogicPose();
        }
        public void SetPlaybackClock(float scale, float speed, bool paused) => SetPlaybackClock(scale, paused);
        public void SetPlaybackClock(float scale, bool paused)
        {
            RestoreLogicPose();
            _scale = scale; _paused = paused;
            foreach (var layer in _layerSpeeds)
                _target?.SetLayerSpeed(layer.Key, _manuallyDriven ? layer.Value : (paused ? 0f : layer.Value * scale));
            CaptureLogicPose();
        }

        public void Tick(float deltaTime)
        {
            if (!_initialized || !_manuallyDriven || _target == null) return;
            RestoreLogicPose();
            _target.EvaluateManually(deltaTime);
            _logicTime += deltaTime;
            CaptureLogicPose();
        }
        internal void BeginLogicSample() => RestoreLogicPose();

        public void TickPresentation(float realDeltaTime)
        {
            if (!_initialized || !_manuallyDriven || _target == null) return;
            RestoreLogicPose();
            _presentationTime = Math.Max(_logicTime, _presentationTime + (_paused ? 0f : realDeltaTime * _scale));
            float lead = (float)(_presentationTime - _logicTime);
            var animator = _target.Animator;
            bool fireEvents = animator.fireEvents;
            animator.fireEvents = false;
            try
            {
                _target.SamplePresentationWeights(lead);
                _target.Graph.Evaluate(lead);
                _clock.MarkPresentationSample();
            }
            finally { animator.fireEvents = fireEvents; }
        }
        private void CaptureLogicPose()
        { if (_initialized && _manuallyDriven && _target != null) _clock.Capture(_target.Graph); }
        private void RestoreLogicPose()
        {
            if (!_initialized || !_manuallyDriven || _target == null || !_clock.Restore()) return;
            var animator = _target.Animator;
            bool fireEvents = animator.fireEvents;
            animator.fireEvents = false;
            try { _target.Graph.Evaluate(0f); }
            finally { animator.fireEvents = fireEvents; }
        }

        public void Dispose()
        {
            if (!_initialized) return;
            _initialized = false;
            if (_target == null) return;
            if (_ownsGraph) _target.ClearPlayGraph();
            if (_manuallyDriven) _target.SetManuallyDriven(_previousManualMode);
        }
    }
}
