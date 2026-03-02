using UnityEngine;
using UnityEngine.Playables;

namespace Game.MAnimSystem
{
    /// <summary>
    /// Base class for all playable animation states.
    /// </summary>
    public abstract class StateBase
    {
        #region Core
        protected Playable _playableCache;

        public Playable Playable => _playableCache;
        public AnimLayer ParentLayer { get; private set; }
        public int PortIndex { get; private set; } = -1;

        public delegate void StateEventHandler(StateBase state);
        public StateEventHandler OnFadeComplete;
        #endregion

        #region Runtime Controls
        public float Weight
        {
            get => _playableCache.IsValid() && ParentLayer != null ? ParentLayer.GetInputWeight(PortIndex) : 0f;
            set
            {
                if (_playableCache.IsValid() && ParentLayer != null)
                {
                    ParentLayer.SetInputWeight(PortIndex, value);
                }
            }
        }

        public float Speed
        {
            get => _playableCache.IsValid() ? (float)_playableCache.GetSpeed() : 0f;
            set
            {
                if (_playableCache.IsValid())
                {
                    _playableCache.SetSpeed(value);
                }
            }
        }

        public virtual float Time
        {
            get => _playableCache.IsValid() ? (float)_playableCache.GetTime() : 0f;
            set
            {
                if (_playableCache.IsValid())
                {
                    _playableCache.SetTime(value);
                }
            }
        }

        public bool IsPaused
        {
            get => Speed == 0f;
            set => Speed = value ? 0f : 1f;
        }

        public void Pause()
        {
            Speed = 0f;
        }

        public void Resume()
        {
            Speed = 1f;
        }
        #endregion

        #region Lifecycle
        public void Initialize(AnimLayer layer, PlayableGraph graph)
        {
            EnsureInitialized(layer, graph);
        }

        internal bool EnsureInitialized(AnimLayer layer, PlayableGraph graph)
        {
            if (layer == null)
            {
                return false;
            }

            if (ParentLayer != null && ParentLayer != layer)
            {
                return false;
            }

            ParentLayer = layer;

            if (!_playableCache.IsValid())
            {
                _playableCache = CreatePlayable(graph);
                OnInitialized();
            }

            return true;
        }

        protected abstract Playable CreatePlayable(PlayableGraph graph);

        protected virtual void OnInitialized()
        {
        }

        public void ConnectToLayer(int portIndex)
        {
            PortIndex = portIndex;
        }

        public virtual void RebuildPlayable()
        {
            if (!_playableCache.IsValid())
            {
                return;
            }

            _playableCache.SetDone(false);
            _playableCache.SetTime(0d);
            _playableCache.SetSpeed(1d);
        }

        public virtual void OnUpdate(float deltaTime)
        {
        }

        public virtual void Clear()
        {
            OnFadeComplete = null;
        }

        public virtual void Destroy()
        {
            if (_playableCache.IsValid())
            {
                _playableCache.Destroy();
                _playableCache = Playable.Null;
            }

            Clear();
        }
        #endregion
    }
}
