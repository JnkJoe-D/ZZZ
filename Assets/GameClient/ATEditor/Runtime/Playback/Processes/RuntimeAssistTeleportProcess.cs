namespace ATEditor
{
    [ProcessBinding(typeof(AssistTeleportClip), PlayMode.Runtime)]
    public class RuntimeAssistTeleportProcess : ProcessBase<AssistTeleportClip>
    {
        private IAssistHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IAssistHandler>();
        }

        public override void OnEnter()
        {
            if (_handler != null)
            {
                _handler.ExecuteAssistTeleport();
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
