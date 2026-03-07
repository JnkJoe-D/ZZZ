using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 相机轨道 Process（编辑器与运行时共用）
    /// 通过 AnimationCurve 采样驱动 ISkillCameraHandler 的 PathPosition
    /// </summary>
    [ProcessBinding(typeof(CameraClip), PlayMode.EditorPreview)]
    [ProcessBinding(typeof(CameraClip), PlayMode.Runtime)]
    public class RuntimeCameraProcess : ProcessBase<CameraClip>
    {
        private ISkillCameraHandler cameraHandler;

        public override void OnEnable()
        {
            cameraHandler = context.GetService<ISkillCameraHandler>();
        }

        public override void OnEnter()
        {
            cameraHandler?.SetCamera(clip.cameraId, context.Owner);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (cameraHandler == null) return;

            float localTime = currentTime - clip.StartTime;
            float t = clip.sampleMode == CurveSampleMode.NormalizedTime
                ? Mathf.Clamp01(localTime / clip.Duration)
                : localTime;

            float pathPos = clip.pathCurve.Evaluate(t);
            cameraHandler.SetPathPosition(pathPos);
            Debug.Log($"POS:<color=#DC143C>{pathPos}</color>,T:<color=#0000FF>{t}</color>");
        }

        public override void OnExit()
        {
            cameraHandler?.ReleaseCamera();
        }
    }
}
