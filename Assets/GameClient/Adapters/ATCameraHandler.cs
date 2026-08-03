using System.Collections.Generic;
using Cinemachine;
using Game.Camera;
using Game.Resource;
using ATEditor;
using UnityEngine;
using Game.Logic;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Adapters
{
    /// <summary>
    /// Adapter used by skill runtime camera-related processes.
    /// </summary>
    public class ATCameraHandler : ICameraHandler
    {
        private CharacterEntity _entity;
        public ATCameraHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        
        public void GenerateImpulse()
        {
            _entity?.CameraController?.GenerateImpulse();
        }

        public void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration)
        {
            _entity?.CameraController?.GenerateImpulseWithVelocity(velocity, force ,duration);
        }

        public GameObject CreateCamera(GameObject prefab)
        {
            return _entity?.CameraController?.CreateCamera(prefab);
        }

        public void DestroyCamera(GameObject cameraInstance)
        {
            _entity?.CameraController?.DestroyCamera(cameraInstance);
        }

        public void PlayCameraTimeline(GameObject cameraInstance, CameraControlParams paramsObj)
        {
            _entity?.CameraController?.PlayCameraTimeline(cameraInstance, paramsObj);
        }

        public void LockCameraRotation(bool lockYaw, bool lockPitch)
        {
            _entity?.CameraController?.LockRotation(lockYaw, lockPitch);
        }

        public void UnlockCameraRotation()
        {
            _entity?.CameraController?.UnlockRotation();
        }

        public void StartRecenter(CameraRecenterTarget target, float smoothTime, float targetPitch, bool disableInput, float framingBiasAngle = -8.0f, float deadzoneAngle = 1.5f, bool allowSoftInput = true)
        {
            _entity?.CameraController?.StartRecenter(target, smoothTime, targetPitch, disableInput, framingBiasAngle, deadzoneAngle, allowSoftInput);
        }

        public void UpdateRecenter(float deltaTime)
        {
            _entity?.CameraController?.UpdateRecenter(deltaTime);
        }

        public void StopRecenter(bool restoreInput)
        {
            _entity?.CameraController?.StopRecenter(restoreInput);
        }

        public void StartLookAtTarget(Vector3 offset, float smoothSpeed, bool fallbackToCharacter)
        {
            _entity?.CameraController?.StartLookAtTarget(offset, smoothSpeed, fallbackToCharacter);
        }

        public void UpdateLookAtTarget(float deltaTime)
        {
            _entity?.CameraController?.UpdateLookAtTarget(deltaTime);
        }

        public void StopLookAtTarget(bool restore)
        {
            _entity?.CameraController?.StopLookAtTarget(restore);
        }

        public void SetCameraFOVAndDistance(float targetFOV, float targetDistance, float speed)
        {
            _entity?.CameraController?.SetCameraFOVAndDistance(targetFOV, targetDistance, speed);
        }

        public void ResetCameraFOVAndDistance(float speed)
        {
            _entity?.CameraController?.ResetCameraFOVAndDistance(speed);
        }
    }
}
