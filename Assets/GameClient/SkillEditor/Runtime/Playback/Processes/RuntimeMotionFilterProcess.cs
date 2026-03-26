namespace SkillEditor
{
    /* 位移窗口本身不直接移动角色，它只负责把当前窗口策略注册到运行时状态里。 */
    [ProcessBinding(typeof(MotionFilterClip), PlayMode.Runtime)]
    public class RuntimeMotionFilterProcess : ProcessBase<MotionFilterClip>
    {
        private ISkillMotionWindowHandler _motionWindowHandler;

        public override void OnEnable()
        {
            _motionWindowHandler =context.GetService<ISkillMotionWindowHandler>();
        }

        public override void OnEnter()
        {
            _motionWindowHandler?.EnableLocalDeltaFilter(clip.localDeltaFilterMode);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {

        }

        public override void OnExit()
        {
            _motionWindowHandler?.DisableLocalDeltaFilter();
        }

        public override void OnDisable()
        {
            _motionWindowHandler?.DisableLocalDeltaFilter();
        }
    }
}
