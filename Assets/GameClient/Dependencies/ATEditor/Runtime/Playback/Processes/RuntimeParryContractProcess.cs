namespace ATEditor
{
    /// <summary>
    /// 拼刀契约生命周期片段运行时处理器。
    /// 挂载于怪物出招动作，通过 IAttackWarningHandler 在攻击动作自然播完 (OnExit) 或中途被打断 (OnDisable) 时，
    /// 单方面权威注销由该怪物发起的拼刀契约，彻底杜绝僵尸契约残留。
    /// </summary>
    [ProcessBinding(typeof(ParryContractClip), PlayMode.Runtime)]
    public class RuntimeParryContractProcess : ProcessBase<ParryContractClip>
    {
        private IAttackWarningHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IAttackWarningHandler>();
        }

        public override void OnEnter()
        {
            _handler?.OnParryContractEnter(clip);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _handler?.OnParryContractExit();
        }

        public override void OnStop()
        {
            _handler?.OnParryContractExit();
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
