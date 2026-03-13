using UnityEngine;
using Cinemachine;

namespace Game.Camera
{
    /// <summary>
    /// 用于配合 CinemachinePOV 和 CinemachineFramingTransposer 的距离微调器。
    /// 可以根据 POV 的 VerticalAxis (仰俯角) 动态平滑改变 FramingTransposer 的 CameraDistance，
    /// 从而模拟类似于 CinemachineFreeLook 三轨 (Top/Middle/Bottom Rig) 独有的半径纵深变化效果。
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    [ExecuteAlways]
    public class CinemachinePOVDistanceAdjuster : MonoBehaviour
    {
        [Header("相机组件引用 (为空自动获取)")]
        public CinemachineVirtualCamera VirtualCamera;

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

        private CinemachinePOV _pov;
        private CinemachineFramingTransposer _transposer;
        private float _currentDistance;

        private void Start()
        {
            if (VirtualCamera == null)
            {
                VirtualCamera = GetComponent<CinemachineVirtualCamera>();
            }

            if (VirtualCamera != null)
            {
                _pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
                _transposer = VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
                
                if (_transposer != null)
                {
                    _currentDistance = _transposer.m_CameraDistance;
                }
            }
        }

        private void LateUpdate()
        {
            // 在 LateUpdate 中随着 POV 视角的更新计算最新的 CameraDistance
            // 若在开发期（ExecuteAlways）未挂载或销毁了相关组件，安全跳过
            if (VirtualCamera == null) return;
            if (_pov == null) _pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (_transposer == null) _transposer = VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_pov == null || _transposer == null) return;

            // 获取实时仰俯角及其极值范围
            float currentAngle = _pov.m_VerticalAxis.Value;
            float minAngle = _pov.m_VerticalAxis.m_MinValue; // 通常为负值，如 -70度（表示镜头俯瞰）
            float maxAngle = _pov.m_VerticalAxis.m_MaxValue; // 通常为正值，如 70度（表示镜头仰视）

            // 1. 将物理角度归一化映射到 -1 ~ 1 之间 (0 为绝对平视)
            float normalizedAngle = 0f;
            if (currentAngle < 0 && minAngle < 0)
            {
                // 俯视域：0 -> -1
                normalizedAngle = - (currentAngle / minAngle); 
            }
            else if (currentAngle > 0 && maxAngle > 0)
            {
                // 仰视域：0 -> 1
                normalizedAngle = currentAngle / maxAngle; 
            }

            // 2. 经过过渡曲线产生非线性形变（比如在0度附近给予一段死区不敏感，在极端角度突然发力拉远距离）
            float curveWeight = TransitionCurve.Evaluate(normalizedAngle);

            // 3. 根据曲线权重，在三个配置的距离标量中线性分段插值
            float targetDistance = NormalDistance;
            if (curveWeight < 0)
            {
                // 如果权重为负 (-1 到 0 之间)，就在 Downward 和 Normal 之间混融
                targetDistance = Mathf.Lerp(NormalDistance, DownwardDistance, Mathf.Abs(curveWeight));
            }
            else if (curveWeight > 0)
            {
                // 如果权重为正 (0 到 1 之间)，就在 Normal 和 Upward 之间混融
                targetDistance = Mathf.Lerp(NormalDistance, UpwardDistance, curveWeight);
            }

            // 4. 应用距离，并施加平滑阻尼，避免因鼠标猛甩导致镜头Z轴急剧位移（晕3D）
            if (Application.isPlaying)
            {
                _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * Damping);
                _transposer.m_CameraDistance = _currentDistance;
            }
            else
            {
                // Editor下实时预览立刻生效无延迟
                _transposer.m_CameraDistance = targetDistance;
                _currentDistance = targetDistance;
            }
        }
    }
}
