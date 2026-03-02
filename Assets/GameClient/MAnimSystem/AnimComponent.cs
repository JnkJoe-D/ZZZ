using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.MAnimSystem
{
    public struct AnimComponentPoolMetrics
    {
        public int LayerCount;
        public int TotalConnectedStates;
        public int TotalActiveStates;
        public int TotalFadingStates;
        public int TotalIdleStates;
        public int TotalFreePorts;
        public int TotalNewStateCount;
        public int TotalReusedStateCount;
        public int TotalReturnedToPoolCount;
        public int TotalDestroyedStateCount;
        public int PeakConnectedStates;
        public int PeakIdleStates;
        public int PeakFadingStates;

        public int TotalAcquireCount => TotalNewStateCount + TotalReusedStateCount;
        public float TotalReuseHitRate => TotalAcquireCount > 0 ? (float)TotalReusedStateCount / TotalAcquireCount : 0f;
    }

    /// <summary>
    /// Core runtime animation entry for one character/object.
    /// Owns PlayableGraph and multiple AnimLayer instances.
    /// </summary>
    public class AnimComponent : MonoBehaviour
    {
        #region Serialized
        [SerializeField] private Animator _animator;
        public bool PlayAutomatically = true;
        #endregion

        #region Runtime Fields
        public Animator Animator => _animator;
        public PlayableGraph Graph { get; private set; }

        private AnimationLayerMixerPlayable _layerMixer;
        private readonly List<AnimLayer> _layers = new List<AnimLayer>();
        private readonly Dictionary<int, double> _layerSpeeds = new Dictionary<int, double>();

        private bool _isGraphCreated;
        private bool _isInitialized;
        #endregion

        #region Indexer
        public AnimLayer this[int index] => GetLayer(index);
        public int LayerCount => _layers.Count;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (PlayAutomatically)
            {
                InitializeGraph();
            }
        }

        private void OnDisable()
        {
            ClearPlayGraph();
        }

        private void Update()
        {
            if (!_isGraphCreated)
            {
                return;
            }

            UpdateInternal(Time.deltaTime);
        }
        #endregion

        #region Init and Update
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _animator ??= GetComponentInChildren<Animator>();
            _isInitialized = true;
        }

        private void UpdateInternal(float deltaTime)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                float layerDeltaTime = deltaTime;
                if (_layerSpeeds.TryGetValue(i, out double speed))
                {
                    _layers[i]?.SetSpeed((float)speed);
                    layerDeltaTime *= (float)speed;
                }

                _layers[i]?.Update(layerDeltaTime);
            }
        }

        public void ManualUpdate(float deltaTime)
        {
            if (!_isGraphCreated)
            {
                return;
            }

            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i]?.Update(deltaTime);
            }
        }
        #endregion

        #region Graph and Layers
        public void InitializeGraph()
        {
            if (_isGraphCreated)
            {
                return;
            }

            Graph = PlayableGraph.Create($"AnimComponent_{gameObject.name}");
            Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _layerMixer = AnimationLayerMixerPlayable.Create(Graph, 1);
            CreateLayer(0);

            var output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
            output.SetSourcePlayable(_layerMixer);

            Graph.Play();
            _isGraphCreated = true;
        }

        public AnimLayer GetLayer(int index)
        {
            if (!_isGraphCreated)
            {
                InitializeGraph();
            }

            if (index < 0)
            {
                return null;
            }

            while (_layers.Count <= index)
            {
                CreateLayer(_layers.Count);
            }

            return _layers[index];
        }

        private void CreateLayer(int index)
        {
            if (index < 0)
            {
                return;
            }

            if (index >= _layerMixer.GetInputCount())
            {
                _layerMixer.SetInputCount(index + 1);
            }

            var layer = new AnimLayer(Graph, index, _layerMixer);
            Graph.Connect(layer.Mixer, 0, _layerMixer, index);

            while (_layers.Count <= index)
            {
                _layers.Add(null);
            }

            _layers[index] = layer;
            if (_layerSpeeds.ContainsKey(index))
            {
                _layerSpeeds[index] = 1.0;
            }
            else
            {
                _layerSpeeds.Add(index, 1.0);
            }
        }

        public void SetLayerSpeed(int layerIndex, float speedScale)
        {
            if (!_isGraphCreated)
            {
                return;
            }

            if (layerIndex < 0 || layerIndex >= _layers.Count)
            {
                return;
            }

            _layerSpeeds[layerIndex] = speedScale;
            _layers[layerIndex].SetSpeed(speedScale);
        }

        public void ClearPlayGraph()
        {
            if (_isGraphCreated)
            {
                foreach (AnimLayer layer in _layers)
                {
                    layer?.Destroy();
                }

                _layers.Clear();
                _layerSpeeds.Clear();
                Graph.Destroy();
                _isGraphCreated = false;
            }

            if (!Animator.isActiveAndEnabled)
            {
                return;
            }

            Animator.Rebind();
            Animator.Update(0f);
        }
        #endregion

        #region Play API - Clip
        public AnimState Play(AnimationClip clip)
        {
            return Play(clip, 0.25f);
        }

        public AnimState Play(AnimationClip clip, float fadeDuration = 0.25f, bool forceResetTime = false)
        {
            return GetLayer(0).Play(clip, fadeDuration, forceResetTime);
        }

        public AnimState Play(AnimationClip clip, int layerIndex, float fadeDuration = 0.25f, bool forceResetTime = false)
        {
            return GetLayer(layerIndex).Play(clip, fadeDuration, forceResetTime);
        }
        #endregion

        #region Play API - AnimState
        public AnimState Play(AnimState state)
        {
            return Play(state, 0.25f);
        }

        public AnimState Play(AnimState state, float fadeDuration, bool forceResetTime = false)
        {
            GetLayer(0).Play(state, fadeDuration, forceResetTime);
            return state;
        }

        public AnimState Play(AnimState state, int layerIndex, float fadeDuration = 0.25f, bool forceResetTime = false)
        {
            GetLayer(layerIndex).Play(state, fadeDuration, forceResetTime);
            return state;
        }

        public void CrossFade(AnimState state, float fadeDuration)
        {
            Play(state, fadeDuration);
        }
        #endregion

        #region Play API - Generic StateBase
        public StateBase Play(StateBase state)
        {
            return Play(state, 0.25f);
        }

        public StateBase Play(StateBase state, float fadeDuration, bool forceResetTime = false)
        {
            GetLayer(0).Play(state, fadeDuration, forceResetTime);
            return state;
        }

        public StateBase Play(StateBase state, int layerIndex, float fadeDuration = 0.25f, bool forceResetTime = false)
        {
            GetLayer(layerIndex).Play(state, fadeDuration, forceResetTime);
            return state;
        }

        public void CrossFade(StateBase state, float fadeDuration)
        {
            Play(state, fadeDuration);
        }
        #endregion

        #region BlendTree Factory
        public BlendTree1DState CreateBlendTree1DState(IReadOnlyList<BlendTree1DChild> children, float initialParameter = 0f)
        {
            var state = new BlendTree1DState();
            state.SetChildren(children);
            state.Parameter = initialParameter;
            return state;
        }

        public BlendTree2DState CreateBlendTree2DState(IReadOnlyList<BlendTree2DChild> children)
        {
            return CreateBlendTree2DState(children, Vector2.zero);
        }

        public BlendTree2DState CreateBlendTree2DState(IReadOnlyList<BlendTree2DChild> children, Vector2 initialParameter)
        {
            var state = new BlendTree2DState();
            state.SetChildren(children);
            state.Parameter = initialParameter;
            return state;
        }
        #endregion

        #region Evaluate and Sampling
        public void Evaluate(AnimationClip clip, int layerIndex, float time)
        {
            if (!_isGraphCreated || clip == null)
            {
                return;
            }

            AnimLayer layer = GetLayer(layerIndex);
            if (layer == null)
            {
                return;
            }

            AnimState state = layer.GetState(clip);
            if (state != null)
            {
                state.Time = time;
                Graph.Evaluate(0f);
            }
        }

        public void SampleClip(AnimationClip clip, float normalizedTime)
        {
            if (!_isGraphCreated || clip == null)
            {
                return;
            }

            AnimState state = GetLayer(0).Play(clip, 0f);
            state.NormalizedTime = normalizedTime;
            Graph.Evaluate(0f);
        }
        #endregion

        #region Query API
        public StateBase GetCurrentState()
        {
            return GetLayer(0).GetCurrentState();
        }

        public AnimationClip GetCurrentClip()
        {
            return GetLayer(0).GetCurrentClip();
        }

        public bool IsPlaying(AnimationClip clip)
        {
            return GetLayer(0).IsPlaying(clip);
        }

        public float GetCurrentTime()
        {
            return GetLayer(0).GetCurrentTime();
        }

        public float GetCurrentProgress()
        {
            return GetLayer(0).GetCurrentProgress();
        }
        #endregion

        #region Pool Metrics
        public AnimComponentPoolMetrics GetPoolMetrics()
        {
            var metrics = new AnimComponentPoolMetrics
            {
                LayerCount = _layers.Count
            };

            for (int i = 0; i < _layers.Count; i++)
            {
                AnimLayer layer = _layers[i];
                if (layer == null)
                {
                    continue;
                }

                AnimLayerPoolMetrics layerMetrics = layer.GetPoolMetrics();
                metrics.TotalConnectedStates += layerMetrics.ConnectedStateCount;
                metrics.TotalActiveStates += layerMetrics.ActiveStateCount;
                metrics.TotalFadingStates += layerMetrics.FadingStateCount;
                metrics.TotalIdleStates += layerMetrics.IdleStateCount;
                metrics.TotalFreePorts += layerMetrics.FreePortCount;
                metrics.TotalNewStateCount += layerMetrics.NewStateCount;
                metrics.TotalReusedStateCount += layerMetrics.ReusedStateCount;
                metrics.TotalReturnedToPoolCount += layerMetrics.ReturnedToPoolCount;
                metrics.TotalDestroyedStateCount += layerMetrics.DestroyedStateCount;
                metrics.PeakConnectedStates += layerMetrics.PeakConnectedStateCount;
                metrics.PeakIdleStates += layerMetrics.PeakIdleStateCount;
                metrics.PeakFadingStates += layerMetrics.PeakFadingStateCount;
            }

            return metrics;
        }

        public string GetPoolMetricsReport(bool includePerLayer = true)
        {
            AnimComponentPoolMetrics total = GetPoolMetrics();
            var sb = new StringBuilder(512);

            sb.Append("[MAnimSystem.Pool] ")
              .Append("layers=").Append(total.LayerCount)
              .Append(", connected=").Append(total.TotalConnectedStates)
              .Append(", active=").Append(total.TotalActiveStates)
              .Append(", fading=").Append(total.TotalFadingStates)
              .Append(", idle=").Append(total.TotalIdleStates)
              .Append(", freePorts=").Append(total.TotalFreePorts)
              .Append(", new=").Append(total.TotalNewStateCount)
              .Append(", reused=").Append(total.TotalReusedStateCount)
              .Append(", returned=").Append(total.TotalReturnedToPoolCount)
              .Append(", destroyed=").Append(total.TotalDestroyedStateCount)
              .Append(", reuseRate=").Append((total.TotalReuseHitRate * 100f).ToString("F1")).Append("%")
              .Append(", peakConnected=").Append(total.PeakConnectedStates)
              .Append(", peakIdle=").Append(total.PeakIdleStates)
              .Append(", peakFading=").Append(total.PeakFadingStates);

            if (includePerLayer)
            {
                for (int i = 0; i < _layers.Count; i++)
                {
                    AnimLayer layer = _layers[i];
                    if (layer == null)
                    {
                        continue;
                    }

                    AnimLayerPoolMetrics m = layer.GetPoolMetrics();
                    sb.Append("\n  [Layer ").Append(m.LayerIndex).Append("] ")
                      .Append("connected=").Append(m.ConnectedStateCount)
                      .Append(", active=").Append(m.ActiveStateCount)
                      .Append(", fading=").Append(m.FadingStateCount)
                      .Append(", idle=").Append(m.IdleStateCount)
                      .Append(", freePorts=").Append(m.FreePortCount)
                      .Append(", new=").Append(m.NewStateCount)
                      .Append(", reused=").Append(m.ReusedStateCount)
                      .Append(", returned=").Append(m.ReturnedToPoolCount)
                      .Append(", destroyed=").Append(m.DestroyedStateCount)
                      .Append(", reuseRate=").Append((m.ReuseHitRate * 100f).ToString("F1")).Append("%")
                      .Append(", peakConnected=").Append(m.PeakConnectedStateCount)
                      .Append(", peakIdle=").Append(m.PeakIdleStateCount)
                      .Append(", peakFading=").Append(m.PeakFadingStateCount);
                }
            }

            return sb.ToString();
        }

        public void LogPoolMetrics(bool includePerLayer = true)
        {
            Debug.Log(GetPoolMetricsReport(includePerLayer));
        }

        public void ResetPoolMetricsCounters()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i]?.ResetPoolMetricsCounters();
            }
        }
        #endregion

        #region Layer Helpers
        public AvatarMask GetLayerMask(int layer)
        {
            if (layer < 0 || layer >= _layers.Count)
            {
                return null;
            }

            return _layers[layer].Mask;
        }

        public void SetLayerMask(int layer, AvatarMask avatarMask)
        {
            if (layer < 0 || layer >= _layers.Count)
            {
                return;
            }

            _layers[layer].Mask = avatarMask;
        }

        public void Log(string message)
        {
            Debug.Log($"[AnimComponent] {message}");
        }
        #endregion
    }
}
