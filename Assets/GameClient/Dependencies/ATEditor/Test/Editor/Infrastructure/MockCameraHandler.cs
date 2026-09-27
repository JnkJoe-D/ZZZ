using System.Collections.Generic;
using UnityEngine;

namespace ATEditor.Test
{
    public class MockCameraHandler : ICameraHandler
    {
        public struct TargetAnchorCall
        {
            public CameraTargetScope Scope;
            public Vector3 TargetWorldPos;
            public float Weight;
            public bool SyncX;
            public bool SyncY;
            public bool SyncZ;
            public Vector3 Offset;
            public CameraTargetSpace Space;
            public bool EnableDamping;
            public float SmoothTime;
            public float DeltaTime;
        }

        public struct RestoreAnchorCall
        {
            public CameraTargetScope Scope;
            public float Duration;
        }

        public readonly List<TargetAnchorCall> SetAnchorCalls = new List<TargetAnchorCall>();
        public readonly List<RestoreAnchorCall> RestoreAnchorCalls = new List<RestoreAnchorCall>();

        public void Initialize() { }

        public void GenerateImpulse() { }
        public void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration) { }

        public GameObject CreateCamera(GameObject prefab) => null;
        public void DestroyCamera(GameObject cameraInstance) { }
        public void PlayCameraTimeline(GameObject cameraInstance, CameraControlParams paramsObj) { }

        public void LockCameraRotation(bool lockYaw, bool lockPitch) { }
        public void UnlockCameraRotation() { }
        public void StartRecenter(CameraRecenterTarget target, float smoothTime, float targetPitch, bool disableInput, float framingBiasAngle = -8.0f, float deadzoneAngle = 1.5f, bool allowSoftInput = true) { }
        public void UpdateRecenter(float deltaTime) { }
        public void StopRecenter(bool restoreInput) { }
        public void StartLookAtTarget(Vector3 offset, float smoothSpeed, bool fallbackToCharacter) { }
        public void UpdateLookAtTarget(float deltaTime) { }
        public void StopLookAtTarget(bool restore) { }
        public void SetCameraFOVAndDistance(float targetFOV, float targetDistance, float speed, bool instant = false) { }
        public void ResetCameraFOVAndDistance(float speed) { }

        public void SetCameraTargetAnchor(CameraTargetScope scope, Vector3 targetWorldPos, float weight, bool syncX, bool syncY, bool syncZ, Vector3 offset, CameraTargetSpace space, bool enableDamping, float smoothTime, float deltaTime)
        {
            SetAnchorCalls.Add(new TargetAnchorCall
            {
                Scope = scope,
                TargetWorldPos = targetWorldPos,
                Weight = weight,
                SyncX = syncX,
                SyncY = syncY,
                SyncZ = syncZ,
                Offset = offset,
                Space = space,
                EnableDamping = enableDamping,
                SmoothTime = smoothTime,
                DeltaTime = deltaTime
            });
        }

        public void RestoreCameraTargetAnchor(CameraTargetScope scope, float duration)
        {
            RestoreAnchorCalls.Add(new RestoreAnchorCall
            {
                Scope = scope,
                Duration = duration
            });
        }
    }
}
