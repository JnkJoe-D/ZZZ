using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>默认特效处理器：基于内部对象池；由表现时间推进，逻辑只发送控制指令。</summary>
    public sealed class DefaultVFXHandler : IVFXHandler, IDisposable
    {
        private sealed class Effect
        {
            public ActionObjectPool.Lease Lease;
            public ParticleSystem[] Systems;
            public float[] Speeds;
            public float Remaining = -1f;
            public bool Draining;
            public bool EmissionStopped;
        }

        private readonly ActionObjectPool _pool;
        private readonly Dictionary<GameObject, Effect> _effects = new Dictionary<GameObject, Effect>();
        private readonly List<GameObject> _finished = new List<GameObject>();
        private bool _disposed;
        private float _scale = 1f;
        private bool _paused;

        internal DefaultVFXHandler(ActionObjectPool pool) => _pool = pool;

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => Spawn(prefab, position, rotation, parent, prefab != null ? prefab.transform.localScale : Vector3.one);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Vector3 scale)
        {
            if (_disposed || prefab == null) return null;
            var lease = _pool.Rent(prefab, storage => UnityEngine.Object.Instantiate(prefab, storage));
            var instance = lease.Instance;
            var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            var originals = prefab.GetComponentsInChildren<ParticleSystem>(true);
            var speeds = new float[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                // 恢复上次顿帧/慢动作修改；池归还会停止并清空粒子。
                speeds[i] = i < originals.Length ? originals[i].main.simulationSpeed : 1f;
                main.simulationSpeed = speeds[i] * _scale;
                main.stopAction = ParticleSystemStopAction.None;
                var emission = systems[i].emission;
                emission.enabled = i < originals.Length && originals[i].emission.enabled;
            }
            var effect = new Effect { Lease = lease, Systems = systems, Speeds = speeds };
            _effects.Add(instance, effect);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            instance.SetActive(true);
            foreach (var system in systems)
            {
                if (system != null) system.Play(true);
            }
            return instance;
        }

        public void SetTime(GameObject instance, float time)
        {
            if (instance == null || !_effects.TryGetValue(instance, out var effect)) return;
            for (int i = 0; i < effect.Systems.Length; i++)
                if (effect.Systems[i] != null)
                {
                    var main = effect.Systems[i].main;
                    main.simulationSpeed = effect.Speeds[i];
                    effect.Systems[i].Simulate(Mathf.Max(0f, time), false, true, false);
                    main.simulationSpeed = effect.Speeds[i] * _scale;
                }
        }

        public void SetPlaybackClock(float scale, bool paused)
        {
            _scale = scale; _paused = paused;
            foreach (var effect in _effects.Values)
                for (int i = 0; i < effect.Systems.Length; i++)
                    if (effect.Systems[i] != null)
                    {
                        var system = effect.Systems[i];
                        var main = system.main;
                        main.simulationSpeed = effect.Speeds[i] * scale;
                        if (paused) system.Pause(false);
                        else if (system.isPaused) system.Play(false);
                    }
        }

        public void Return(GameObject instance)
        {
            if (ReferenceEquals(instance, null) || !_effects.TryGetValue(instance, out var effect)) return;
            _effects.Remove(instance);
            for (int i = 0; i < effect.Systems.Length; i++)
            {
                var system = effect.Systems[i];
                if (system == null) continue;
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.simulationSpeed = effect.Speeds[i];
            }
            effect.Lease.Dispose();
        }

        public void ReturnVFXDelay(GameObject instance, float delay)
        {
            if (instance == null || !_effects.TryGetValue(instance, out var effect)) return;
            if (delay <= 0f) { Return(instance); return; }
            effect.Remaining = effect.Remaining < 0f ? delay : Mathf.Min(effect.Remaining, delay);
        }

        public void ReleaseWhenFinished(GameObject instance, bool stopEmission)
        {
            if (instance == null || !_effects.TryGetValue(instance, out var effect)) return;
            if (effect.Systems.Length == 0) { Return(instance); return; }
            effect.Draining = true;
            effect.EmissionStopped |= stopEmission;
            if (stopEmission)
                foreach (var system in effect.Systems)
                    if (system != null)
                    {
                        var emission = system.emission;
                        emission.enabled = false;
                        system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    }
        }

        public void Tick(float deltaTime)
        {
            if (_paused) return;
            _finished.Clear();
            foreach (var pair in _effects)
            {
                var effect = pair.Value;
                if (!effect.Lease.IsValid) { _finished.Add(pair.Key); continue; }
                foreach (var system in effect.Systems)
                    if (system != null && system.gameObject.activeInHierarchy)
                    {
                        // 粒子由 Unity 原生自驱动更新；排空时若已非循环且到期，停止发射
                        if (effect.Draining && (effect.EmissionStopped || (!system.main.loop && system.time >= system.main.duration)))
                        {
                            var emission = system.emission;
                            emission.enabled = false;
                            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        }
                    }
                bool finished = effect.Remaining >= 0f && (effect.Remaining -= deltaTime * _scale) <= 0f;
                if (effect.Draining)
                {
                    bool alive = false;
                    foreach (var system in effect.Systems)
                        if (system != null && system.IsAlive(false)) { alive = true; break; }
                    finished |= !alive;
                }
                if (finished) _finished.Add(pair.Key);
            }
            foreach (var instance in _finished) Return(instance);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _finished.Clear();
            _finished.AddRange(_effects.Keys);
            foreach (var instance in _finished) Return(instance);
            _finished.Clear();
        }
    }
}
