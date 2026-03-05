using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.MAnimSystem
{
    public struct AnimLayerPoolMetrics
    {
        public int LayerIndex;
        public int ConnectedStateCount;
        public int ActiveStateCount;
        public int FadingStateCount;
        public int IdleStateCount;
        public int FreePortCount;
        public int NewStateCount;
        public int ReusedStateCount;
        public int ReturnedToPoolCount;
        public int DestroyedStateCount;
        public int PeakConnectedStateCount;
        public int PeakIdleStateCount;
        public int PeakFadingStateCount;

        public int AcquireCount => NewStateCount + ReusedStateCount;
        public float ReuseHitRate => AcquireCount > 0 ? (float)ReusedStateCount / AcquireCount : 0f;
    }

    public enum AnimBlendMode
    {
        Linear = 0,
        SmoothStep = 1
    }

    /// <summary>
    /// Manages cross-fade and state lifecycle for one animation layer.
    /// </summary>
    public class AnimLayer
    {
        #region Constants
        private const float INTERRUPT_SPEED_MULTIPLIER = 2f;
        private const int MAX_CACHE_SIZE = 32;
        #endregion

        #region Types
        private struct FadingState
        {
            public StateBase State;
            public float FadeSpeed;
            public bool IsInterrupted;
        }
        #endregion

        #region Public Properties
        public PlayableGraph Graph { get; private set; }
        public AnimationMixerPlayable Mixer { get; private set; }
        public int LayerIndex { get; private set; }

        public float Weight
        {
            get => _weight;
            set => SetLayerWeight(value);
        }

        public double PlaybackSpeed
        {
            get => Mixer.GetSpeed();
            set => Mixer.SetSpeed(value);
        }

        public AvatarMask Mask
        {
            get => _mask == null ? new AvatarMask() : _mask;
            set
            {
                _mask = value ?? new AvatarMask();
                if (_layerMixer.IsValid())
                {
                    _layerMixer.SetLayerMaskFromAvatarMask((uint)LayerIndex, _mask);
                }
            }
        }

        public bool IsAdditive
        {
            get => _isAdditive;
            set => SetAdditive(value);
        }

        public AnimBlendMode BlendMode { get; set; } = AnimBlendMode.SmoothStep;
        #endregion

        #region Fields - Layer Settings
        private readonly AnimationLayerMixerPlayable _layerMixer;
        private float _weight = 1f;
        private AvatarMask _mask;
        private bool _isAdditive;

        private float _targetLayerWeight;
        private float _layerFadeSpeed;
        private bool _isLayerFading;
        #endregion

        #region Fields - State Runtime
        private readonly List<StateBase> _states = new List<StateBase>();
        private readonly List<int> _freePorts = new List<int>();

        private StateBase _targetState;
        private float _targetFadeProgress;
        private float _fadeSpeed;
        private readonly List<FadingState> _fadingStates = new List<FadingState>();

        private readonly List<StateBase> _pendingCleanup = new List<StateBase>();
        private readonly HashSet<StateBase> _pendingCleanupSet = new HashSet<StateBase>();
        #endregion

        #region Fields - AnimState Pool
        private readonly Stack<AnimState> _idleAnimStates = new Stack<AnimState>();
        private readonly HashSet<AnimState> _idleAnimStateSet = new HashSet<AnimState>();
        #endregion

        #region Fields - Metrics
        private int _newStateCount;
        private int _reusedStateCount;
        private int _returnedToPoolCount;
        private int _destroyedStateCount;
        private int _peakConnectedStateCount;
        private int _peakIdleStateCount;
        private int _peakFadingStateCount;
        #endregion

        #region Constructor
        public AnimLayer(PlayableGraph graph, int layerIndex, AnimationLayerMixerPlayable layerMixer = default)
        {
            Graph = graph;
            LayerIndex = layerIndex;
            _layerMixer = layerMixer;
            Mixer = AnimationMixerPlayable.Create(graph, 0);

            if (_layerMixer.IsValid())
            {
                _layerMixer.SetInputWeight(layerIndex, _weight);
            }

            UpdatePeakMetrics();
        }
        #endregion

        #region Layer Controls
        public void SetLayerWeight(float weight)
        {
            _weight = Mathf.Clamp01(weight);
            if (_layerMixer.IsValid())
            {
                _layerMixer.SetInputWeight(LayerIndex, _weight);
            }
        }

        public AvatarMask GetMask()
        {
            return Mask;
        }

        public void SetAdditive(bool additive)
        {
            _isAdditive = additive;
            if (_layerMixer.IsValid())
            {
                _layerMixer.SetLayerAdditive((uint)LayerIndex, additive);
            }
        }

        public void SetSpeed(float speed)
        {
            PlaybackSpeed = speed;
        }

        public void StartLayerFade(float targetWeight, float duration)
        {
            targetWeight = Mathf.Clamp01(targetWeight);
            if (Mathf.Abs(targetWeight - _weight) < 0.001f)
            {
                _isLayerFading = false;
                return;
            }

            if (duration <= 0f)
            {
                SetLayerWeight(targetWeight);
                _isLayerFading = false;
                return;
            }

            _targetLayerWeight = targetWeight;
            _layerFadeSpeed = 1f / duration;
            _isLayerFading = true;
        }

        private void UpdateLayerFade(float deltaTime)
        {
            if (!_isLayerFading)
            {
                return;
            }

            float direction = _targetLayerWeight > _weight ? 1f : -1f;
            float newWeight = _weight + direction * _layerFadeSpeed * deltaTime;

            if ((direction > 0f && newWeight >= _targetLayerWeight) ||
                (direction < 0f && newWeight <= _targetLayerWeight))
            {
                SetLayerWeight(_targetLayerWeight);
                _isLayerFading = false;
            }
            else
            {
                SetLayerWeight(newWeight);
            }
        }
        #endregion

        #region Play API
        public void Play(AnimState state)
        {
            Play((StateBase)state, 0.25f, false);
        }

        public void Play(AnimState state, float fadeDuration, bool forceResetTime = false)
        {
            Play((StateBase)state, fadeDuration, forceResetTime);
        }

        public void Play(StateBase state)
        {
            Play(state, 0.25f, false);
        }

        public void Play(StateBase state, float fadeDuration, bool forceResetTime = false)
        {
            if (state == null)
            {
                return;
            }

            if (!PrepareStateForPlay(state))
            {
                return;
            }

            CancelPendingCleanup(state);

            if (state == _targetState)
            {
                if (forceResetTime)
                {
                    state.RebuildPlayable();
                }
                return;
            }

            if (!IsStateConnected(state))
            {
                ConnectState(state);
            }

            float salvagedWeight = 0f;
            bool wasFading = false;
            for (int i = _fadingStates.Count - 1; i >= 0; i--)
            {
                if (_fadingStates[i].State == state)
                {
                    salvagedWeight = _fadingStates[i].State.Weight;
                    wasFading = true;
                    _fadingStates.RemoveAt(i);
                }
            }

            RemoveFromFadingStates(state);

            float nextFadeSpeed = 1f / Mathf.Max(fadeDuration, 0.001f);

            if (_targetState != null)
            {
                // The previous target should fade out with the current transition's duration.
                AddToFadingStates(_targetState, nextFadeSpeed);
            }

            for (int i = 0; i < _fadingStates.Count; i++)
            {
                FadingState fs = _fadingStates[i];
                if (!fs.IsInterrupted)
                {
                    fs.IsInterrupted = true;
                    fs.FadeSpeed *= INTERRUPT_SPEED_MULTIPLIER;
                    _fadingStates[i] = fs;
                }
            }

            _targetState?.Clear();
            _targetState = state;
            _fadeSpeed = nextFadeSpeed;
            _targetFadeProgress = salvagedWeight;

            if (!wasFading || forceResetTime)
            {
                _targetState.RebuildPlayable();
            }

            if (_targetState.Playable.IsValid())
            {
                _targetState.Playable.SetDone(false);
            }

            _targetState.Weight = salvagedWeight;

            if (fadeDuration <= 0f)
            {
                _targetState.Weight = 1f;
                _targetFadeProgress = 1f;

                for (int i = 0; i < _fadingStates.Count; i++)
                {
                    StateBase fading = _fadingStates[i].State;
                    if (fading == null)
                    {
                        continue;
                    }

                    fading.Weight = 0f;
                    MarkForCleanup(fading);
                }

                _fadingStates.Clear();
                _targetState.OnFadeComplete?.Invoke(_targetState);
            }
        }

        public AnimState Play(AnimationClip clip, float fadeDuration = 0.25f, bool forceResetTime = false)
        {
            if (clip == null)
            {
                return null;
            }

            AnimState state = AcquireAnimState(clip);
            Play(state, fadeDuration, forceResetTime);
            return state;
        }
        #endregion

        #region Query API
        public AnimState GetState(AnimationClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            if (_targetState is AnimState targetAnim && targetAnim.Clip == clip)
            {
                return targetAnim;
            }

            for (int i = 0; i < _fadingStates.Count; i++)
            {
                if (_fadingStates[i].State is AnimState anim && anim.Clip == clip)
                {
                    return anim;
                }
            }

            return null;
        }

        public StateBase GetCurrentState()
        {
            return _targetState;
        }

        public AnimationClip GetCurrentClip()
        {
            return (_targetState as AnimState)?.Clip;
        }

        public bool IsPlaying(AnimationClip clip)
        {
            return GetCurrentClip() == clip;
        }
        public AnimState GetAnimState(AnimationClip clip)
        {
            if(clip==null)return null;
            for(int i=0;i< _states.Count;++i)
            {
                if((_states[i] as AnimState).Clip == clip)
                {
                    return _states[i] as AnimState;
                }
            }
            return null;
        }
        public float GetCurrentTime()
        {
            return _targetState?.Time ?? 0f;
        }

        public float GetCurrentProgress()
        {
            if (_targetState is AnimState animState)
            {
                return animState.NormalizedTime;
            }

            return 0f;
        }

        public float GetInputWeight(int portIndex)
        {
            if (portIndex >= 0 && portIndex < Mixer.GetInputCount())
            {
                return Mixer.GetInputWeight(portIndex);
            }

            return 0f;
        }

        public void SetInputWeight(int portIndex, float weight)
        {
            if (portIndex >= 0 && portIndex < Mixer.GetInputCount())
            {
                Mixer.SetInputWeight(portIndex, Mathf.Clamp01(weight));
            }
        }
        #endregion

        #region Metrics
        public AnimLayerPoolMetrics GetPoolMetrics()
        {
            int activeCount = 0;
            for (int i = 0; i < _states.Count; i++)
            {
                StateBase state = _states[i];
                if (state != null && (state == _targetState || state.Weight > 0.001f))
                {
                    activeCount++;
                }
            }

            return new AnimLayerPoolMetrics
            {
                LayerIndex = LayerIndex,
                ConnectedStateCount = _states.Count,
                ActiveStateCount = activeCount,
                FadingStateCount = _fadingStates.Count,
                IdleStateCount = _idleAnimStates.Count,
                FreePortCount = _freePorts.Count,
                NewStateCount = _newStateCount,
                ReusedStateCount = _reusedStateCount,
                ReturnedToPoolCount = _returnedToPoolCount,
                DestroyedStateCount = _destroyedStateCount,
                PeakConnectedStateCount = _peakConnectedStateCount,
                PeakIdleStateCount = _peakIdleStateCount,
                PeakFadingStateCount = _peakFadingStateCount
            };
        }

        public void ResetPoolMetricsCounters()
        {
            _newStateCount = 0;
            _reusedStateCount = 0;
            _returnedToPoolCount = 0;
            _destroyedStateCount = 0;
            _peakConnectedStateCount = _states.Count;
            _peakIdleStateCount = _idleAnimStates.Count;
            _peakFadingStateCount = _fadingStates.Count;
        }

        private void UpdatePeakMetrics()
        {
            if (_states.Count > _peakConnectedStateCount)
            {
                _peakConnectedStateCount = _states.Count;
            }

            if (_idleAnimStates.Count > _peakIdleStateCount)
            {
                _peakIdleStateCount = _idleAnimStates.Count;
            }

            if (_fadingStates.Count > _peakFadingStateCount)
            {
                _peakFadingStateCount = _fadingStates.Count;
            }
        }
        #endregion

        #region Update
        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
        }

        private void OnUpdate(float deltaTime)
        {
            UpdateLayerFade(deltaTime);

            if (_targetState != null && _targetFadeProgress < 1f)
            {
                _targetFadeProgress = Mathf.Clamp01(_targetFadeProgress + _fadeSpeed * deltaTime);
                float targetWeight = BlendMode == AnimBlendMode.SmoothStep
                    ? Mathf.SmoothStep(0f, 1f, _targetFadeProgress)
                    : _targetFadeProgress;

                _targetState.Weight = targetWeight;
                if (_targetFadeProgress >= 1f)
                {
                    _targetState.OnFadeComplete?.Invoke(_targetState);
                }
            }

            float totalFadeOutWeight = 0f;
            for (int i = _fadingStates.Count - 1; i >= 0; i--)
            {
                FadingState fs = _fadingStates[i];
                if (fs.State == null || fs.State == _targetState)
                {
                    _fadingStates.RemoveAt(i);
                    continue;
                }

                float newWeight = fs.State.Weight - fs.FadeSpeed * deltaTime;
                if (newWeight <= 0f)
                {
                    fs.State.Weight = 0f;
                    MarkForCleanup(fs.State);
                    _fadingStates.RemoveAt(i);
                }
                else
                {
                    fs.State.Weight = newWeight;
                    totalFadeOutWeight += newWeight;
                }
            }

            NormalizeWeights(totalFadeOutWeight);

            for (int i = 0; i < _states.Count; i++)
            {
                StateBase state = _states[i];
                if (state != null && (state.Weight > 0.001f || state == _targetState))
                {
                    state.OnUpdate(deltaTime);
                }
            }

            ProcessCleanupQueue();
            UpdatePeakMetrics();
        }

        private void NormalizeWeights(float totalFadeOutWeight)
        {
            if (_targetState == null)
            {
                if (_fadingStates.Count == 0)
                {
                    return;
                }

                if (totalFadeOutWeight <= 0.001f)
                {
                    float even = 1f / _fadingStates.Count;
                    for (int i = 0; i < _fadingStates.Count; i++)
                    {
                        _fadingStates[i].State.Weight = even;
                    }
                    return;
                }

                float scale = 1f / totalFadeOutWeight;
                for (int i = 0; i < _fadingStates.Count; i++)
                {
                    _fadingStates[i].State.Weight *= scale;
                }

                return;
            }

            if (_fadingStates.Count == 0)
            {
                if (_targetFadeProgress >= 1f)
                {
                    _targetState.Weight = 1f;
                }
                return;
            }

            if (totalFadeOutWeight <= 0.001f)
            {
                _targetState.Weight = 1f;
                for (int i = 0; i < _fadingStates.Count; i++)
                {
                    _fadingStates[i].State.Weight = 0f;
                    MarkForCleanup(_fadingStates[i].State);
                }
                _fadingStates.Clear();
                return;
            }

            float remain = Mathf.Clamp01(1f - Mathf.Clamp01(_targetState.Weight));
            float factor = remain / totalFadeOutWeight;
            for (int i = 0; i < _fadingStates.Count; i++)
            {
                _fadingStates[i].State.Weight *= factor;
            }
        }
        #endregion

        #region Connectivity Helpers
        private bool PrepareStateForPlay(StateBase state)
        {
            if (state.ParentLayer != null && state.ParentLayer != this)
            {
                Debug.LogError($"[AnimLayer] State owner mismatch. state={state.GetType().Name}, ownerLayer={state.ParentLayer.LayerIndex}, playLayer={LayerIndex}");
                return false;
            }

            if (!state.EnsureInitialized(this, Graph))
            {
                Debug.LogError($"[AnimLayer] Failed to initialize state. state={state.GetType().Name}, layer={LayerIndex}");
                return false;
            }

            if (!state.Playable.IsValid())
            {
                Debug.LogError($"[AnimLayer] State playable is invalid. state={state.GetType().Name}, layer={LayerIndex}");
                return false;
            }

            return true;
        }

        private bool IsStateConnected(StateBase state)
        {
            return state != null && state.PortIndex >= 0 && _states.Contains(state);
        }

        private void ConnectState(StateBase state)
        {
            if (state == null || !state.Playable.IsValid())
            {
                return;
            }

            int port;
            if (_freePorts.Count > 0)
            {
                port = _freePorts[_freePorts.Count - 1];
                _freePorts.RemoveAt(_freePorts.Count - 1);
            }
            else
            {
                port = Mixer.GetInputCount();
                Mixer.SetInputCount(port + 1);
            }

            Graph.Connect(state.Playable, 0, Mixer, port);
            state.ConnectToLayer(port);
            state.Weight = 0f;

            if (!_states.Contains(state))
            {
                _states.Add(state);
            }

            UpdatePeakMetrics();
        }

        private void DisconnectState(StateBase state)
        {
            if (state == null)
            {
                return;
            }

            if (state.PortIndex >= 0)
            {
                if (Mixer.IsValid())
                {
                    Mixer.SetInputWeight(state.PortIndex, 0f);
                }

                Graph.Disconnect(Mixer, state.PortIndex);
                if (!_freePorts.Contains(state.PortIndex))
                {
                    _freePorts.Add(state.PortIndex);
                }

                state.ConnectToLayer(-1);
            }

            _states.Remove(state);
        }

        private void AddToFadingStates(StateBase state, float fadeSpeed)
        {
            if (state == null)
            {
                return;
            }

            for (int i = 0; i < _fadingStates.Count; i++)
            {
                FadingState fs = _fadingStates[i];
                if (fs.State == state)
                {
                    fs.FadeSpeed = Mathf.Max(fs.FadeSpeed, fadeSpeed);
                    fs.IsInterrupted = true;
                    _fadingStates[i] = fs;
                    return;
                }
            }

            _fadingStates.Add(new FadingState
            {
                State = state,
                FadeSpeed = fadeSpeed,
                IsInterrupted = true
            });
        }

        private void RemoveFromFadingStates(StateBase state)
        {
            if (state == null)
            {
                return;
            }

            for (int i = _fadingStates.Count - 1; i >= 0; i--)
            {
                if (_fadingStates[i].State == state)
                {
                    _fadingStates.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Pooling and Cleanup
        public void ClearCache()
        {
            while (_idleAnimStates.Count > 0)
            {
                AnimState state = _idleAnimStates.Pop();
                if (state == null)
                {
                    continue;
                }

                _idleAnimStateSet.Remove(state);
                state.Destroy();
            }

            _idleAnimStateSet.Clear();
        }

        private AnimState AcquireAnimState(AnimationClip clip)
        {
            AnimState state = null;

            while (_idleAnimStates.Count > 0 && state == null)
            {
                state = _idleAnimStates.Pop();
            }

            if (state != null)
            {
                _idleAnimStateSet.Remove(state);
                _reusedStateCount++;
            }
            else
            {
                state = new AnimState();
                state.Initialize(this, Graph);
                _newStateCount++;
            }

            state.BindClip(clip, Graph);
            state.Clear();
            state.RebuildPlayable();
            UpdatePeakMetrics();
            return state;
        }

        private void MarkForCleanup(StateBase state)
        {
            if (state == null || state == _targetState)
            {
                return;
            }

            if (_pendingCleanupSet.Add(state))
            {
                _pendingCleanup.Add(state);
            }
        }

        private void CancelPendingCleanup(StateBase state)
        {
            if (state == null)
            {
                return;
            }

            if (!_pendingCleanupSet.Remove(state))
            {
                return;
            }

            for (int i = _pendingCleanup.Count - 1; i >= 0; i--)
            {
                if (_pendingCleanup[i] == state)
                {
                    _pendingCleanup.RemoveAt(i);
                    break;
                }
            }
        }

        private void ProcessCleanupQueue()
        {
            if (_pendingCleanup.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingCleanup.Count; i++)
            {
                StateBase state = _pendingCleanup[i];
                if (state == null)
                {
                    continue;
                }

                if (state is AnimState animState)
                {
                    ReturnAnimStateToPool(animState);
                }
                else
                {
                    DestroyStateNow(state, true);
                }
            }

            _pendingCleanup.Clear();
            _pendingCleanupSet.Clear();
        }

        private void ReturnAnimStateToPool(AnimState animState)
        {
            if (animState == null || animState == _targetState)
            {
                return;
            }

            if (_idleAnimStateSet.Contains(animState))
            {
                return;
            }

            RemoveFromFadingStates(animState);
            animState.Weight = 0f;
            DisconnectState(animState);
            animState.Clear();
            animState.Pause();
            animState.RebuildPlayable();

            if (_idleAnimStates.Count >= MAX_CACHE_SIZE)
            {
                animState.Destroy();
                _destroyedStateCount++;
                UpdatePeakMetrics();
                return;
            }

            _idleAnimStates.Push(animState);
            _idleAnimStateSet.Add(animState);
            _returnedToPoolCount++;
            UpdatePeakMetrics();
        }

        private void DestroyStateNow(StateBase state, bool countAsDestroyed)
        {
            if (state == null || state == _targetState)
            {
                return;
            }

            RemoveFromFadingStates(state);
            DisconnectState(state);
            state.Clear();
            state.Destroy();

            if (countAsDestroyed)
            {
                _destroyedStateCount++;
            }

            UpdatePeakMetrics();
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _pendingCleanup.Clear();
            _pendingCleanupSet.Clear();

            ClearCache();

            for (int i = _states.Count - 1; i >= 0; i--)
            {
                DestroyStateNow(_states[i], false);
            }

            _states.Clear();
            _fadingStates.Clear();
            _freePorts.Clear();
            _targetState = null;
            _targetFadeProgress = 0f;

            if (Mixer.IsValid())
            {
                Mixer.Destroy();
            }
        }
        #endregion
    }
}
