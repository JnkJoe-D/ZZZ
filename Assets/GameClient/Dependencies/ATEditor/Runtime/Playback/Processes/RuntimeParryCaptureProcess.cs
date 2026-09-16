namespace ATEditor
{
    [ProcessBinding(typeof(ParryCaptureClip), PlayMode.Runtime)]
    public class RuntimeParryCaptureProcess : ProcessBase<ParryCaptureClip>
    {
        private IParryWindowHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IParryWindowHandler>();
        }

        public override void OnEnter()
        {
            _handler?.OnCaptureWindowEnter();
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _handler?.OnCaptureWindowExit(isInterrupted: false);
        }

        public override void OnDisable()
        {
            bool isInterrupted = context != null && context.IsInterrupted;
            _handler?.OnCaptureWindowExit(isInterrupted: isInterrupted);
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
