namespace ATEditor
{
    /* 旋转过滤窗口不直接旋转角色，它负责把当前窗口的旋转约束策略注册到运行时运动解算中。 */
    [ProcessBinding(typeof(RotationFilterClip), PlayMode.Runtime)]
    public class RuntimeRotationFilterProcess : ProcessBase<RotationFilterClip>
    {
        private IMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableRotationFilter(clip);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableRotationFilter();
        }

        public override void OnStop()
        {
            _motionWindowHandler?.DisableRotationFilter();
        }
    }
}
