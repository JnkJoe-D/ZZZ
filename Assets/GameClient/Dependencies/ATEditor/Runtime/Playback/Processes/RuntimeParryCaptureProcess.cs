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
            if (clip?.data == null) return;
            _handler?.OnCaptureWindowEnter(clip.data);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            if (clip?.data == null) return;
            _handler?.OnCaptureWindowExit(clip.data, isInterrupted: false);
        }

        public override void OnStop()
        {
            if (clip?.data == null) return;
            bool isInterrupted = context != null && context.IsInterrupted;
            _handler?.OnCaptureWindowExit(clip.data, isInterrupted: isInterrupted);
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
