namespace ATEditor
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
                comboHandler.OnComboWindowEnter(clip.comboTag);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // No repeated notification is needed while the window stays active.
        }

        public override void OnExit()
        {
            if (comboHandler != null && clip != null)
            {
                comboHandler.OnComboWindowExit(clip.comboTag);
            }
        }

        public override void OnDisable()
        {
        }

        public override void Reset()
        {
            base.Reset();
            if (context != null && comboHandler != null && clip != null)
            {
                // Ensure the active tag is cleaned up even if playback is stopped mid-window.
                comboHandler.OnComboWindowExit(clip.comboTag);
            }

            comboHandler = null;
        }
    }
}
