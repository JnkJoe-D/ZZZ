namespace SkillEditor.Editor
{
    [ProcessBinding(typeof(VisualOffsetClip), PlayMode.EditorPreview)]
    public class EditorVisualOffsetProcess : ProcessBase<VisualOffsetClip>
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
