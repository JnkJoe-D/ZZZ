namespace ATEditor
{
    [ProcessBinding(typeof(EventClip), PlayMode.Runtime)]
    public class RuntimeEventProcess : ProcessBase<EventClip>
    {
        private IEventHandler eventHandler;

        public override void OnEnable()
        {
            eventHandler = context.GetService<IEventHandler>();
        }

        public override void OnEnter()
        {
            if (eventHandler != null)
            {
                eventHandler.OnActionTimelineEvent(clip.eventName, clip.parameters);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 事件进程通常在进入片段的第一帧 (OnEnter) 瞬间触发
        }

        public override void Reset()
        {
            base.Reset();
            eventHandler = null;
        }
    }
}
