namespace ATEditor
{
    [ProcessBinding(typeof(ParryWindowClip), PlayMode.Runtime)]
    public class RuntimeParryWindowProcess : ProcessBase<ParryWindowClip>
    {
        private IParryWindowHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IParryWindowHandler>();
        }

        public override void OnEnter()
        {
            _handler?.SetParryWindowActive(true);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _handler?.SetParryWindowActive(false);
        }

        public override void OnDisable()
        {
            _handler?.SetParryWindowActive(false);
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
