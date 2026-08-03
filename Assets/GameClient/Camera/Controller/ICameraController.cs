using Game.Logic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Game.Camera
{
    /// <summary>
    /// 脱水的、独立于各角色的专属相机控制接口
    /// 在业务逻辑中，不直接操作 Cinemachine 或 Camera.main
    /// 只通过它获取对齐向量，以及通知它开启和关闭自由旋转输入
    /// </summary>
    public interface ICameraController
    {
        void Init(CharacterEntity entity);
        /// <summary>
        /// 冻结/解冻相机视角的旋转输入（例如进入 UI 或者释放某些固定视角的锁敌技能时禁用）
        /// </summary>
        void EnableInput(bool enable);
        void SetCameraActive(bool active);
        void SnapToPose(Vector3 position, Quaternion rotation);

        /// <summary>
        /// 提供目标当前的视觉主前向向量（只取水平 XZ 分量并正规化）
        /// 供移动系统和地面 FSM 推算实际挪动方向
        /// </summary>
        Vector3 GetForward();

        /// <summary>
        /// 提供目标当前的视觉主右向向量（只取水平 XZ 分量并正规化）
        /// </summary>
        Vector3 GetRight();
        void GenerateImpulse();
        void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration);

        GameObject CreateCamera(GameObject prefab);
        void DestroyCamera(GameObject cameraInstance);
        void PlayCameraTimeline(GameObject cameraInstance, ATEditor.CameraControlParams paramsObj);

        // 常规相机控制
        void LockRotation(bool lockYaw, bool lockPitch);
        void UnlockRotation();
        void StartRecenter(ATEditor.CameraRecenterTarget target, float smoothTime, float targetPitch, bool disableInput, float framingBiasAngle = -8.0f, float deadzoneAngle = 1.5f, bool allowSoftInput = true);
        void UpdateRecenter(float deltaTime);
        void StopRecenter(bool restoreInput);
        void StartLookAtTarget(Vector3 offset, float smoothSpeed, bool fallbackToCharacter);
        void UpdateLookAtTarget(float deltaTime);
        void StopLookAtTarget(bool restore);
        void SetCameraFOVAndDistance(float targetFOV, float targetDistance, float speed);
        void ResetCameraFOVAndDistance(float speed);
    }
}
