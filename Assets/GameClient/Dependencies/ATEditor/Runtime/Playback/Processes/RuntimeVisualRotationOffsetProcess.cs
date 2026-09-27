using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 运行时视觉模型旋转偏移驱动 Process
    /// 在进入窗口时触发物理根转向对齐与模型反向补偿，按曲线驱动衰减并在退出时彻底归零。
    /// </summary>
    [ProcessBinding(typeof(VisualRotationOffsetClip), PlayMode.Runtime)]
    public class RuntimeVisualRotationOffsetProcess : ProcessBase<VisualRotationOffsetClip>
    {
        private IMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableVisualRotationOffset(clip);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.Duration > 0.0001f)
            {
                float normalizedTime = Mathf.Clamp01((currentTime - clip.StartTime) / clip.Duration);
                _motionWindowHandler?.UpdateVisualRotationOffset(normalizedTime);
            }
        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableVisualRotationOffset();
        }

        public override void OnStop()
        {
            _motionWindowHandler?.DisableVisualRotationOffset();
        }
    }
}
