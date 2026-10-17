using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>默认音频处理器：会话私有音源；ID 对应一次借用，不随 AudioSource 复用而复用。</summary>
    public sealed class DefaultAudioHandler : IAudioHandler, IDisposable
    {
        private sealed class Voice
        {
            public ActionObjectPool.Lease Lease;
            public AudioSource Source;
            public float Pitch;
            public float Time;
            public bool AffectedBySpeed;
            public bool Paused;
        }

        private static readonly object AudioKey = new object();
        private static int _nextSoundId;
        private readonly ActionObjectPool _pool;
        private readonly Dictionary<int, Voice> _voices = new Dictionary<int, Voice>();
        private readonly List<int> _finished = new List<int>();
        private float _speed = 1f;
        private bool _paused;
        private bool _disposed;

        internal DefaultAudioHandler(ActionObjectPool pool) => _pool = pool;

        public int PlaySound(UnityEngine.AudioClip clip, AudioArgs args)
        {
            if (_disposed || clip == null) return -1;
            var lease = _pool.Rent(AudioKey, storage =>
            {
                var go = new GameObject("Action Audio");
                go.SetActive(false);
                go.transform.SetParent(storage, false);
                go.AddComponent<AudioSource>().playOnAwake = false;
                return go;
            });
            var source = lease.Instance.GetComponent<AudioSource>();
            source.transform.SetParent(args.parent, false);
            source.transform.position = args.position;
            source.clip = clip;
            source.volume = Mathf.Clamp01(args.volume);
            source.loop = args.loop;
            source.spatialBlend = Mathf.Clamp01(args.spatialBlend);
            var voice = new Voice
            {
                Lease = lease, Source = source, Pitch = args.pitch,
                Time = Mathf.Clamp(args.startTime, 0f, Mathf.Max(0f, clip.length - 1f / clip.frequency)),
                AffectedBySpeed = args.affectedByPlaybackSpeed
            };
            int id = checked(++_nextSoundId);
            _voices.Add(id, voice);
            lease.Instance.SetActive(true);
            source.pitch = EffectivePitch(voice);
            source.time = voice.Time;
            source.Play();
            ApplyPlayback(voice);
            return id;
        }

        public void StopSound(int soundId)
        {
            if (!_voices.TryGetValue(soundId, out var voice)) return;
            _voices.Remove(soundId);
            if (voice.Source != null) { voice.Source.Stop(); voice.Source.clip = null; }
            voice.Lease.Dispose();
        }

        public void UpdateSound(int soundId, float volume, float pitch, float time)
        {
            if (!_voices.TryGetValue(soundId, out var voice) || voice.Source == null) return;
            voice.Source.volume = Mathf.Clamp01(volume);
            voice.Pitch = pitch;
            if (time >= 0f && voice.Source.clip != null)
            {
                voice.Time = Mathf.Clamp(time, 0f, Mathf.Max(0f, voice.Source.clip.length - 1f / voice.Source.clip.frequency));
                voice.Source.time = voice.Time;
            }
            ApplyPlayback(voice);
        }

        public void PauseSound(int soundId)
        {
            if (!_voices.TryGetValue(soundId, out var voice)) return;
            voice.Paused = true;
            ApplyPlayback(voice);
        }

        public void ResumeSound(int soundId)
        {
            if (!_voices.TryGetValue(soundId, out var voice)) return;
            voice.Paused = false;
            ApplyPlayback(voice);
        }

        public void SetPlayback(float speed, bool paused)
        {
            _speed = speed;
            _paused = paused;
            foreach (var voice in _voices.Values) ApplyPlayback(voice);
        }
        public void SetPlaybackClock(float scale, bool paused) => SetPlayback(scale, paused);

        private float EffectivePitch(Voice voice) => Mathf.Clamp(voice.Pitch * (voice.AffectedBySpeed ? _speed : 1f), 0f, 3f);
        private void ApplyPlayback(Voice voice)
        {
            if (voice.Source == null) return;
            voice.Source.pitch = EffectivePitch(voice);
            if (_paused || voice.Paused || voice.Source.pitch <= 0f) voice.Source.Pause();
            else voice.Source.UnPause();
        }

        public void Tick(float deltaTime)
        {
            _finished.Clear();
            foreach (var pair in _voices)
            {
                var voice = pair.Value;
                if (!voice.Lease.IsValid || voice.Source == null || voice.Source.clip == null)
                { _finished.Add(pair.Key); continue; }
                if (_paused || voice.Paused) continue;
                voice.Time += deltaTime * EffectivePitch(voice);
                if (!voice.Source.loop && voice.Time >= voice.Source.clip.length) _finished.Add(pair.Key);
                else if (voice.Source.loop && voice.Source.clip.length > 0f) voice.Time %= voice.Source.clip.length;
            }
            foreach (int id in _finished) StopSound(id);
        }

        public void StopAll()
        {
            _finished.Clear();
            _finished.AddRange(_voices.Keys);
            foreach (int id in _finished) StopSound(id);
            _finished.Clear();
        }

        public void Dispose() { if (_disposed) return; _disposed = true; StopAll(); }
    }
}
