namespace SkillEditor.Editor
{
    [ProcessBinding(typeof(VisualOffsetRecoverClip), PlayMode.EditorPreview)]
    public class EditorVisualOffsetRecoverProcess : ProcessBase<VisualOffsetRecoverClip>
    {
        private ISkillMotionWindowHandler _motionWindowHandler;
        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<ISkillMotionWindowHandler>();
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
