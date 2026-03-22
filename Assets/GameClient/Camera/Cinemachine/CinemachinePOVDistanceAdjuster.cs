using UnityEngine;
using Cinemachine;

namespace Game.Camera
{
    /// <summary>
    /// 用于配合 CinemachinePOV 和 CinemachineFramingTransposer 的距离微调器。
    /// 可以根据 POV 的 VerticalAxis (仰俯角) 动态平滑改变 FramingTransposer 的 CameraDistance，
    /// 从而模拟类似于 CinemachineFreeLook 三轨 (Top/Middle/Bottom Rig) 独有的半径纵深变化效果。
    /// </summary>
    [ExecuteAlways]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Extensions/POV Distance Adjuster")]
    public class CinemachinePOVDistanceAdjuster : CinemachineExtension
    {
        [Header("距离三段配置 (Camera Distance)")]
        [Tooltip("仰视时（视角最低，往苍穹看，通常是正角度最大值）的相机与角色的距离")]
        public float UpwardDistance = 2.0f;
        
        [Tooltip("平视时（0度，水平看向角色）的基础中心距离")]
        public float NormalDistance = 4.0f;
        
        [Tooltip("俯视时（视角最高，往下俯瞰，通常是负角度最小值）的相机与角色的距离")]
        public float DownwardDistance = 6.0f;

        [Header("插值缓冲")]
        [Tooltip("距离变化的平滑跟随率 (Damping)，由于视角可能会被鼠标瞬间甩动，加入缓冲可以防止画面突变闪烁。值越小跟随越慢。")]
        public float Damping = 5.0f;

        [Header("非线性过渡曲线 (仰俯角映射权重)")]
        [Tooltip("控制当前实际俯仰角映射到目标距离的融合权重，便于做出非线性过渡。\n\n" +
                 "X轴代表视角的归一化：-1 (最大俯视点) -> 0 (平视原点) -> 1 (最大仰视点) \n" +
                 "Y轴代表插值权重。当 Y<0 时会向 Downward 距离插值；Y>0 时向 Upward 距离插值。")]
        public AnimationCurve TransitionCurve = new AnimationCurve(
            new Keyframe(-1f, -1f, 0f, 1f),
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(1f, 1f, 1f, 0f)
        );

        private float _currentDistance = -1f;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // 在 Aim 阶段之后获取 POV 的旋转值，并计算目标距离
            if (stage != CinemachineCore.Stage.Aim) return;

            var vcamImpl = vcam as CinemachineVirtualCamera;
            if (vcamImpl == null) return;

            var pov = vcamImpl.GetCinemachineComponent<CinemachinePOV>();
            var transposer = vcamImpl.GetCinemachineComponent<CinemachineFramingTransposer>();
            
            if (pov == null || transposer == null) return;

            if (_currentDistance < 0)
            {
                _currentDistance = transposer.m_CameraDistance;
            }

            // 获取实时仰俯角及其极值范围
            float currentAngle = pov.m_VerticalAxis.Value;
            float minAngle = pov.m_VerticalAxis.m_MinValue; 
            float maxAngle = pov.m_VerticalAxis.m_MaxValue; 

            // 1. 将物理角度归一化映射到 -1 ~ 1 之间 (0 为绝对平视)
            float normalizedAngle = 0f;
            if (currentAngle < 0 && minAngle < 0)
            {
                normalizedAngle = - (currentAngle / minAngle); 
            }
            else if (currentAngle > 0 && maxAngle > 0)
            {
                normalizedAngle = currentAngle / maxAngle; 
            }

            // 2. 经过过渡曲线产生非线性形变
            float curveWeight = TransitionCurve.Evaluate(normalizedAngle);

            // 3. 根据曲线权重，在三个配置的距离标量中线性分段插值
            float targetDistance = NormalDistance;
            if (curveWeight < 0)
            {
                targetDistance = Mathf.Lerp(NormalDistance, DownwardDistance, Mathf.Abs(curveWeight));
            }
            else if (curveWeight > 0)
            {
                targetDistance = Mathf.Lerp(NormalDistance, UpwardDistance, curveWeight);
            }

            // 4. 应用距离，并施加平滑阻尼
            if (Application.isPlaying && deltaTime > 0)
            {
                _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, deltaTime * Damping);
                transposer.m_CameraDistance = _currentDistance;
            }
            else
            {
                transposer.m_CameraDistance = targetDistance;
                _currentDistance = targetDistance;
            }
        }
    }
}
