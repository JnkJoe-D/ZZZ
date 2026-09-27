using System;
using NUnit.Framework;
using UnityEngine;

namespace ATEditor.Test
{
    [TestFixture]
    public class CameraTargetClipTests
    {
        private GameObject _ownerGo;
        private GameObject _boneGo;
        private MockCameraHandler _mockCameraHandler;
        private ProcessContext _context;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("TestOwner_CameraTarget");
            _boneGo = new GameObject("Bip001 Head");
            _boneGo.transform.SetParent(_ownerGo.transform);
            _boneGo.transform.localPosition = new Vector3(0, 1.6f, 0);

            _mockCameraHandler = new MockCameraHandler();
            _context = new ProcessContext(_ownerGo, PlayMode.Runtime, (type, go) =>
            {
                if (type == typeof(ICameraHandler)) return _mockCameraHandler;
                return null;
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_ownerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_ownerGo);
            }
        }

        [Test]
        public void CameraTargetClip_DefaultValues_AreConsistentWithSpec()
        {
            var clip = new CameraTargetClip();

            Assert.AreEqual("相机目标跟随", clip.clipName);
            Assert.AreEqual(CameraTargetScope.Both, clip.targetScope);
            Assert.AreEqual(BindPoint.Bip001_Head, clip.bindPoint);
            Assert.AreEqual(CameraTargetSpace.CharacterLocal, clip.referenceSpace);
            Assert.AreEqual(Vector3.zero, clip.positionOffset);

            Assert.IsTrue(clip.syncX);
            Assert.IsTrue(clip.syncY);
            Assert.IsTrue(clip.syncZ);

            Assert.IsTrue(clip.SupportsBlending);
            Assert.AreEqual(0.15f, clip.BlendInDuration, 0.001f);
            Assert.AreEqual(0.2f, clip.BlendOutDuration, 0.001f);
            Assert.IsNotNull(clip.blendInCurve);
            Assert.IsNotNull(clip.blendOutCurve);

            Assert.IsTrue(clip.enableDamping);
            Assert.AreEqual(0.08f, clip.smoothTime, 0.001f);

            Assert.IsTrue(clip.restoreOnExit);
            Assert.IsTrue(clip.restoreOnStop);
            Assert.AreEqual(0.2f, clip.interruptRestoreDuration, 0.001f);
        }

        [Test]
        public void CameraTargetClip_Clone_CreatesDeepCopy()
        {
            var clip = new CameraTargetClip
            {
                targetScope = CameraTargetScope.FollowOnly,
                bindPoint = BindPoint.CustomBone,
                customBoneName = "Weapon_Root",
                referenceSpace = CameraTargetSpace.World,
                positionOffset = new Vector3(1, 2, 3),
                syncX = false,
                syncY = true,
                syncZ = false,
                BlendInDuration = 0.3f,
                BlendOutDuration = 0.4f,
                enableDamping = false,
                smoothTime = 0.15f,
                restoreOnExit = false,
                restoreOnStop = true,
                interruptRestoreDuration = 0.5f
            };

            var clone = (CameraTargetClip)clip.Clone();

            Assert.AreNotEqual(clip.clipId, clone.clipId);
            Assert.AreEqual(clip.targetScope, clone.targetScope);
            Assert.AreEqual(clip.bindPoint, clone.bindPoint);
            Assert.AreEqual(clip.customBoneName, clone.customBoneName);
            Assert.AreEqual(clip.referenceSpace, clone.referenceSpace);
            Assert.AreEqual(clip.positionOffset, clone.positionOffset);
            Assert.AreEqual(clip.syncX, clone.syncX);
            Assert.AreEqual(clip.syncY, clone.syncY);
            Assert.AreEqual(clip.syncZ, clone.syncZ);
            Assert.AreEqual(clip.BlendInDuration, clone.BlendInDuration, 0.001f);
            Assert.AreEqual(clip.BlendOutDuration, clone.BlendOutDuration, 0.001f);
            Assert.AreEqual(clip.enableDamping, clone.enableDamping);
            Assert.AreEqual(clip.smoothTime, clone.smoothTime, 0.001f);
            Assert.AreEqual(clip.restoreOnExit, clone.restoreOnExit);
            Assert.AreEqual(clip.restoreOnStop, clone.restoreOnStop);
            Assert.AreEqual(clip.interruptRestoreDuration, clone.interruptRestoreDuration, 0.001f);

            // 验证曲线是独立副本
            Assert.AreNotSame(clip.blendInCurve, clone.blendInCurve);
            Assert.AreNotSame(clip.blendOutCurve, clone.blendOutCurve);
        }

        [Test]
        public void RuntimeCameraTargetProcess_Update_CallsHandlerWithTargetAnchor()
        {
            var process = new RuntimeCameraTargetProcess();
            var clip = new CameraTargetClip
            {
                StartTime = 0.5f,
                Duration = 1.0f,
                bindPoint = BindPoint.Bip001_Head,
                targetScope = CameraTargetScope.Both,
                positionOffset = new Vector3(0, 0.2f, 0),
                syncX = true,
                syncY = false,
                syncZ = true,
                BlendInDuration = 0.2f,
                BlendOutDuration = 0.2f
            };

            process.Initialize(clip, _context);
            process.OnEnter();

            // 采样在 0.7s (elapsed = 0.2s，刚好达到 100% 权重)
            process.OnUpdate(0.7f, 0.033f);

            Assert.AreEqual(1, _mockCameraHandler.SetAnchorCalls.Count);
            var call = _mockCameraHandler.SetAnchorCalls[0];
            Assert.AreEqual(CameraTargetScope.Both, call.Scope);
            Assert.IsTrue(call.SyncX);
            Assert.IsFalse(call.SyncY);
            Assert.IsTrue(call.SyncZ);
            Assert.AreEqual(new Vector3(0, 0.2f, 0), call.Offset);
            Assert.AreEqual(1.0f, call.Weight, 0.01f);
            Assert.AreEqual(0.033f, call.DeltaTime, 0.001f);
        }

        [Test]
        public void RuntimeCameraTargetProcess_OnExit_RespectsRestoreFlag()
        {
            var process = new RuntimeCameraTargetProcess();
            var clip = new CameraTargetClip
            {
                StartTime = 0f,
                Duration = 1f,
                targetScope = CameraTargetScope.FollowOnly,
                BlendOutDuration = 0.25f,
                restoreOnExit = true
            };

            process.Initialize(clip, _context);
            process.OnEnter();
            process.OnExit();

            Assert.AreEqual(1, _mockCameraHandler.RestoreAnchorCalls.Count);
            Assert.AreEqual(CameraTargetScope.FollowOnly, _mockCameraHandler.RestoreAnchorCalls[0].Scope);
            Assert.AreEqual(0.25f, _mockCameraHandler.RestoreAnchorCalls[0].Duration, 0.001f);

            // 若 restoreOnExit 为 false，OnExit 不应触发恢复
            _mockCameraHandler.RestoreAnchorCalls.Clear();
            clip.restoreOnExit = false;
            process.OnExit();
            Assert.AreEqual(0, _mockCameraHandler.RestoreAnchorCalls.Count);
        }

        [Test]
        public void RuntimeCameraTargetProcess_OnStop_RespectsRestoreOnStop()
        {
            var process = new RuntimeCameraTargetProcess();
            var clip = new CameraTargetClip
            {
                StartTime = 0f,
                Duration = 1f,
                targetScope = CameraTargetScope.LookAtOnly,
                interruptRestoreDuration = 0.15f,
                restoreOnStop = true
            };

            process.Initialize(clip, _context);
            process.OnEnter();
            process.OnStop();

            Assert.AreEqual(1, _mockCameraHandler.RestoreAnchorCalls.Count);
            Assert.AreEqual(CameraTargetScope.LookAtOnly, _mockCameraHandler.RestoreAnchorCalls[0].Scope);
            Assert.AreEqual(0.15f, _mockCameraHandler.RestoreAnchorCalls[0].Duration, 0.001f);

            // 若 restoreOnStop 为 false，OnStop 不应触发恢复
            _mockCameraHandler.RestoreAnchorCalls.Clear();
            clip.restoreOnStop = false;
            process.OnStop();
            Assert.AreEqual(0, _mockCameraHandler.RestoreAnchorCalls.Count);
        }
    }
}
