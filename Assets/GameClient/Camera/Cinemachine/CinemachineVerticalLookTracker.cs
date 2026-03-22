using UnityEngine;
using Cinemachine;

namespace Game.Camera
{
    /// <summary>
    /// Cinemachine 扩展：垂直高度追踪器
    /// 当 Follow 目标（通常是 Root）和 LookAt 目标（通常是骨骼或 Aim 点）产生垂直位移差时，
    /// 自动为相机增加一个仰角/俯角偏移。
    /// 解决在 Body 为 FramingTransposer 且 Follow 为 Root 时，模型向上运动相机不抬头的问题。
    /// </summary>
    [ExecuteAlways]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Extensions/Vertical Look Tracker")]
    public class CinemachineVerticalLookTracker : CinemachineExtension
    {
        [Header("追踪配置")]
        [Tooltip("高度差参考基准点（通常为角色的 Root，为空则默认使用 VCam 的 Follow 目标）")]
        public Transform Basis;

        [Tooltip("实际追踪目标（通常为角色骨骼或 Aim 节点，为空则默认使用 VCam 的 LookAt 目标）")]
        public Transform TrackerTarget;

        [Header("旋转补偿 (Aim)")]
        [Tooltip("仰角追踪强度。微幅旋转建议控制在 1-5 之间。")]
        [Range(0, 50)]
        public float RotationSensitivity = 2f;

        [Tooltip("最大允许的仰角偏移（度）")]
        public float MaxTiltAngle = 10f;

        [Tooltip("是否反转旋转方向。")]
        public bool InvertRotation = false;

        [Header("位移补偿 (Body)")]
        [Tooltip("垂直位移追踪强度。0.5 表示相机跟随高度差上升一半的位移。")]
        [Range(0, 10)]
        public float PositionSensitivity = 0.5f;

        [Tooltip("最大允许的垂直位移量")]
        public float MaxPositionYOffset = 5f;

        [Tooltip("是否反转位移方向。")]
        public bool InvertPosition = false;

        [Header("通用阈值与平滑")]
        [Tooltip("触发补偿的垂直高度阈值。只有模型高度差超过此值时，相机才会开始反应。")]
        public float HeightThreshold = 0.5f;

        [Tooltip("是否仅追踪向上位移。")]
        public bool OnlyTrackUpward = true;

        [Tooltip("追踪平滑度。值越大，跟随越平缓。")]
        public float SmoothTime = 0.1f;

        private float _currentTilt;
        private float _tiltVelocity;

        private float _currentPosYOffset;
        private float _posVelocity;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // 将处理移至 Finalize 阶段，确保在所有 Cinemachine 标准计算完成后进行最终由于干预
            // 这样可以避免与 Body/Aim 阶段的内部缓存或中间计算冲突
            if (stage != CinemachineCore.Stage.Finalize) return;

            Transform basis = Basis != null ? Basis : vcam.Follow;
            Transform target = TrackerTarget != null ? TrackerTarget : vcam.LookAt;

            if (basis == null || target == null) return;

            // 1. 计算垂直高度差
            float deltaY = target.position.y - basis.position.y;

            // 2. 计算目标补偿值
            float targetTilt = 0;
            float targetPosOffset = 0;
            
            if (deltaY > HeightThreshold)
            {
                float excess = deltaY - HeightThreshold;
                targetTilt = -excess * RotationSensitivity * (InvertRotation ? -1 : 1);
                targetPosOffset = excess * PositionSensitivity * (InvertPosition ? -1 : 1);
            }
            else if (!OnlyTrackUpward && deltaY < -HeightThreshold)
            {
                float excess = deltaY + HeightThreshold;
                targetTilt = -excess * RotationSensitivity * (InvertRotation ? -1 : 1);
                targetPosOffset = excess * PositionSensitivity * (InvertPosition ? -1 : 1);
            }

            // 箝位
            targetTilt = Mathf.Clamp(targetTilt, -MaxTiltAngle, MaxTiltAngle);
            targetPosOffset = Mathf.Clamp(targetPosOffset, -MaxPositionYOffset, MaxPositionYOffset);

            // 3. 平滑处理
            if (Application.isPlaying && deltaTime > 0)
            {
                _currentTilt = Mathf.SmoothDamp(_currentTilt, targetTilt, ref _tiltVelocity, SmoothTime, float.MaxValue, deltaTime);
                _currentPosYOffset = Mathf.SmoothDamp(_currentPosYOffset, targetPosOffset, ref _posVelocity, SmoothTime, float.MaxValue, deltaTime);
            }
            else
            {
                _currentTilt = targetTilt;
                _currentPosYOffset = targetPosOffset;
                _tiltVelocity = 0;
                _posVelocity = 0;
            }

            // 4. 应用补偿
            // 应用位移 (叠加在最终计算出的 RawPosition 上)
            state.RawPosition.y += _currentPosYOffset;

            // 应用旋转 (绕相机当前的右向量旋转)
            Quaternion tiltOffset = Quaternion.Euler(_currentTilt, 0, 0);
            state.RawOrientation = state.RawOrientation * tiltOffset;
        }
    }
}
