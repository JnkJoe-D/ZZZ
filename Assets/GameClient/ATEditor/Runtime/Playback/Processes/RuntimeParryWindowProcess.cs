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
            _handler?.OnParryWindowEnter();
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            // 自然离开时间区间：自然结束，注销契约并关闭窗口
            _handler?.OnParryWindowExit(isInterrupted: false);
        }

        public override void OnDisable()
        {
            bool isInterrupted = context != null && context.IsInterrupted;
            // 若被路由切招打断，则标记 isInterrupted = true，Handler 绝不注销契约
            _handler?.OnParryWindowExit(isInterrupted: isInterrupted);
        }

        public override void Reset()
        {
            base.Reset();
            _handler = null;
        }
    }
}
