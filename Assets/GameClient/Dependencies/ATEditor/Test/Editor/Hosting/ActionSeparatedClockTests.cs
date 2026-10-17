using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ATEditor.Test
{
    public sealed class ActionSeparatedClockTests
    {
        private readonly List<Object> assets = new List<Object>();
        private readonly List<GameObject> actors = new List<GameObject>();
        [TearDown]
        public void Cleanup()
        {
            foreach (var actor in actors) if (actor != null) Object.DestroyImmediate(actor);
            foreach (var asset in assets) if (asset != null) Object.DestroyImmediate(asset);
            actors.Clear(); assets.Clear();
        }
        [UnityTearDown]
        public IEnumerator ExitRuntime() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        private ActionDirector Director(bool nativeAvatar = false)
        {
            var owner = nativeAvatar ? Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameClient/Dependencies/ATEditor/Editor/Resources/DefaultPreviewCharacter.prefab")) : new GameObject("SeparatedClock");
            actors.Add(owner);
            if (!nativeAvatar)
            {
                owner.AddComponent<Animator>();
                new GameObject("Visual").transform.SetParent(owner.transform, false);
            }
            owner.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var director = owner.AddComponent<ActionDirector>(); director.UpdateAutomatically = false;
            return director;
        }
        private ActionTimeline Timeline(float duration = 2f)
        {
            var timeline = TestTimelineBuilder.Create(duration); assets.Add(timeline); return timeline;
        }
        private UnityEngine.AnimationClip Animation(bool root = false, float speed = 1f)
        {
            var clip = new UnityEngine.AnimationClip(); assets.Add(clip);
            if (!root) clip.SetCurve("Visual", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 2f, 2f));
            else
            {
                clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0f, 0f, 2f, speed * 2f));
                foreach (string axis in new[] { "y", "z" }) clip.SetCurve("", typeof(Animator), "RootT." + axis, AnimationCurve.Constant(0f, 2f, 0f));
                foreach (string axis in new[] { "x", "y", "z", "w" }) clip.SetCurve("", typeof(Animator), "RootQ." + axis, AnimationCurve.Constant(0f, 2f, axis == "w" ? 1f : 0f));
            }
            return clip;
        }
        private void AddAnimation(ActionTimeline timeline, UnityEngine.AnimationClip clip, float start = 0f, float fade = 0f)
        {
            var track = new AnimationTrack { isMasterTrack = true };
            track.clips.Add(new AnimationClip { animationClip = clip, StartTime = start, Duration = 2f - start, BlendInDuration = fade });
            timeline.Groups[0].tracks.Add(track);
        }

        [UnityTest]
        public IEnumerator RenderRatesDoNotChangeLogicPoseOrAttackSamples()
        {
            yield return new EnterPlayMode();
            foreach (int rate in new[] { 30, 60, 144 }) CheckRate(rate);
        }
        private void CheckRate(int rate)
        {
            var director = Director();
            var timeline = Timeline(); AddAnimation(timeline, Animation());
            var attacks = new CombatTrack(); attacks.clips.Add(new AttackClip { StartTime = 0.15f, Duration = 0.1f, sampleCount = 4 });
            timeline.Groups[0].tracks.Add(attacks);
            var detector = new PoseRecorder();
            director.ServiceProvider = (type, owner) => type == typeof(IAttackDetectionHandler) ? detector : null;
            director.Play(timeline);
            var visual = director.transform.Find("Visual");
            const double step = 1d / 60d;
            double accumulator = 0, elapsed = 0;
            for (int frame = 0; frame < rate / 2; frame++)
            {
                double dt = 1d / rate; elapsed += dt; accumulator += dt;
                while (accumulator + 1e-10 >= step)
                {
                    director.Tick((float)step); accumulator -= step;
                    Assert.That(visual.localPosition.x, Is.EqualTo(director.CurrentTime).Within(0.0002f));
                }
                float logicTime = director.CurrentTime; int samples = detector.Times.Count;
                director.UpdatePresentation((float)dt);
                Assert.That(director.CurrentTime, Is.EqualTo(logicTime));
                Assert.That(detector.Times.Count, Is.EqualTo(samples), "显示更新不得执行检测");
                Assert.That(visual.localPosition.x, Is.EqualTo(elapsed).Within(0.0003f), "显示使用独立连续采样时间");
            }
            Assert.That(director.CurrentTime, Is.EqualTo(0.5f).Within(0.0002f));
            Assert.That(detector.Times.Count, Is.EqualTo(4));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(detector.Times[i], Is.EqualTo(0.15f + i * 0.025f).Within(0.0002f));
                Assert.That(detector.Poses[i], Is.EqualTo(detector.Times[i]).Within(0.0002f), "攻击读到对应逻辑姿态，不能读上次显示姿态");
            }
        }

        [UnityTest]
        public IEnumerator RootMotionAndBlendAreIdenticalWithInterleavedPresentation()
        {
            yield return new EnterPlayMode();
            var timeline = Timeline(); AddAnimation(timeline, Animation(true)); AddAnimation(timeline, Animation(true, 2f), 0.25f, 0.15f);
            var filters = new MotionWindowTrack();
            filters.clips.Add(new MotionFilterClip { StartTime = 0.1f, Duration = 0.1f, localDeltaFilterMode = MotionWindowLocalDeltaFilterMode.ZeroLocalXZ });
            timeline.Groups[0].tracks.Add(filters);
            var baseline = Director(true); baseline.Play(timeline);
            var mixed = Director(true); mixed.Play(timeline);
            for (int i = 0; i < 36; i++)
            {
                baseline.Tick(1f / 60f); mixed.Tick(1f / 60f);
                Assert.That(Vector3.Distance(mixed.transform.position, baseline.transform.position), Is.LessThan(0.0003f));
                Vector3 position = mixed.transform.position;
                for (int sample = 0; sample < 3; sample++) mixed.UpdatePresentation(1f / 180f);
                Assert.That(mixed.transform.position, Is.EqualTo(position), "显示求值禁止额外提交根位移");
            }
            Assert.That(baseline.transform.position.magnitude, Is.GreaterThan(0.1f));
            Vector3 beforeSeek = mixed.transform.position;
            mixed.Seek(0.8f); mixed.UpdatePresentation(0.01f);
            Assert.That(mixed.transform.position, Is.EqualTo(beforeSeek));
        }

        [UnityTest]
        public IEnumerator ClockChangesPauseParticlesAndAudioWithoutAnyLogicTick()
        {
            yield return new EnterPlayMode();
            var director = Director(); var timeline = Timeline(4f);
            var effect = new GameObject("ClockEffect"); effect.SetActive(false); assets.Add(effect);
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main; main.simulationSpeed = 2f; main.duration = 4f; main.loop = false;
            var vfx = new VFXTrack(); vfx.clips.Add(new VFXClip { effectPrefab = effect, Duration = 4f });
            var sound = UnityEngine.AudioClip.Create("ClockSound", 44100 * 4, 1, 44100, false); assets.Add(sound);
            var audio = new AudioTrack(); audio.clips.Add(new AudioClip { audioClips = new List<UnityEngine.AudioClip> { sound }, Duration = 4f, isAffectSpeed = true });
            timeline.Groups[0].tracks.Add(vfx); timeline.Groups[0].tracks.Add(audio);
            director.PresentationTimeScale = 0f; director.Play(timeline);
            var system = director.GetComponentInChildren<ParticleSystem>();
            var source = director.GetComponentInChildren<AudioSource>();
            Assert.That(source.pitch, Is.EqualTo(0f));
            director.PresentationTimeScale = 0.5f;
            Assert.That(source.pitch, Is.EqualTo(0.5f));
            director.Speed = 2f; Assert.That(source.pitch, Is.EqualTo(1f)); director.Speed = 1f;
            director.UpdatePresentation(0.2f);
            Assert.That(system.time, Is.EqualTo(0.2f).Within(0.002f));
            Assert.That(director.CurrentTime, Is.EqualTo(0f));
            director.Pause(); director.UpdatePresentation(0.4f);
            Assert.That(system.time, Is.EqualTo(0.2f).Within(0.002f)); Assert.IsFalse(source.isPlaying);
            director.PresentationTimeScale = 0.1f; director.Resume(); director.UpdatePresentation(0.2f);
            Assert.That(system.time, Is.EqualTo(0.24f).Within(0.002f)); Assert.That(source.pitch, Is.EqualTo(0.1f));
            director.Tick(0.1f);
            Assert.That(director.CurrentTime, Is.EqualTo(0.1f).Within(0.0001f), "表现倍率不能再次乘入调用方授权的逻辑 dt");
            Assert.That(system.time, Is.EqualTo(0.24f).Within(0.002f));
        }

        [Test]
        public void ClockAwareExternalBackendReceivesLocalSpeedAndClockSeparately()
        {
            var director = Director(); var timeline = Timeline(); AddAnimation(timeline, Animation());
            var backend = new ExternalAnimation();
            director.ServiceProvider = (type, owner) => type == typeof(IAnimationHandler) ? backend : null;
            director.PresentationTimeScale = 0.5f; director.Play(timeline);
            Assert.That(backend.Speed, Is.EqualTo(0.5f));
            director.Speed = 2f; Assert.That(backend.Speed, Is.EqualTo(1f));
            director.Pause(); Assert.That(backend.Speed, Is.EqualTo(0f)); director.Resume(); Assert.That(backend.Speed, Is.EqualTo(1f));
            Assert.IsNull(director.GetComponent<MAnimSystem.AnimComponent>());
        }

        [Test]
        public void DirectRunnerRetainsPauseAcrossClockChangeAndResetsItOnReplay()
        {
            var director = Director(); var timeline = Timeline(); AddAnimation(timeline, Animation());
            var backend = new ExternalAnimation();
            var context = new ProcessContext(director.gameObject, PlayMode.Runtime);
            context.AddService<IAnimationHandler>(backend);
            var runner = new ActionRunner(PlayMode.Runtime);
            try
            {
                runner.Play(timeline, context); runner.Pause();
                context.PresentationPlaySpeed = 0.5f;
                Assert.That(backend.Speed, Is.EqualTo(0f), "倍率变更不能取消旧入口的 Runner 暂停");
                runner.Resume(); Assert.That(backend.Speed, Is.EqualTo(0.5f));
                runner.Pause(); runner.Play(timeline, context);
                Assert.That(backend.Speed, Is.EqualTo(0.5f), "复用上下文的新播放必须重置暂停状态");
            }
            finally { runner.Stop(); }
        }

        [UnityTest]
        public IEnumerator RealEllenPresentationUsesPlayerLoopWithoutAuthorizingLogicOrRootMotion()
        {
            yield return new EnterPlayMode();
            var source = AssetDatabase.LoadAssetAtPath<Game.GamePlay.RoleActionConfigAsset>(
                "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/Ellen_Action_Attack_Normal_01_01.asset");
            var owner = new GameObject("EllenDisplayProbe"); owner.SetActive(false); actors.Add(owner);
            var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Prefab/Role/Ellen/Avatar_Female_Size02_Ellen.prefab"), owner.transform);
            var scripts = visual.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = scripts.Length - 1; i >= 0; i--) if (scripts[i] != null) Object.DestroyImmediate(scripts[i]);
            var animator = visual.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var timeline = Timeline(5f);
            var track = source.actionTimelineSO.GetMasterAnimationTrack();
            AddAnimation(timeline, ((AnimationClip)track.clips[0]).animationClip);
            var director = owner.AddComponent<ActionDirector>(); director.UpdateAutomatically = false; owner.SetActive(true);
            director.Play(timeline); director.Tick(0.1f);
            Vector3 position = owner.transform.position;
            var cameraObject = new GameObject("DisplayProbeCamera"); actors.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            var bounds = new Bounds(visual.transform.position + Vector3.up, Vector3.one);
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            camera.transform.position = bounds.center + new Vector3(3f, 1f, 5f);
            camera.transform.LookAt(bounds.center); camera.orthographic = true; camera.orthographicSize = bounds.extents.magnitude * 0.8f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.18f, 0.2f, 0.24f);
            var lightObject = new GameObject("DisplayProbeLight"); actors.Add(lightObject);
            lightObject.AddComponent<Light>().type = LightType.Directional; lightObject.transform.rotation = Quaternion.Euler(30f, 30f, 0f);
            var texture = new RenderTexture(256, 320, 24); assets.Add(texture); camera.targetTexture = texture;
            var strip = new Texture2D(768, 320, TextureFormat.RGB24, false); assets.Add(strip);
            float firstTime = -1f;
            for (int frame = 0; frame < 25; frame++)
            {
                yield return null;
                Assert.That(director.CurrentTime, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(owner.transform.position, Is.EqualTo(position));
                float displayed = owner.GetComponent<MAnimSystem.AnimComponent>().GetCurrentState().Time;
                if (frame == 0) firstTime = displayed;
                if (frame == 24) Assert.That(displayed, Is.GreaterThan(firstTime), "真实 PlayerLoop 必须推进显示，逻辑时间保持不变");
                if (frame % 12 != 0) continue;
                camera.Render();
                var previous = RenderTexture.active;
                try { RenderTexture.active = texture; strip.ReadPixels(new Rect(0, 0, 256, 320), frame / 12 * 256, 0); }
                finally { RenderTexture.active = previous; }
            }
            strip.Apply();
            System.IO.File.WriteAllBytes("docs/实施证据/Plan02/ellen-playerloop.png", strip.EncodeToPNG());
            camera.targetTexture = null;
        }

        private sealed class PoseRecorder : IAttackDetectionHandler
        {
            public readonly List<float> Times = new List<float>(), Poses = new List<float>();
            public IAttackDetectionWindow OpenWindow(ProcessContext context, AttackClip clip) => new Window(this, context);
            private sealed class Window : IAttackDetectionWindow
            {
                private readonly PoseRecorder owner; private readonly ProcessContext context;
                public Window(PoseRecorder owner, ProcessContext context) { this.owner = owner; this.context = context; }
                public IReadOnlyList<AttackObservation> Sample(float time, int index)
                { owner.Times.Add(time); owner.Poses.Add(context.Owner.transform.Find("Visual").localPosition.x); return Array.Empty<AttackObservation>(); }
                public void Dispose() { }
            }
        }
        private sealed class ExternalAnimation : IAnimationHandler
        {
            public float Speed;
            public void Initialize() { }
            public void PlayAnimation(UnityEngine.AnimationClip clip, int layer, float fade, float speed, float startTime = 0f) => Speed = speed;
            public void SetLayerSpeed(int layer, float speed) => Speed = speed;
            public void SetTime(int layer, float time) { }
            public void SetLayerMask(int layer, AvatarMask mask) { }
            public AvatarMask GetLayerMask(int layer) => null;
        }
    }
}
