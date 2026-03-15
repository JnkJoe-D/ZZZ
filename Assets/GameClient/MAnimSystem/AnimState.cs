using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;

namespace Game.MAnimSystem
{
    /// <summary>
    /// 单个 AnimationClip 的状态封装。
    /// </summary>
    public class AnimState : StateBase
    {
        public AnimationClip Clip { get; private set; }

        private AnimationClipPlayable _clipPlayable;
        private float _cachedLength;
        private bool _cachedIsLooping;
        private readonly Dictionary<float, StateEventHandler> _scheduledEvents = new Dictionary<float, StateEventHandler>();

        /// <summary>
        /// 播放完成事件 (非循环且 Time >= Length 时触发)。
        /// </summary>
        public StateEventHandler OnEnd;

        public AnimState(AnimationClip clip = null)
        {
            if (UpdateClipMetadata(clip))
            {
                Clip = clip;
            }
        }

        protected override Playable CreatePlayable(PlayableGraph graph)
        {
            if (Clip == null) return Playable.Null;
            try
            {
                _clipPlayable = AnimationClipPlayable.Create(graph, Clip);
                return _clipPlayable;
            }
            catch (MissingReferenceException)
            {
                Clip = null;
                _cachedLength = 0f;
                _cachedIsLooping = false;
                Debug.LogWarning("[AnimState] Skipped creating playable because the AnimationClip was already destroyed or unloaded.");
                return Playable.Null;
            }
        }

        /// <summary>
        /// 绑定播放片段并将状态复位为可播放状态。
        /// 当 clip 变化时才重建底层 playable；否则仅复位运行态。
        /// </summary>
        public void BindClip(AnimationClip clip, PlayableGraph graph)
        {
            if (clip == null) return;

            if (!UpdateClipMetadata(clip))
            {
                Debug.LogWarning("[AnimState] Skipped binding an AnimationClip because it was already destroyed or unloaded.");
                return;
            }

            bool clipChanged = Clip != clip;
            Clip = clip;

            if (!_playableCache.IsValid() || clipChanged)
            {
                bool wasConnected = ParentLayer != null && PortIndex >= 0;
                if (wasConnected)
                {
                    ParentLayer.Graph.Disconnect(ParentLayer.Mixer, PortIndex);
                }

                if (_playableCache.IsValid())
                {
                    _playableCache.Destroy();
                }

                try
                {
                    _clipPlayable = AnimationClipPlayable.Create(graph, clip);
                }
                catch (MissingReferenceException)
                {
                    Clip = null;
                    _cachedLength = 0f;
                    _cachedIsLooping = false;
                    Debug.LogWarning("[AnimState] Skipped rebuilding playable because the AnimationClip was destroyed or unloaded during bind.");
                    return;
                }
                _playableCache = _clipPlayable;

                if (wasConnected)
                {
                    ParentLayer.Graph.Connect(_playableCache, 0, ParentLayer.Mixer, PortIndex);
                }
            }

            RebuildPlayable();
        }

        private bool UpdateClipMetadata(AnimationClip clip)
        {
            if (clip == null)
            {
                _cachedLength = 0f;
                _cachedIsLooping = false;
                return false;
            }

            try
            {
                _cachedLength = clip.length;
                _cachedIsLooping = clip.isLooping;
                return true;
            }
            catch (MissingReferenceException)
            {
                _cachedLength = 0f;
                _cachedIsLooping = false;
                return false;
            }
        }

        public float Length => _cachedLength;

        public bool IsLooping
        {
            get => _cachedIsLooping;
            set => _cachedIsLooping = value;
        }

        public float NormalizedTime
        {
            get
            {
                float len = Length;
                return len > 0 ? Mathf.Clamp01(Time / len) : 0f;
            }
            set
            {
                float len = Length;
                if (len > 0)
                {
                    Time = Mathf.Clamp01(value) * len;
                }
            }
        }

        public bool IsDone => !IsLooping && Time >= Length;

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            UpdateScheduledEvents();

            if (IsDone && OnEnd != null)
            {
                OnEnd.Invoke(this);
                OnEnd = null;
            }
        }

        public void AddScheduledEvent(float triggerTime, StateEventHandler callback)
        {
            if (triggerTime < 0f || callback == null) return;

            if (_scheduledEvents.ContainsKey(triggerTime))
            {
                _scheduledEvents[triggerTime] += callback;
            }
            else
            {
                _scheduledEvents[triggerTime] = callback;
            }
        }

        public void RemoveScheduledEvent(float triggerTime, StateEventHandler callback)
        {
            if (!_scheduledEvents.ContainsKey(triggerTime)) return;

            _scheduledEvents[triggerTime] -= callback;
            if (_scheduledEvents[triggerTime] == null)
            {
                _scheduledEvents.Remove(triggerTime);
            }
        }

        public void RemoveScheduledEvents(float triggerTime)
        {
            _scheduledEvents.Remove(triggerTime);
        }

        public override void Clear()
        {
            base.Clear();
            OnEnd = null;
            _scheduledEvents.Clear();
        }

        private void UpdateScheduledEvents()
        {
            if (_scheduledEvents.Count == 0) return;

            float now = Time;
            List<float> keysToFire = null;
            foreach (var kvp in _scheduledEvents)
            {
                if (now >= kvp.Key)
                {
                    if (keysToFire == null) keysToFire = new List<float>();
                    keysToFire.Add(kvp.Key);
                }
            }

            if (keysToFire == null || keysToFire.Count == 0) return;

            keysToFire.Sort();
            for (int i = 0; i < keysToFire.Count; i++)
            {
                float key = keysToFire[i];
                if (!_scheduledEvents.TryGetValue(key, out StateEventHandler callback) || callback == null)
                {
                    continue;
                }

                _scheduledEvents.Remove(key);
                callback.Invoke(this);
            }
        }
    }
}
