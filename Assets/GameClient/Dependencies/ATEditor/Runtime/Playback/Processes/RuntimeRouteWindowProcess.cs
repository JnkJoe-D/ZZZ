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

        private IRouteWindowHandler EnsureHandler()
        {
            if (comboHandler == null)
            {
                comboHandler = context.GetService<IRouteWindowHandler>();
            }
            return comboHandler;
        }

        public override void OnEnter()
        {
            var handler = EnsureHandler();
            if (handler != null && clip?.routewindow != null)
            {
                handler.OnWindowEnter(clip.routewindow);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            var handler = EnsureHandler();
            if (handler != null && clip?.routewindow != null)
            {
                handler.OnWindowProcess(clip.routewindow);
            }
        }

        public override void OnExit()
        {
            var handler = EnsureHandler();
            if (handler != null && clip?.routewindow != null)
            {
                handler.OnWindowExit(clip.routewindow);
            }
        }

        public override void OnStop()
        {
            var handler = EnsureHandler();
            if (handler != null && clip?.routewindow != null)
            {
                handler.OnWindowDisable(clip.routewindow);
            }
        }

        public override void Reset()
        {
            comboHandler = null;
            base.Reset();
        }
    }
}
