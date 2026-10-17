using System;
using System.Collections;
using System.Collections.Generic;
using ATEditor.Editor;
using Game.GamePlay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ATEditor.Test
{
    public class ActionAttackTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private readonly List<IAttackDetectionWindow> _windows = new List<IAttackDetectionWindow>();
        private GameObject _owner;
        private static readonly Vector3 Origin = new Vector3(1000f, 1000f, 1000f);
        private const int Mask = 1 << 31;

        private GameObject Create(string name, Vector3 offset)
        {
            var obj = new GameObject(name); obj.layer = 31; obj.transform.position = Origin + offset;
            _objects.Add(obj); return obj;
        }
        private Collider Target(string name, Vector3 offset, Transform parent = null)
        {
            var obj = Create(name, offset);
            if (parent != null) obj.transform.SetParent(parent, true);
            var collider = obj.AddComponent<SphereCollider>(); collider.radius = 0.1f;
            return collider;
        }
        private AttackClip Clip()
        {
            var clip = new AttackClip { Duration = 1f, layers = Mask };
            clip.scope.shape.radius = 2f; return clip;
        }
        private ProcessContext Context(IAttackTargetResolver resolver = null)
        {
            if (_owner == null) _owner = Create("Attacker", Vector3.zero);
            return new ProcessContext(_owner, PlayMode.Runtime, (type, owner) =>
                type == typeof(IAttackTargetResolver) ? resolver : null);
        }
        private IAttackDetectionWindow Open(AttackClip clip, IAttackTargetResolver resolver = null)
        {
            var window = new DefaultAttackDetectionHandler().OpenWindow(Context(resolver), clip);
            _windows.Add(window); return window;
        }
        private ActionDirector Director()
        {
            Context(); var director = _owner.AddComponent<ActionDirector>();
            director.UpdateAutomatically = false; return director;
        }
        private ActionTimeline Timeline(ClipBase clip, float duration = 1f)
        {
            var timeline = TestTimelineBuilder.Create(duration); _objects.Add(timeline);
            var track = new CombatTrack(); track.clips.Add(clip); timeline.Groups[0].tracks.Add(track);
            return timeline;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Dispose(); _windows.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear(); _owner = null;
        }
        [UnityTearDown]
        public IEnumerator ExitRuntime() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [Test]
        public void CompoundTargetsUseOneBudgetAndSceneSiblingsAreNotSelf()
        {
            Context();
            var sceneContainer = Create("SceneContainer", Vector3.zero);
            _owner.transform.SetParent(sceneContainer.transform, true);
            Target("OwnHurtBox", Vector3.forward * 0.1f, _owner.transform);
            var compound = Create("CompoundVictim", Vector3.forward * 0.7f);
            compound.transform.SetParent(sceneContainer.transform, true);
            var rb = compound.AddComponent<Rigidbody>(); rb.isKinematic = true;
            Target("Chest", Vector3.forward * 0.6f, compound.transform);
            Target("Head", Vector3.forward * 0.8f, compound.transform);
            var other = Target("OtherVictim", Vector3.forward * 1.2f);
            var clip = Clip(); clip.maxTargets = 2;
            var result = Open(clip).Sample(0f, 0);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(compound, result[0].Target);
            Assert.AreEqual(other.gameObject, result[1].Target);
        }

        [Test]
        public void ResolverFiltersBeforeQuotaAndDoesNotRequireGameplayTypes()
        {
            var wall = Target("Wall", Vector3.forward * 0.2f);
            var victim = Target("Victim", Vector3.forward);
            var clip = Clip(); clip.maxTargets = 1;
            var result = Open(clip, new FilterResolver(wall)).Sample(0f, 0);
            Assert.AreEqual(1, result.Count); Assert.AreEqual(victim, result[0].Collider);
        }

        [Test]
        public void MoreThanSixtyFourCollidersAreNotSilentlyTruncated()
        {
            for (int i = 0; i < 100; i++) Target("Victim " + i, Vector3.forward);
            Assert.AreEqual(100, Open(Clip()).Sample(0f, 0).Count);
        }

        [TestCase(AttackRepeatMode.OncePerWindow, 1)]
        [TestCase(AttackRepeatMode.OncePerSample, 3)]
        [TestCase(AttackRepeatMode.Cooldown, 2)]
        public void RepeatPolicyUsesActionTimeAndCannotRepeatASample(AttackRepeatMode mode, int expected)
        {
            Target("Victim", Vector3.forward);
            var clip = Clip(); clip.repeatMode = mode; clip.cooldown = 0.5f;
            var window = Open(clip);
            int count = window.Sample(0f, 0).Count;
            Assert.AreEqual(0, window.Sample(0f, 0).Count);
            count += window.Sample(0.25f, 1).Count;
            count += window.Sample(0.5f, 2).Count;
            Assert.AreEqual(expected, count);
        }

        [Test]
        public void DistinctTargetBudgetAllowsKnownTargetRepeatsButNotNewTargets()
        {
            var a = Target("A", Vector3.forward * 0.5f);
            var b = Target("B", Vector3.forward);
            var clip = Clip(); clip.repeatMode = AttackRepeatMode.OncePerSample; clip.maxTargets = 1;
            var window = Open(clip);
            Assert.AreEqual(a, window.Sample(0f, 0)[0].Collider);
            a.transform.position = Origin + Vector3.forward * 1.5f;
            b.transform.position = Origin + Vector3.forward * 0.2f;
            Assert.AreEqual(a, window.Sample(0.25f, 1)[0].Collider);
        }

        [TestCase(HitBoxType.Sphere)]
        [TestCase(HitBoxType.Box)]
        [TestCase(HitBoxType.Capsule)]
        [TestCase(HitBoxType.Sector)]
        [TestCase(HitBoxType.Ring)]
        public void ShapesAcceptInsideAndRejectOutsideTargets(HitBoxType type)
        {
            var clip = Clip(); var shape = clip.scope.shape;
            shape.shapeType = type; shape.size = Vector3.one * 2f; shape.angle = 60f; shape.innerRadius = 1f;
            Vector3 inside = Vector3.forward * 1.4f, outside = Vector3.forward * 3f;
            if (type == HitBoxType.Box) inside = Vector3.right * 0.7f;
            if (type == HitBoxType.Capsule)
            { shape.radius = 0.4f; shape.height = 4f; inside = Vector3.up * 1.5f; outside = Vector3.right; }
            if (type == HitBoxType.Sector) outside = Vector3.right * 1.4f;
            if (type == HitBoxType.Ring) outside = Vector3.forward * 0.2f;
            var accepted = Target("Inside", inside); Target("Outside", outside);
            var result = Open(clip).Sample(0f, 0);
            Assert.AreEqual(1, result.Count); Assert.AreEqual(accepted, result[0].Collider);
        }

        [Test]
        public void TriggerPolicyIsExplicitAndDoesNotDependOnGlobalSetting()
        {
            var target = Target("Trigger", Vector3.forward); target.isTrigger = true;
            bool previous = Physics.queriesHitTriggers;
            try
            {
                Physics.queriesHitTriggers = false;
                var clip = Clip(); Assert.AreEqual(1, Open(clip).Sample(0f, 0).Count);
                clip.triggers = QueryTriggerInteraction.Ignore;
                Assert.AreEqual(0, Open(clip).Sample(0f, 0).Count);
            }
            finally { Physics.queriesHitTriggers = previous; }
        }

        [Test]
        public void FrozenPoseRemainsAtEntryWhileFollowingPoseMoves()
        {
            Context(); Target("Victim", Vector3.forward);
            var clip = Clip(); clip.scope.hitBoxFollowMode = HitBoxFollowMode.None;
            var frozen = Open(clip);
            clip.scope.hitBoxFollowMode = HitBoxFollowMode.Both;
            var following = Open(clip);
            _owner.transform.position += Vector3.right * 10f;
            Assert.AreEqual(1, frozen.Sample(0f, 0).Count);
            Assert.AreEqual(0, following.Sample(0f, 0).Count);
        }

        [Test]
        public void SamplesAreStableSnapshotsAndWindowsDoNotShareMutableBuffers()
        {
            Target("A", Vector3.forward);
            var clip = Clip(); clip.repeatMode = AttackRepeatMode.OncePerSample;
            var first = Open(clip); var second = Open(clip);
            var snapshot = first.Sample(0f, 0);
            Assert.AreEqual(1, second.Sample(0f, 0).Count);
            Assert.AreNotEqual(snapshot[0].WindowId, second.Sample(0.2f, 1)[0].WindowId);
            first.Dispose(); Assert.AreEqual(0, first.Sample(0.4f, 2).Count);
            Assert.AreEqual(1, snapshot.Count); Assert.IsNotNull(snapshot[0].Collider);
        }

        [Test]
        public void LargeDirectorTickSamplesAfterMovementRegardlessOfTrackOrder()
        {
            var director = Director(); Target("Victim", Vector3.forward);
            var clip = Clip(); clip.scope.shape.radius = 10f; clip.sampleCount = 4;
            clip.repeatMode = AttackRepeatMode.OncePerSample;
            var timeline = Timeline(clip);
            var motion = new TransformTrack();
            motion.clips.Add(new MovementClip { Duration = 1f, targetPosition = Vector3.forward * 4f, faceTargetOnArrival = false });
            timeline.Groups[0].tracks.Add(motion); // 故意将运动放到检测之后。
            var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
            director.Play(timeline); director.Tick(1f);
            Assert.AreEqual(4, results.Count);
            for (int i = 0; i < results.Count; i++)
            {
                Assert.That(results[i].TimelineTime, Is.EqualTo(i * 0.25f).Within(0.0001f));
                Assert.That(results[i].QueryCenter.z - Origin.z, Is.EqualTo(i).Within(0.001f));
                Assert.AreEqual(i, results[i].SampleIndex);
            }
            Assert.IsFalse(director.IsPlaying);
        }

        [Test]
        public void SeekDoesNotAttackAndResumeDoesNotReplayHistoricalSamples()
        {
            var director = Director(); Target("Victim", Vector3.forward);
            var clip = Clip(); clip.StartTime = 0.5f; clip.sampleCount = 4; clip.repeatMode = AttackRepeatMode.OncePerSample;
            var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
            director.Play(Timeline(clip, 2f)); director.Pause(); director.Seek(1f); director.Tick(1f);
            Assert.AreEqual(0, results.Count);
            director.Resume(); director.Tick(0.25f);
            Assert.AreEqual(1, results.Count); Assert.AreEqual(3, results[0].SampleIndex);
            director.Seek(0.6f); director.Tick(0.7f);
            Assert.AreEqual(1, results.Count, "向后定位不重放已消费的攻击窗口");
        }

        [Test]
        public void ReplayAndLoopHaveUnambiguousPlaybackAndWindowIdentity()
        {
            var director = Director(); Target("Victim", Vector3.forward);
            var clip = Clip(); clip.Duration = 0.5f;
            var timeline = Timeline(clip); timeline.isLoop = true;
            var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
            director.Play(timeline); director.Tick(1f);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(results[0].PlaybackId, results[1].PlaybackId);
            Assert.AreNotEqual(results[0].WindowId, results[1].WindowId);
            director.Play(timeline);
            Assert.AreNotEqual(results[0].PlaybackId, results[2].PlaybackId);
            Assert.AreEqual(clip.clipId, results[2].SourceClipId);
        }

        [Test]
        public void StopFromObservationCancelsRemainingNotifications()
        {
            var director = Director(); Target("A", Vector3.forward * 0.5f); Target("B", Vector3.forward);
            int count = 0; director.AttackObserved += _ => { count++; director.Stop(); };
            director.Play(Timeline(Clip()));
            Assert.AreEqual(1, count); Assert.IsFalse(director.IsPlaying);
            director.Tick(1f); Assert.AreEqual(1, count);
        }

        [Test]
        public void ExternalObservationHandlerReplacesDefaultEventAndBindsZZZEffectsExplicitly()
        {
            var director = Director(); Target("Victim", Vector3.forward);
            var clip = Clip(); var combat = new RecordingCombat(); int defaultEvents = 0;
            var bridge = new ATAttackObservationHandler(observation => observation.SourceClipId == clip.clipId
                ? new DetectConfig { hitEffectId = 42, multiHitCount = 5, hitStopDuration = 0.12f } : null, combat);
            director.ServiceProvider = (type, owner) => type == typeof(IAttackObservationHandler) ? bridge : null;
            director.AttackObserved += _ => defaultEvents++;
            director.Play(Timeline(clip));
            Assert.AreEqual(0, defaultEvents); Assert.AreEqual(1, combat.Hits.Count);
            Assert.AreEqual(42, combat.Hits[0].hitEffectId);
            Assert.AreEqual(5, combat.Hits[0].multiHitCount);
            Assert.AreEqual(0.12f, combat.Hits[0].hitStopDuration);
            director.Play(Timeline(Clip()));
            Assert.AreEqual(1, combat.Hits.Count, "未知片段不能推断配表效果");
        }

        [Test]
        public void LegacyHitClipSeekDoesNotInvokeCombatOrRewriteEffectBindings()
        {
            var director = Director(); Target("Victim", Vector3.forward);
            var combat = new RecordingCombat();
            director.ServiceProvider = (type, owner) => type == typeof(IHitHandler) ? combat : null;
            var clip = new HitClip { StartTime = 0.5f, Duration = 1f, hitLayerMask = Mask,
                detects = new[] { new DetectConfig { hitEffectId = 73 } } };
            director.Play(Timeline(clip, 2f)); director.Seek(0.6f);
            Assert.AreEqual(0, combat.Hits.Count); Assert.AreEqual(73, clip.detects[0].hitEffectId);
            director.Stop(); clip.StartTime = 0f; director.Play(Timeline(clip, 2f));
            Assert.AreEqual(1, combat.Hits.Count); Assert.AreEqual(73, combat.Hits[0].hitEffectId);
        }

        [UnityTest]
        public IEnumerator AnimationPoseIsFlushedBeforeFreezingDetectionWindow()
        {
            yield return new EnterPlayMode();
            ValidateAnimatedWeaponPose();
            yield return null;
        }
        private void ValidateAnimatedWeaponPose()
        {
            var director = Director(); _owner.AddComponent<Animator>();
            var weapon = Create("Weapon", Vector3.forward * 10f);
            weapon.transform.SetParent(_owner.transform, true);
            Target("Victim", Vector3.zero);
            var clip = Clip(); clip.StartTime = 0.5f; clip.Duration = 0.5f;
            clip.scope.bindPoint = BindPoint.CustomBone; clip.scope.customBoneName = "Weapon";
            clip.scope.hitBoxFollowMode = HitBoxFollowMode.None; clip.scope.shape.radius = 0.2f;
            var timeline = Timeline(clip);
            var animation = new UnityEngine.AnimationClip(); _objects.Add(animation);
            animation.SetCurve("Weapon", typeof(Transform), "localPosition.z", UnityEngine.AnimationCurve.Linear(0f, 0f, 0.5f, 1f));
            var track = new AnimationTrack { isMasterTrack = true };
            track.clips.Add(new AnimationClip { StartTime = 0.5f, Duration = 0.5f, animationClip = animation, BlendInDuration = 0f });
            timeline.Groups[0].tracks.Add(track);
            var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
            director.Play(timeline); director.Tick(0.5f);
            Assert.AreEqual(1, results.Count, "不能在动画起始姿态求值前冻结旧武器位置");
            Assert.That(Vector3.Distance(Origin, results[0].QueryCenter), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator ExternalDetectionBackendOwnsServiceWhileProcessReleasesEachWindow()
        {
            yield return new EnterPlayMode();
            ValidateExternalWindowOwnership();
            yield return null;
        }
        private void ValidateExternalWindowOwnership()
        {
            var director = Director(); var backend = new RecordingDetection();
            var clip = Clip(); clip.sampleCount = 4;
            director.ServiceProvider = (type, owner) => type == typeof(IAttackDetectionHandler) ? backend : null;
            var timeline = Timeline(clip);
            director.Play(timeline); director.Tick(0.26f); director.Stop(); director.Stop();
            Assert.AreEqual(1, backend.Windows.Count);
            Assert.AreEqual(2, backend.Windows[0].Samples);
            Assert.AreEqual(1, backend.Windows[0].Disposals);
            director.Play(timeline); _owner.SetActive(false);
            Assert.AreEqual(2, backend.Windows.Count); Assert.AreEqual(1, backend.Windows[1].Disposals);
        }

        [UnityTest]
        public IEnumerator NativePlayModeDetectsMovedTargetsAndReleasesOnDisable()
        {
            yield return new EnterPlayMode();
            ValidateNativePlayback();
            yield return null;
        }
        private void ValidateNativePlayback()
        {
            var director = Director(); var target = Target("MovingVictim", Vector3.forward * 5f);
            var clip = Clip(); clip.sampleCount = 4; clip.repeatMode = AttackRepeatMode.OncePerSample;
            var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
            director.Play(Timeline(clip)); Assert.AreEqual(0, results.Count);
            target.transform.position = Origin + Vector3.forward; director.Tick(0.3f);
            Assert.AreEqual(1, results.Count, "无需等待 FixedUpdate，也能查询最新 Transform");
            director.Pause(); director.Tick(1f); Assert.AreEqual(1, results.Count);
            _owner.SetActive(false); director.Tick(1f);
            Assert.IsFalse(director.IsPlaying); Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void AttackSampleReloadsAndRunsWithoutZZZServices()
        {
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/__ATEditorAttackTest");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            try
            {
                ActionDirectorSampleMenu.CreateAttackAssets(folder);
                AssetDatabase.ImportAsset(folder + "/IndependentAttack.asset", ImportAssetOptions.ForceUpdate);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/IndependentAttack.prefab");
                Assert.IsNotNull(prefab.GetComponent<AttackDetectionSample>());
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); _objects.Add(instance);
                instance.transform.position = Origin; // 隔离打开的编辑器预览对象及其碰撞体。
                var director = instance.GetComponentInChildren<ActionDirector>(); director.UpdateAutomatically = false;
                var results = new List<AttackObservation>(); director.AttackObserved += results.Add;
                director.Play(); director.Tick(2f);
                Assert.AreEqual(2, results.Count, Describe(results));
                Assert.AreNotEqual(results[0].Target, results[1].Target);
                foreach (var component in instance.GetComponentsInChildren<Component>())
                    StringAssert.DoesNotStartWith("Game.", component.GetType().Namespace ?? "");
                Object.DestroyImmediate(instance);
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        private static string Describe(List<AttackObservation> results)
        {
            var text = new System.Text.StringBuilder();
            foreach (var result in results)
                text.Append($"sample={result.SampleIndex}, time={result.TimelineTime}, center={result.QueryCenter}, target={result.Target}; ");
            return text.ToString();
        }

        private sealed class RecordingDetection : IAttackDetectionHandler
        {
            public readonly List<RecordingWindow> Windows = new List<RecordingWindow>();
            public IAttackDetectionWindow OpenWindow(ProcessContext context, AttackClip clip)
            { var window = new RecordingWindow(); Windows.Add(window); return window; }
        }
        private sealed class RecordingWindow : IAttackDetectionWindow
        {
            public int Samples, Disposals;
            public IReadOnlyList<AttackObservation> Sample(float time, int index)
            { Samples++; return Array.Empty<AttackObservation>(); }
            public void Dispose() => Disposals++;
        }
        private sealed class FilterResolver : IAttackTargetResolver
        {
            private readonly Collider _wall;
            public FilterResolver(Collider wall) { _wall = wall; }
            public Object ResolveTarget(GameObject owner, Collider collider) => collider == _wall ? null : collider.gameObject;
        }
        private sealed class RecordingCombat : IHitHandler
        {
            public readonly List<HitData> Hits = new List<HitData>();
            public void OnHitDetect(HitData data) => Hits.Add(data);
        }
    }
}
