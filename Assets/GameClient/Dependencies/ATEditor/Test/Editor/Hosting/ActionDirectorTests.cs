using System;
using System.Collections;
using System.Collections.Generic;
using MAnimSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ATEditor.Test
{
    public class ActionDirectorTests
    {
        private GameObject _owner;
        private readonly List<Object> _assets = new List<Object>();

        private ActionDirector CreateDirector()
        {
            _owner = new GameObject("IsolatedActionDirector");
            var director = _owner.AddComponent<ActionDirector>();
            director.UpdateAutomatically = false;
            return director;
        }

        private ActionTimeline Timeline(float duration = 2f)
        {
            var timeline = TestTimelineBuilder.Create(duration);
            _assets.Add(timeline);
            return timeline;
        }

        private ActionTimeline AnimatedTimeline(float start = 0f, float duration = 2f)
        {
            var timeline = Timeline(start + duration);
            if (_owner.transform.Find("Visual") == null)
            {
                var visual = new GameObject("Visual");
                visual.transform.SetParent(_owner.transform, false);
            }
            var animation = new UnityEngine.AnimationClip { name = "MemoryMotion" };
            animation.SetCurve("Visual", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 2f, 2f));
            _assets.Add(animation);
            var track = new AnimationTrack { isMasterTrack = true };
            track.clips.Add(new AnimationClip
            {
                animationClip = animation, StartTime = start, Duration = duration, BlendInDuration = 0f
            });
            timeline.Groups[0].tracks.Add(track);
            return timeline;
        }

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
        public void ReplayCreatesFreshSessionAndClearsOldContext()
        {
            var director = CreateDirector();
            var timeline = Timeline();
            Assert.IsTrue(director.Play(timeline));
            var first = director.GetRunner();
            first.Context.Flags.Add("OldState");
            int cleanups = 0;
            first.Context.RegisterCleanup(() => cleanups++);
            director.Tick(0.2f);
            Assert.IsTrue(director.Play(timeline));
            Assert.AreNotSame(first, director.GetRunner());
            Assert.AreNotSame(first.Context, director.GetRunner().Context);
            Assert.IsEmpty(director.GetRunner().Context.Flags);
            Assert.AreEqual(1, cleanups);
            Assert.AreEqual(0f, director.CurrentTime);
        }

        [Test]
        public void ExternalAnimationWinsAndRemainsOwnedByCaller()
        {
            var director = CreateDirector();
            var backend = new BorrowedAnimation();
            director.ServiceProvider = (type, owner) => type == typeof(IAnimationHandler) ? backend : null;
            Assert.IsTrue(director.Play(AnimatedTimeline()));
            Assert.AreEqual(1, backend.PlayCount);
            Assert.IsNull(_owner.GetComponent<AnimComponent>(), "外部覆盖不能创建默认动画图");
            director.Stop();
            Assert.AreEqual(0, backend.DisposeCount);
        }

        [Test]
        public void MissingAnimatorFailsAndReleasesSession()
        {
            var director = CreateDirector();
            Assert.Throws<InvalidOperationException>(() => director.Play(AnimatedTimeline()));
            Assert.IsFalse(director.IsPlaying);
            Assert.IsNull(director.GetRunner());
            Assert.IsTrue(director.Play(Timeline()), "失败后仍可正常播放不含动画的时间轴");
        }

        [Test]
        public void CompletionCallbackCanStartNextSessionWithoutOldTickAdvancingIt()
        {
            var director = CreateDirector();
            var first = Timeline(0.2f);
            var next = Timeline();
            int completions = 0, interruptions = 0;
            director.Interrupted += _ => interruptions++;
            director.Completed += _ => { completions++; Assert.IsTrue(director.Play(next)); };
            director.Play(first);
            director.Tick(0.3f);
            Assert.AreEqual(1, completions);
            Assert.AreEqual(0, interruptions);
            Assert.AreSame(next, director.GetRunner().Timeline);
            Assert.AreEqual(0f, director.CurrentTime);
        }

        [Test]
        public void InterruptionCallbackNewRequestTakesPrecedence()
        {
            var director = CreateDirector();
            var first = Timeline();
            var requested = Timeline();
            var callbackChoice = Timeline();
            director.Play(first);
            Action<ActionTimeline> handler = null;
            handler = _ =>
            {
                director.Interrupted -= handler;
                Assert.IsTrue(director.Play(callbackChoice));
            };
            director.Interrupted += handler;
            Assert.IsFalse(director.Play(requested));
            Assert.AreSame(callbackChoice, director.GetRunner().Timeline);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CleanupCannotNestPlaybackAndPublicNotificationCan(bool complete)
        {
            var director = CreateDirector();
            var next = Timeline();
            director.Play(Timeline(0.2f));
            int cleanups = 0;
            director.GetRunner().Context.RegisterCleanup(() =>
            {
                cleanups++;
                Assert.IsFalse(director.Play(next));
            });
            if (complete) director.Completed += _ => Assert.IsTrue(director.Play(next));
            else
            {
                Action<ActionTimeline> handler = null;
                handler = _ => { director.Interrupted -= handler; Assert.IsTrue(director.Play(next)); };
                director.Interrupted += handler;
            }
            if (complete) director.Tick(0.3f);
            else director.Stop();
            Assert.AreEqual(1, cleanups);
            Assert.AreSame(next, director.GetRunner().Timeline);
        }

        [Test]
        public void StopRequestedFromProcessTickIsAppliedAfterDispatch()
        {
            var director = CreateDirector();
            var timeline = Timeline();
            timeline.AddMockClip(0f, 2f);
            director.Play(timeline);
            var process = (MockProcess)director.GetRunner().ActiveProcesses[0].process;
            process.CustomOnUpdateAction = (_, __, ___) => director.Stop();
            director.Tick(0.1f);
            Assert.IsFalse(director.IsPlaying);
            Assert.IsNull(director.GetRunner());
        }

        [UnityTest]
        public IEnumerator DefaultAnimation_PlayerLoopUsesOneClockAndReleasesGraph()
        {
            yield return new EnterPlayMode();
            var director = CreateDirector();
            _owner.AddComponent<Animator>();
            var timeline = AnimatedTimeline();
            Assert.IsTrue(director.Play(timeline));
            var animation = _owner.GetComponent<AnimComponent>();
            Assert.IsNotNull(animation);
            Assert.AreEqual(DirectorUpdateMode.Manual, animation.Graph.GetTimeUpdateMode());
            director.Tick(0.25f);
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0.25d).Within(0.001));
            Assert.That(_owner.transform.Find("Visual").localPosition.x, Is.EqualTo(0.25f).Within(0.001f), "动画必须真正驱动场景对象");
            yield return null;
            // 手动一次推进到 0.25，显示时钟首帧先追到该逻辑锚点；随后独立连续推进。
            float firstDisplayTime = animation.GetCurrentState().Time;
            yield return null;
            Assert.That(animation.GetCurrentState().Time, Is.GreaterThan(firstDisplayTime), "显示按渲染帧继续采样");
            Assert.That(director.CurrentTime, Is.EqualTo(0.25f).Within(0.001f), "显示不得推进逻辑时间");
            float pausedTime = animation.GetCurrentState().Time;
            director.Pause();
            director.Tick(0.2f);
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(pausedTime).Within(0.001));
            director.Resume();
            director.Speed = 0.5f;
            director.Tick(0.2f);
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0.35d).Within(0.001));
            Assert.That(_owner.transform.Find("Visual").localPosition.x, Is.EqualTo(0.35f).Within(0.001f));
            director.Seek(0.75f);
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0.75d).Within(0.001));
            director.Stop();
            Assert.IsFalse(animation.Graph.IsValid());
            Assert.IsTrue(director.Play(timeline));
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0d).Within(0.001));
            _owner.SetActive(false);
            Assert.IsFalse(director.IsPlaying);
            Assert.IsFalse(animation.Graph.IsValid());
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SampleCreatesReloadablePrefabWithoutGameplayDependencies(bool presentation, bool motion)
        {
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/__ATEditorDirectorTest");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            try
            {
                ActionDirectorSampleMenu.CreateAssets(folder, presentation, motion);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/IndependentAction.prefab");
                Assert.IsNotNull(prefab);
                var director = prefab.GetComponent<ActionDirector>();
                Assert.IsNotNull(director.Timeline);
                Assert.AreEqual(2f, director.Timeline.Duration);
                var track = (AnimationTrack)director.Timeline.Groups[0].tracks[0];
                Assert.IsTrue(track.isMasterTrack);
                Assert.IsNotNull(((AnimationClip)track.clips[0]).animationClip);
                if (presentation)
                {
                    Assert.IsNotNull(((VFXClip)director.Timeline.Groups[0].tracks[1].clips[0]).effectPrefab);
                    Assert.IsNotNull(((AudioClip)director.Timeline.Groups[0].tracks[2].clips[0]).audioClips[0]);
                }
                if (motion)
                    Assert.IsInstanceOf<MovementClip>(director.Timeline.Groups[0].tracks[3].clips[0]);
                foreach (var component in prefab.GetComponentsInChildren<Component>())
                {
                    Assert.IsNotNull(component, "样例不能包含缺失脚本");
                    StringAssert.DoesNotStartWith("Game.", component.GetType().Namespace ?? "");
                }
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        [UnityTest]
        public IEnumerator LateAnimationEntryDoesNotApplyFrameDeltaTwice()
        {
            yield return new EnterPlayMode();
            var director = CreateDirector();
            _owner.AddComponent<Animator>();
            director.Play(AnimatedTimeline(0.5f));
            director.Tick(0.6f);
            var animation = _owner.GetComponent<AnimComponent>();
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0.1d).Within(0.001));
            director.Tick(0.1f);
            Assert.That(animation.GetCurrentState().Time, Is.EqualTo(0.2d).Within(0.001));
        }

        private sealed class BorrowedAnimation : IAnimationHandler, IDisposable
        {
            public int PlayCount;
            public int DisposeCount;
            public void Initialize() { }
            public void Dispose() => DisposeCount++;
            public void PlayAnimation(UnityEngine.AnimationClip clip, int layer, float fade, float speed, float startTime = 0f) => PlayCount++;
            public void SetLayerMask(int layer, AvatarMask mask) { }
            public AvatarMask GetLayerMask(int layer) => null;
            public void SetLayerSpeed(int layer, float speed) { }
            public void SetTime(int layer, float time) { }
        }
    }
}
