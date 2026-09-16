namespace ATEditor
{
    [ProcessBinding(typeof(RouteWindowClip), PlayMode.Runtime)]
    public class RuntimeRouteWindowProcess : ProcessBase<RouteWindowClip>
    {
        private IRouteWindowHandler comboHandler;

        public override void OnEnable()
        {
            comboHandler = context.GetService<IRouteWindowHandler>();
        }

        public override void OnEnter()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowEnter(clip.comboTag, this);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 窗口处于激活期间无需重复派发通知。
        }

        public override void OnExit()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowExit(clip.comboTag, this);
            }
        }

        public override void OnDisable()
        {
        }

        public override void Reset()
        {
            comboHandler = null;
            base.Reset();
        }
    }
}
