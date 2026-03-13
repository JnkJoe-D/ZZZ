namespace SkillEditor
{
    [ProcessBinding(typeof(ComboWindowClip), PlayMode.Runtime)]
    public class RuntimeComboWindowProcess : ProcessBase<ComboWindowClip>
    {
        private ISkillComboWindowHandler comboHandler;

        public override void OnEnable()
        {
            comboHandler = context.GetService<ISkillComboWindowHandler>();
        }

        public override void OnEnter()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowEnter(clip.comboTag, clip.windowType);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 持续时间内无需重复通知，标签已在 OnEnter 注入
        }

        public override void OnExit()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowExit(clip.comboTag, clip.windowType);
            }
        }

        public override void Reset()
        {
            base.Reset();
            // 为了安全起见，如果在播放中途被强制 Stop，我们也需要撤销该标签
            if (context != null &&  comboHandler != null && clip != null)
            {
                // 注意：在硬切换时，State可能已经析构，但为了保证对当前实体的标签清理，可以调用一次Exit。
                comboHandler.OnComboWindowExit(clip.comboTag, clip.windowType);
            }
            comboHandler = null;
        }
    }
}
