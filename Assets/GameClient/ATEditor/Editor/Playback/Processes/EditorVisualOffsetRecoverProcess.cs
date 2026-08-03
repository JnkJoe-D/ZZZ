namespace ATEditor.Editor
{
    [ProcessBinding(typeof(VisualOffsetRecoverClip), PlayMode.EditorPreview)]
    public class EditorVisualOffsetRecoverProcess : ProcessBase<VisualOffsetRecoverClip>
    {
        private IMotionWindowHandler _motionWindowHandler;
        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<IMotionWindowHandler>();
        }

        public override void OnEnter()
        {

        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {

        }

        public override void OnExit()
        {

        }

        public override void OnDisable()
        {

        }
    }
}
