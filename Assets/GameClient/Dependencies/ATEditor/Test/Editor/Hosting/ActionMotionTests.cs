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
    public class ActionMotionTests
    {
        private GameObject _owner;
        private readonly List<Object> _assets = new List<Object>();

        private ActionDirector Director()
        {
            _owner = new GameObject("IndependentMotion");
            var director = _owner.AddComponent<ActionDirector>();
            director.UpdateAutomatically = false;
            director.Play(Timeline());
            return director;
        }
        private ActionTimeline Timeline(float duration = 2f)
        {
            var timeline = TestTimelineBuilder.Create(duration);
            _assets.Add(timeline);
            return timeline;
        }
        private static DefaultRootMotionHandler Motion(ActionDirector director) => (DefaultRootMotionHandler)director.GetRunner().Context.GetService<IMotionWindowHandler>();

        [TearDown]
        public void Cleanup()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
            foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
            _assets.Clear();
        }
        [UnityTearDown]
        public IEnumerator ExitRuntime()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [Test]
        public void ExtremelyLargeLoopStepFailsInsteadOfHanging()
        {
            var director = Director();
            director.Timeline.isLoop = true;
            Assert.Throws<ArgumentOutOfRangeException>(() => director.Tick(float.MaxValue));
        }

        [Test]
        public void RejectedAnimatorOwnershipDoesNotAlterExistingBinding()
        {
            var director = Director();
            director.Stop();
            var visual = new GameObject("SharedAnimator");
            visual.transform.SetParent(_owner.transform, false);
            var animator = visual.AddComponent<Animator>();
            var other = visual.AddComponent<ActionDirector>();
            other.UpdateAutomatically = false;
            var timeline = NativeTimeline();
            director.Play(timeline);
            Assert.IsTrue(animator.applyRootMotion);
            Assert.Throws<InvalidOperationException>(() => other.Play(timeline));
            Assert.IsTrue(animator.applyRootMotion, "失败装配不能恢复/覆盖另一个宿主的状态");
            Assert.IsNull(visual.GetComponent<MAnimSystem.AnimComponent>(), "所有权校验失败前不能创建第二份图");
            director.Stop();
            Assert.IsFalse(animator.applyRootMotion);
        }

        [Test]
        public void LocalFilterPreservesHeightAndDoesNotRescaleSampledDelta()
        {
            var director = Director();
            _owner.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            _owner.transform.localScale = Vector3.one * 3f;
            director.Speed = 0.5f;
            var motion = Motion(director);
            motion.EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode.ZeroLocalX);
            motion.ApplyRootMotion(new Vector3(2f, 1f, 3f), Quaternion.identity);
            Assert.That(Vector3.Distance(new Vector3(2f, 1f, 0f), _owner.transform.position), Is.LessThan(0.001f));
        }

        [Test]
        public void OverlappingWindowsReleaseOnlyTheirOwnRegistration()
        {
            var director = Director();
            var motion = Motion(director);
            motion.EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode.ZeroLocalX);
            motion.EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode.ZeroLocalZ);
            motion.DisableLocalDeltaFilter();
            motion.ApplyRootMotion(Vector3.one, Quaternion.identity);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), _owner.transform.position);
            motion.DisableLocalDeltaFilter();
            motion.ApplyRootMotion(Vector3.one, Quaternion.identity);
            Assert.AreEqual(new Vector3(2f, 2f, 1f), _owner.transform.position);
        }

        [Test]
        public void RotationLimitsJitterAndInterruptedWindowDoesNotAlign()
        {
            var director = Director();
            var motion = Motion(director);
            motion.EnableRotationFilter(new RotationFilterClip { maxAccumulatedAngle = 90f });
            motion.ApplyRootMotion(Vector3.zero, Quaternion.Euler(20f, 60f, 10f));
            motion.ApplyRootMotion(Vector3.zero, Quaternion.Euler(0f, -15f, 0f));
            motion.ApplyRootMotion(Vector3.zero, Quaternion.Euler(0f, 60f, 0f));
            motion.IsSeeking = true;
            motion.DisableRotationFilter();
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
            director.Tick(0.2f);
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void CompletedRotationWindowBlendsUsingActionTime()
        {
            var director = Director();
            var motion = Motion(director);
            motion.EnableRotationFilter(new RotationFilterClip { targetRelativeYaw = 90f, alignBlendDuration = 0.2f });
            motion.DisableRotationFilter();
            director.Speed = 0.5f;
            motion.Tick(0.1f);
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 45f, 0f)), Is.LessThan(0.01f));
            director.Pause();
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 45f, 0f)), Is.LessThan(0.01f));
            director.Resume();
            motion.Tick(0.1f);
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void SeekAndStopDoNotApplyRotationExitAlignment()
        {
            var director = Director();
            director.Stop();
            var timeline = Timeline();
            var track = new MotionWindowTrack();
            track.clips.Add(new RotationFilterClip { Duration = 0.5f, targetRelativeYaw = 180f, alignBlendDuration = 0f });
            timeline.Groups[0].tracks.Add(track);
            director.Play(timeline);
            director.Seek(1f);
            Assert.That(Quaternion.Angle(Quaternion.identity, _owner.transform.rotation), Is.LessThan(0.01f));
            director.Play(timeline);
            director.Stop();
            Assert.That(Quaternion.Angle(Quaternion.identity, _owner.transform.rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void NaturalCompletionFinalizesExitAlignmentBeforeReleasingSession()
        {
            var director = Director();
            director.Stop();
            var timeline = Timeline(0.5f);
            var track = new MotionWindowTrack();
            track.clips.Add(new RotationFilterClip { Duration = 0.5f, targetRelativeYaw = 90f, alignBlendDuration = 0.2f });
            timeline.Groups[0].tracks.Add(track);
            director.Play(timeline);
            director.Tick(0.5f);
            Assert.IsFalse(director.IsPlaying);
            Assert.That(Quaternion.Angle(_owner.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void MovementTrackReachesEndAndSupportsPausedSeek()
        {
            var director = Director();
            director.Stop();
            var timeline = Timeline(1f);
            var track = new TransformTrack();
            track.clips.Add(new MovementClip { Duration = 1f, targetPosition = Vector3.forward * 4f, faceTargetOnArrival = false });
            timeline.Groups[0].tracks.Add(track);
            director.Play(timeline);
            director.Tick(0.25f);
            Assert.That(_owner.transform.position.z, Is.EqualTo(1f).Within(0.001f));
            director.Pause(); director.Seek(0.5f);
            Assert.That(_owner.transform.position.z, Is.EqualTo(2f).Within(0.001f));
            director.Resume(); director.Tick(0.5f);
            Assert.That(_owner.transform.position.z, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void RotationOnceAtExitUsesBoundInputOnlyAtExit()
        {
            var director = Director();
            director.Stop();
            var transformHandler = new DefaultTransformHandler(_owner.transform) { InputDirection = Vector3.right };
            director.ServiceProvider = (type, owner) => type == typeof(ITransformHandler) ? transformHandler : null;
            var timeline = Timeline();
            var track = new TransformTrack();
            track.clips.Add(new RotationClip { Duration = 0.5f, updateFrequency = UpdateFrequency.OnceAtExit, rotationMode = RotationMode.Immediate });
            timeline.Groups[0].tracks.Add(track);
            director.Play(timeline);
            director.Tick(0.2f);
            Assert.That(Quaternion.Angle(Quaternion.identity, _owner.transform.rotation), Is.LessThan(0.01f));
            director.Tick(0.3f);
            Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), _owner.transform.rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void StopAtObstacleUsesControllerAndIgnoresSelfCollider()
        {
            var director = Director();
            _owner.transform.position = Vector3.up * 1000f;
            director.Stop();
            var controller = _owner.AddComponent<CharacterController>();
            controller.center = Vector3.up; controller.height = 2f; controller.radius = 0.5f;
            director.Play(director.Timeline);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _assets.Add(wall);
            wall.transform.position = new Vector3(0f, 1001f, 2f);
            wall.transform.localScale = new Vector3(4f, 2f, 0.2f);
            Physics.SyncTransforms();
            var motion = Motion(director);
            motion.EnableCollisionMode(RootMotionCollisionMode.StopAtObstacle, ~0);
            motion.ApplyRootMotion(Vector3.forward * 5f, Quaternion.identity);
            Assert.That(_owner.transform.position.z, Is.GreaterThan(0.5f).And.LessThan(1.5f));
        }

        private ActionDirector NativeDirector(out Animator animator)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameClient/Dependencies/ATEditor/Editor/Resources/DefaultPreviewCharacter.prefab");
            Assert.IsNotNull(prefab);
            _owner = Object.Instantiate(prefab);
            animator = _owner.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _owner.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var director = _owner.AddComponent<ActionDirector>();
            director.UpdateAutomatically = false;
            return director;
        }

        private ActionTimeline NativeTimeline(bool window = false)
        {
            var timeline = Timeline(1f);
            var animation = new UnityEngine.AnimationClip { name = "MemoryRootMotion" };
            _assets.Add(animation);
            animation.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            animation.SetCurve("", typeof(Animator), "RootT.y", AnimationCurve.Constant(0f, 1f, 0f));
            animation.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Constant(0f, 1f, 0f));
            animation.SetCurve("", typeof(Animator), "RootQ.x", AnimationCurve.Constant(0f, 1f, 0f));
            animation.SetCurve("", typeof(Animator), "RootQ.y", AnimationCurve.Constant(0f, 1f, 0f));
            animation.SetCurve("", typeof(Animator), "RootQ.z", AnimationCurve.Constant(0f, 1f, 0f));
            animation.SetCurve("", typeof(Animator), "RootQ.w", AnimationCurve.Constant(0f, 1f, 1f));
            var track = new AnimationTrack { isMasterTrack = true };
            track.clips.Add(new AnimationClip { animationClip = animation, Duration = 1f, BlendInDuration = 0f });
            timeline.Groups[0].tracks.Add(track);
            if (window)
            {
                var filters = new MotionWindowTrack();
                filters.clips.Add(new MotionFilterClip { StartTime = 0.1f, Duration = 0.1f, localDeltaFilterMode = MotionWindowLocalDeltaFilterMode.ZeroLocalXZ });
                timeline.Groups[0].tracks.Add(filters);
            }
            return timeline;
        }

        [UnityTest]
        public IEnumerator NativeRootMotion_PlayerLoopUsesSingleClockAndWindowBoundaries()
        {
            yield return new EnterPlayMode();
            var director = NativeDirector(out var animator);
            bool originalRootMotion = animator.applyRootMotion;
            var timeline = NativeTimeline();
            director.Play(timeline);
            director.Tick(0.3f);
            float baseline = _owner.transform.position.x;
            Assert.That(baseline, Is.GreaterThan(0.05f), "真实 Animator 根运动必须移动宿主");
            yield return null;
            Assert.That(_owner.transform.position.x, Is.EqualTo(baseline).Within(0.001f));
            director.Stop();
            Assert.AreEqual(originalRootMotion, animator.applyRootMotion);
            Assert.That(_owner.transform.position.x, Is.EqualTo(baseline).Within(0.001f), "停止不能重置已经提交的根位移");
            _owner.transform.position = Vector3.zero;
            director.Play(NativeTimeline(true));
            director.Tick(0.3f);
            Assert.That(_owner.transform.position.x / baseline, Is.EqualTo(2f / 3f).Within(0.06f), "跨越短过滤窗口时必须分别采样被过滤与未过滤区间");
            director.Pause();
            var position = _owner.transform.position;
            director.Tick(0.2f);
            Assert.That(Vector3.Distance(position, _owner.transform.position), Is.LessThan(0.001f));
            director.Seek(0.7f);
            Assert.That(Vector3.Distance(position, _owner.transform.position), Is.LessThan(0.001f), "Seek 采样不得提交伪根运动");
            director.Resume();
            director.Stop();
            _owner.transform.position = Vector3.zero;
            director.Speed = 0.5f;
            director.Play(timeline);
            director.Tick(0.6f);
            Assert.That(_owner.transform.position.x, Is.EqualTo(baseline).Within(0.01f), "动画采样已缩放，位移不得再次乘倍率");
            director.Stop();
            _owner.transform.position = Vector3.zero;
            director.ApplyRootMotion = false;
            director.Play(timeline);
            director.Tick(0.2f);
            Assert.That(_owner.transform.position.sqrMagnitude, Is.LessThan(0.000001f));
        }

        [UnityTest]
        public IEnumerator NativeRootMotionCanBeOverriddenWithoutMovingDefaultTransform()
        {
            yield return new EnterPlayMode();
            var director = NativeDirector(out _);
            var external = ConfigureBorrowedMotion(director);
            director.Play(NativeTimeline());
            director.Tick(0.2f);
            Assert.That(external.Distance, Is.GreaterThan(0.01f));
            Assert.That(_owner.transform.position.sqrMagnitude, Is.LessThan(0.000001f));
            director.Stop();
            Assert.IsFalse(external.Disposed);
        }

        // 在进入 Play Mode 后创建闭包，避免 Unity 恢复测试枚举器时丢失跨域的编译器显示类。
        private static BorrowedMotion ConfigureBorrowedMotion(ActionDirector director)
        {
            var external = new BorrowedMotion();
            director.ServiceProvider = (type, owner) => type == typeof(IMotionWindowHandler) ? external : null;
            return external;
        }

        private sealed class BorrowedMotion : IMotionWindowHandler, IDisposable
        {
            public float Distance;
            public bool Disposed;
            public void ApplyRootMotion(Vector3 delta, Quaternion rotation) => Distance += delta.magnitude;
            public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode) { }
            public void DisableLocalDeltaFilter() { }
            public void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask) { }
            public void DisableCollisionMode() { }
            public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode) { }
            public void DisableVisualOffset() { }
            public void EnableVisualOffsetRecover(float speed) { }
            public void DisableVisualOffsetRecover() { }
            public void EnableRotationFilter(RotationFilterClip clipData) { }
            public void DisableRotationFilter() { }
            public void EnableVisualRotationOffset(VisualRotationOffsetClip clipData) { }
            public void UpdateVisualRotationOffset(float normalizedTime) { }
            public void DisableVisualRotationOffset() { }
            public void Dispose() => Disposed = true;
        }
    }
}
