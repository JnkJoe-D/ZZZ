using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 相机脉冲运行时处理器
    /// </summary>
    [ProcessBinding(typeof(CameraImpluseClip), PlayMode.Runtime)]
    public class RuntimeCameraImpluseProcess : ProcessBase<CameraImpluseClip>
    {
        private ISkillCameraHandler cameraHandler;

        public override void OnEnable()
        {
            base.OnEnable();
            cameraHandler = context.GetService<ISkillCameraHandler>();
        }

        public override void OnEnter()
        {
            cameraHandler?.GenerateImpulseWithVelocity(clip.velocity, clip.force,clip.Duration);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {

        }

        public override void OnPause()
        {
           
        }

        public override void OnResume()
        {
            
        }

        public override void OnExit()
        {
            
        }

        public override void OnDisable()
        {
            
        }

        public override void Reset()
        {
            
        }
    }
}
