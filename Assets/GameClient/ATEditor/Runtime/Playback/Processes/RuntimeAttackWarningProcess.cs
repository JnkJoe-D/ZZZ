namespace ATEditor
{
    [ProcessBinding(typeof(AttackWarningClip), PlayMode.Runtime)]
    public class RuntimeAttackWarningProcess : ProcessBase<AttackWarningClip>
    {
        private IAttackWarningHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IAttackWarningHandler>();
        }

        public override void OnEnter()
        {
            UnityEngine.Debug.Log($"<color=red>[RuntimeAttackWarningProcess] AttackWarning!!!!!!</color>");
            if (_handler == null || clip == null) return;
            _handler.RegisterWarningMarker(clip);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _handler?.UnregisterWarningMarker();
        }

        public override void OnDisable()
        {
            _handler?.UnregisterWarningMarker();
        }
    }
}
