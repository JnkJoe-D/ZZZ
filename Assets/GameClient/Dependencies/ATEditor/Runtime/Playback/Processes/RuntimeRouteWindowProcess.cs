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
            if (comboHandler != null && clip?.routewindow != null)
            {
                comboHandler.OnWindowEnter(clip.routewindow);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (comboHandler != null && clip?.routewindow != null)
            {
                comboHandler.OnWindowProcess(clip.routewindow);
            }
        }

        public override void OnExit()
        {
            if (comboHandler != null && clip?.routewindow != null)
            {
                comboHandler.OnWindowExit(clip.routewindow);
            }
        }

        public override void OnStop()
        {
            if (comboHandler != null && clip?.routewindow != null)
            {
                comboHandler.OnWindowDisable(clip.routewindow);
            }
        }

        public override void Reset()
        {
            comboHandler = null;
            base.Reset();
        }
    }
}
