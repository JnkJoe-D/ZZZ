using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 技能运行时处理进程使用的相机服务接口。
    /// </summary>
    public interface ICameraHandler : IService
    {
        void GenerateImpulse();
        void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration);

        // 动画相机 (CameraAnimationClip)
        GameObject CreateCamera(GameObject prefab);
        void DestroyCamera(GameObject cameraInstance);
        void PlayCameraTimeline(GameObject cameraInstance, CameraControlParams paramsObj);

        // 常规相机控制 (CameraControlClip)
        void LockCameraRotation(bool lockYaw, bool lockPitch);
        void UnlockCameraRotation();
        void StartRecenter(CameraRecenterTarget target, float smoothTime, float targetPitch, bool disableInput, float framingBiasAngle = -8.0f, float deadzoneAngle = 1.5f, bool allowSoftInput = true);
        void UpdateRecenter(float deltaTime);
        void StopRecenter(bool restoreInput);
        void StartLookAtTarget(Vector3 offset, float smoothSpeed, bool fallbackToCharacter);
        void UpdateLookAtTarget(float deltaTime);
        void StopLookAtTarget(bool restore);
        void SetCameraFOVAndDistance(float targetFOV, float targetDistance, float speed, bool instant = false);
        void ResetCameraFOVAndDistance(float speed);

        // 目标跟随控制 (CameraTargetClip)
        void SetCameraTargetAnchor(CameraTargetScope scope, Vector3 targetWorldPos, float weight, bool syncX, bool syncY, bool syncZ, Vector3 offset, CameraTargetSpace space, bool enableDamping, float smoothTime, float deltaTime);
        void RestoreCameraTargetAnchor(CameraTargetScope scope, float duration);
    }

    public class CameraControlParams
    {
        public UnityEngine.Playables.PlayableAsset timelineAsset;
        public string followBoneName;
        public string lookAtBoneName;
        public bool overrideSettings;
        public Color backgroundColor;
        public LayerMask cullingMask;
    }
}
