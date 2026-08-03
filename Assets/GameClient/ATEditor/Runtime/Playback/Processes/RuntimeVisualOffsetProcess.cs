namespace ATEditor
{
    [ProcessBinding(typeof(VisualOffsetClip), PlayMode.Runtime)]
    public class RuntimeVisualOffsetProcess : ProcessBase<VisualOffsetClip>
    {
        private IMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableVisualOffset(clip.visualOffsetMode);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {

        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableVisualOffset();
        }

        public override void OnDisable()
        {
            _motionWindowHandler?.DisableVisualOffset();
        }
    }
}
