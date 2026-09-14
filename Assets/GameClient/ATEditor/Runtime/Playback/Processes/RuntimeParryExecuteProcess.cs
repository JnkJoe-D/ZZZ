namespace ATEditor
{
    [ProcessBinding(typeof(ParryExecuteClip), PlayMode.Runtime)]
    public class RuntimeParryExecuteProcess : ProcessBase<ParryExecuteClip>
    {
        private IParryWindowHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IParryWindowHandler>();
        }

        public override void OnEnter()
        {
            if (clip != null)
            {
                _handler?.OnExecuteWindowEnter(clip);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _handler?.OnExecuteWindowExit();
        }

        public override void OnDisable()
        {
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
