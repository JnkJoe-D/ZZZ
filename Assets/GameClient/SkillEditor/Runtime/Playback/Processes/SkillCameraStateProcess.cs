namespace SkillEditor
{
    [ProcessBinding(typeof(SkillCameraStateClip), PlayMode.EditorPreview)]
    [ProcessBinding(typeof(SkillCameraStateClip), PlayMode.Runtime)]
    public class SkillCameraStateProcess : ProcessBase<SkillCameraStateClip>
    {
        ISkillCameraHandler _cameraHandler;
        int _stateToken;

        public override void OnEnable()
        {
            _cameraHandler = context.GetService<ISkillCameraHandler>();
        }

        public override void OnEnter()
        {
            if (_cameraHandler == null)
                return;

            _stateToken = _cameraHandler.AcquireSkillCameraState(clip.stateName, clip.priority);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            ReleaseToken();
        }

        public override void OnDisable()
        {
            ReleaseToken();
        }

        public override void Reset()
        {
            base.Reset();
            _cameraHandler = null;
            _stateToken = 0;
        }

        void ReleaseToken()
        {
            if (_cameraHandler == null || _stateToken == 0)
                return;

            _cameraHandler.ReleaseSkillCameraState(_stateToken);
            _stateToken = 0;
        }
    }
}
