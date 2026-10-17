using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ATEditor.Test
{
    public class ActionPresentationTests
    {
        private GameObject _owner;
        private readonly List<Object> _assets = new List<Object>();

        private ActionDirector Director(float duration = 10f)
        {
            _owner = new GameObject("IndependentPresentation");
            var director = _owner.AddComponent<ActionDirector>();
            director.UpdateAutomatically = false;
            var timeline = TestTimelineBuilder.Create(duration);
            _assets.Add(timeline);
            director.Play(timeline);
            return director;
        }

        private GameObject Effect(bool particle = false)
        {
            var prefab = new GameObject("MemoryEffect");
            prefab.SetActive(false);
            if (particle)
            {
                var system = prefab.AddComponent<ParticleSystem>();
                var main = system.main;
                main.duration = 5f;
                main.loop = false;
                main.startLifetime = 3f;
                main.simulationSpeed = 1f;
            }
            _assets.Add(prefab);
            return prefab;
        }

        private UnityEngine.AudioClip Sound(float duration = 2f)
        {
            var sound = UnityEngine.AudioClip.Create("MemorySound", (int)(44100 * duration), 1, 44100, false);
            _assets.Add(sound);
            return sound;
        }

        private static IVFXHandler VFX(ActionDirector director) => director.GetRunner().Context.GetService<IVFXHandler>();
        private static IAudioHandler Audio(ActionDirector director) => director.GetRunner().Context.GetService<IAudioHandler>();
        private AudioSource Source() => _owner.GetComponentInChildren<AudioSource>();

        [TearDown]
        public void Cleanup()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
            foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [UnityTearDown]
        public IEnumerator ExitRuntime()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [Test]
        public void DelayedReturnCannotAffectReusedLeaseAndDuplicateReturnIsHarmless()
        {
            var director = Director();
            var handler = VFX(director);
            var prefab = Effect();
            var first = handler.Spawn(prefab, Vector3.zero, Quaternion.identity, _owner.transform);
            handler.ReturnVFXDelay(first, 0.1f);
            handler.Return(first);
            handler.Return(first);
            var second = handler.Spawn(prefab, Vector3.one, Quaternion.identity, _owner.transform);
            Assert.AreSame(first, second);
            director.Tick(0.2f);
            Assert.IsTrue(second.activeInHierarchy, "旧延迟归还不得回收新借用");
            var simultaneous = handler.Spawn(prefab, Vector3.zero, Quaternion.identity, _owner.transform);
            Assert.AreNotSame(second, simultaneous, "重复归还不能把同一对象借出两次");
        }

        [Test]
        public void DelayedReturnUsesActionTimeAndFreezesWhenPaused()
        {
            var director = Director();
            var handler = VFX(director);
            var instance = handler.Spawn(Effect(), Vector3.zero, Quaternion.identity, _owner.transform);
            handler.ReturnVFXDelay(instance, 1f);
            director.Speed = 0.5f;
            director.UpdatePresentation(1f);
            Assert.IsTrue(instance.activeSelf);
            director.Pause();
            director.UpdatePresentation(5f);
            Assert.IsTrue(instance.activeSelf);
            director.Resume();
            director.UpdatePresentation(1f);
            Assert.IsFalse(instance.activeSelf);
        }

        [Test]
        public void StopReturnsDetachedEffectsAndReplayReusesCache()
        {
            var director = Director();
            var timeline = director.Timeline;
            var old = VFX(director);
            var prefab = Effect();
            var first = old.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.IsNull(first.transform.parent);
            director.Stop();
            Assert.IsFalse(first.activeSelf);
            Assert.IsNull(old.Spawn(prefab, Vector3.zero, Quaternion.identity), "结束会话不能继续借对象");
            director.Play(timeline);
            var next = VFX(director).Spawn(prefab, Vector3.one, Quaternion.identity);
            Assert.AreSame(first, next);
        }

        [Test]
        public void NaturalCompletionReturnsUnfinishedTail()
        {
            var director = Director(0.2f);
            var handler = VFX(director);
            var instance = handler.Spawn(Effect(), Vector3.zero, Quaternion.identity);
            handler.ReturnVFXDelay(instance, 5f);
            director.Tick(0.3f);
            Assert.IsFalse(instance.activeSelf);
            Assert.IsFalse(director.IsPlaying);
        }

        [Test]
        public void CacheCapacityIsBoundedPerAsset()
        {
            var director = Director();
            var handler = VFX(director);
            var prefab = Effect();
            var instances = new List<GameObject>();
            for (int i = 0; i < 12; i++) instances.Add(handler.Spawn(prefab, Vector3.zero, Quaternion.identity));
            foreach (var instance in instances) handler.Return(instance);
            Assert.AreEqual(8, _owner.transform.Find("ActionSystem Pool").childCount);
        }

        [Test]
        public void ExternalPresentationServicesOverrideDefaultsWithoutTransferringOwnership()
        {
            var director = Director();
            director.Stop();
            var external = new BorrowedPresentation();
            director.ServiceProvider = (type, owner) => type == typeof(IVFXHandler) || type == typeof(IAudioHandler) ? external : null;
            director.Play(director.Timeline);
            Assert.AreSame(external, VFX(director));
            Assert.AreSame(external, Audio(director));
            director.Stop();
            Assert.AreEqual(0, external.DisposeCount);
            Assert.IsNull(_owner.transform.Find("ActionSystem Pool"));
        }

        [UnityTest]
        public IEnumerator Particle_PlayerLoopUsesPresentationClockAndConfiguredActivation()
        {
            yield return new EnterPlayMode();
            var director = Director();
            var prefab = Effect(true);
            prefab.AddComponent<ActionEffectActivationProbe>();
            var handler = VFX(director);
            var instance = handler.Spawn(prefab, new Vector3(3f, 4f, 5f), Quaternion.identity, _owner.transform, Vector3.one * 2f);
            var probe = instance.GetComponent<ActionEffectActivationProbe>();
            Assert.AreEqual(new Vector3(3f, 4f, 5f), probe.PositionAtEnable);
            Assert.AreEqual(Vector3.one * 2f, probe.ScaleAtEnable);
            var system = instance.GetComponent<ParticleSystem>();
            director.Tick(0.2f);
            Assert.That(system.time, Is.EqualTo(0f).Within(0.001f), "逻辑 Tick 不推进粒子显示");
            director.UpdatePresentation(0.2f);
            Assert.That(system.time, Is.EqualTo(0.2f).Within(0.001f));
            yield return null;
            Assert.That(system.time, Is.GreaterThan(0.2f), "PlayerLoop 独立推进粒子显示");
            float beforePause = system.time;
            director.Pause();
            director.Tick(1f);
            director.UpdatePresentation(1f);
            Assert.That(system.time, Is.EqualTo(beforePause).Within(0.001f));
            director.Resume();
            director.Speed = 0.5f;
            director.Tick(0.2f);
            director.UpdatePresentation(0.2f);
            Assert.That(system.time, Is.EqualTo(beforePause + 0.1f).Within(0.001f));
            handler.SetTime(instance, 0.8f);
            Assert.That(system.time, Is.EqualTo(0.8f).Within(0.001f));
            _owner.SetActive(false);
            yield return null;
            Assert.IsTrue(instance == null, "禁用宿主应销毁活跃对象和缓存");
        }

        [UnityTest]
        public IEnumerator Audio_PlayerLoopSupportsSpeedPauseReuseAndRejectsOldIds()
        {
            yield return new EnterPlayMode();
            var director = Director();
            var handler = Audio(director);
            var sound = Sound();
            int firstId = handler.PlaySound(sound, new AudioArgs
            { volume = 0.6f, pitch = 1f, affectedByPlaybackSpeed = true, parent = _owner.transform });
            var source = Source();
            Assert.IsNotNull(source);
            Assert.AreSame(sound, source.clip);
            director.Speed = 0.5f;
            Assert.That(source.pitch, Is.EqualTo(0.5f).Within(0.001f));
            director.Pause();
            Assert.IsFalse(source.isPlaying);
            director.Tick(5f);
            Assert.AreSame(sound, source.clip);
            director.Resume();
            Assert.IsTrue(source.isPlaying);
            handler.StopSound(firstId);
            int nextId = handler.PlaySound(sound, new AudioArgs
            { volume = 1f, pitch = 1f, parent = _owner.transform });
            Assert.AreSame(source, Source());
            Assert.AreNotEqual(firstId, nextId);
            Assert.That(source.pitch, Is.EqualTo(1f).Within(0.001f), "未配置速度同步的音效保持原音调");
            handler.StopSound(firstId);
            Assert.AreSame(sound, source.clip);
            director.UpdatePresentation(2.1f);
            Assert.IsNull(source.clip, "一次性音效结束后自动归还");
            Assert.IsFalse(source.gameObject.activeSelf);
            director.Stop();
            director.Play(director.Timeline);
            handler = Audio(director);
            int newSessionId = handler.PlaySound(sound, new AudioArgs { volume = 1f, pitch = 1f, parent = _owner.transform, loop = true });
            Assert.AreNotEqual(nextId, newSessionId);
            handler.StopSound(nextId);
            Assert.AreSame(sound, Source().clip);
            director.UpdatePresentation(5f);
            Assert.IsNotNull(Source(), "循环音效不会按一次性时长回收");
            director.Stop();
            Assert.IsFalse(source.gameObject.activeSelf);
            Assert.IsNull(source.clip);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TracksPlaySeekAndCompleteWithoutGameplayServices()
        {
            yield return new EnterPlayMode();
            var director = Director();
            director.Stop();
            var timeline = director.Timeline;
            _owner.transform.localScale = Vector3.one * 2f;
            var vfx = new VFXTrack();
            vfx.clips.Add(new VFXClip
            {
                effectPrefab = Effect(true), Duration = 1f, positionOffset = Vector3.right * 2f,
                scale = Vector3.one * 0.5f, stopEmissionOnEnd = true
            });
            var audio = new AudioTrack();
            audio.clips.Add(new AudioClip
            { audioClips = new List<UnityEngine.AudioClip> { Sound(4f) }, Duration = 1f, lifeControlByClip = false, isAffectSpeed = true });
            timeline.Groups[0].tracks.Add(vfx);
            timeline.Groups[0].tracks.Add(audio);
            TestTimelineBuilder.SetDuration(timeline, 1f);
            director.Play(timeline);
            var particle = _owner.GetComponentInChildren<ParticleSystem>();
            Assert.IsNotNull(particle);
            Assert.AreEqual(2f, particle.transform.localPosition.x);
            Assert.AreEqual(0.5f, particle.transform.localScale.x);
            Assert.IsNotNull(Source());
            director.Pause();
            director.Seek(0.5f);
            Assert.That(particle.time, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(Source().time, Is.EqualTo(0.5f).Within(0.02f));
            var source = Source();
            director.Resume();
            director.Tick(0.6f);
            Assert.IsFalse(director.IsPlaying);
            Assert.IsFalse(particle.gameObject.activeSelf);
            Assert.IsNull(source.clip, "会话完成也回收不受片段生命周期控制的音效");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ParticleReleaseWaitsForEmissionDurationInsteadOfGuessingLifetime()
        {
            yield return new EnterPlayMode();
            var director = Director();
            var prefab = Effect(true);
            var main = prefab.GetComponent<ParticleSystem>().main;
            main.startLifetime = 0.1f;
            var handler = VFX(director);
            var instance = handler.Spawn(prefab, Vector3.zero, Quaternion.identity, _owner.transform);
            handler.ReleaseWhenFinished(instance, false);
            director.UpdatePresentation(0.2f);
            Assert.IsTrue(instance.activeSelf, "单粒子寿命很短，但发射器仍在发射，不能据此提前回收");
            director.UpdatePresentation(5.1f);
            var system = instance.GetComponent<ParticleSystem>();
            Assert.IsFalse(instance.activeSelf, $"time={system.time}, particles={system.particleCount}, emitting={system.isEmitting}, loop={system.main.loop}");
            instance = handler.Spawn(prefab, Vector3.zero, Quaternion.identity, _owner.transform);
            director.UpdatePresentation(0.1f);
            handler.ReleaseWhenFinished(instance, true);
            director.UpdatePresentation(0.2f);
            Assert.IsFalse(instance.activeSelf, "停止发射后应等待存量粒子结束并归还");
        }

        [UnityTest]
        public IEnumerator DestroyingOwnerCleansDetachedInstances()
        {
            yield return new EnterPlayMode();
            var director = Director();
            var instance = VFX(director).Spawn(Effect(), Vector3.zero, Quaternion.identity);
            Object.Destroy(_owner);
            yield return null;
            yield return null;
            Assert.IsTrue(instance == null);
        }

        private sealed class BorrowedPresentation : IVFXHandler, IAudioHandler, IDisposable
        {
            public int DisposeCount;
            public void Dispose() => DisposeCount++;
            public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) => null;
            public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Vector3 scale) => null;
            public void SetTime(GameObject instance, float time) { }
            public void ReleaseWhenFinished(GameObject instance, bool stopEmittingImmediately) { }
            public void Return(GameObject instance) { }
            public void ReturnVFXDelay(GameObject instance, float delay) { }
            public int PlaySound(UnityEngine.AudioClip clip, AudioArgs args) => 1;
            public void StopSound(int id) { }
            public void UpdateSound(int id, float volume, float pitch, float time) { }
            public void PauseSound(int id) { }
            public void ResumeSound(int id) { }
            public void StopAll() { }
        }
    }
}
