namespace ATEditor
{
    [ProcessBinding(typeof(EventClip), PlayMode.Runtime)]
    public class RuntimeEventProcess : ProcessBase<EventClip>
    {
        private ISkillEventHandler eventHandler;

        public override void OnEnable()
        {
            eventHandler = context.GetService<ISkillEventHandler>();
        }

        public override void OnEnter()
        {
            if (eventHandler != null)
            {
                eventHandler.OnSkillEvent(clip.eventName, clip.parameters);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // Event process is generally instantaneous on enter
        }

        public override void Reset()
        {
            base.Reset();
            eventHandler = null;
        }
    }
}
