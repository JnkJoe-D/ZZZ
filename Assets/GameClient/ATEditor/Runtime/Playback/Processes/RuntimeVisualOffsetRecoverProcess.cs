namespace ATEditor
{
    [ProcessBinding(typeof(VisualOffsetRecoverClip), PlayMode.Runtime)]
    public class RuntimeVisualOffsetRecoverProcess : ProcessBase<VisualOffsetRecoverClip>
    {
        private IMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableVisualOffsetRecover(clip.recoverySpeed);
        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableVisualOffsetRecover();
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 逻辑已在 MovementController 的 OnAnimatorMove 中实现，这里无需额外处理。
        }

        public override void OnDisable()
        {
            _motionWindowHandler?.DisableVisualOffsetRecover();
        }
    }
}
