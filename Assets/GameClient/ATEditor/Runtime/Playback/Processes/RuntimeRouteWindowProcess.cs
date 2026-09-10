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
                comboHandler.OnComboWindowEnter(clip.comboTag);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // No repeated notification is needed while the window stays active.
        }

        public override void OnExit()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowExit(clip.comboTag);
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
