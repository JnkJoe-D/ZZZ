namespace ATEditor
{
    /* 位移窗口本身不直接移动角色，它只负责把当前窗口策略注册到运行时状态里。 */
    [ProcessBinding(typeof(MotionFilterClip), PlayMode.Runtime)]
    public class RuntimeMotionFilterProcess : ProcessBase<MotionFilterClip>
    {
        private IMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler =context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableLocalDeltaFilter(clip.localDeltaFilterMode);
            _motionWindowHandler?.EnableCollisionMode(clip.collisionMode, clip.obstacleMask);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {

        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableLocalDeltaFilter();
            _motionWindowHandler?.DisableCollisionMode();
        }

        public override void OnDisable()
        {
            _motionWindowHandler?.DisableLocalDeltaFilter();
            _motionWindowHandler?.DisableCollisionMode();
        }
    }
}
