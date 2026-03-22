namespace SkillEditor
{
    /* 位移窗口本身不直接移动角色，它只负责把当前窗口策略注册到运行时状态里。 */
    [ProcessBinding(typeof(MotionWindowClip), PlayMode.Runtime)]
    public class RuntimeMotionWindowProcess : ProcessBase<MotionWindowClip>
    {
        private ISkillMotionWindowHandler _motionWindowHandler;
        private bool _isWindowRegistered;

        public override void OnEnable()
        {
            _motionWindowHandler = context.GetService<ISkillMotionWindowHandler>();
            _isWindowRegistered = false;
        }

        public override void OnEnter()
        {
            if (_motionWindowHandler != null && clip != null && !_isWindowRegistered)
            {
                /* 进入时间窗口时登记策略，真正的根运动执行仍在 MovementController.OnAnimatorMove 里。 */ _motionWindowHandler.OnMotionWindowEnter(clip, context, clip.StartTime);
                _isWindowRegistered = true;
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (_motionWindowHandler != null && clip != null)
            {
                /* 持续窗口用于更新目标、参考方向等运行时数据。 */ _motionWindowHandler.OnMotionWindowUpdate(clip, currentTime, deltaTime);
            }
        }

        public override void OnExit()
        {
            ReleaseWindow(applyEndPlacement: true);
        }

        public override void OnDisable()
        {
            /* Stop / Interrupt / FullCleanup 只保证走 OnDisable，不保证会走 OnExit。这里补一层兜底清理，避免连段中断后窗口状态残留到 locomotion。 */ ReleaseWindow(applyEndPlacement: false);
        }

        public override void Reset()
        {
            ReleaseWindow(applyEndPlacement: false);
            _motionWindowHandler = null;
            _isWindowRegistered = false;
            base.Reset();
        }

        private void ReleaseWindow(bool applyEndPlacement)
        {
            if (!_isWindowRegistered || _motionWindowHandler == null || clip == null)
            {
                return;
            }

            if (applyEndPlacement)
            {
                _motionWindowHandler.OnMotionWindowExit(clip);
            }
            else
            {
                _motionWindowHandler.OnMotionWindowCancel(clip);
            }

            _isWindowRegistered = false;
        }
    }
}
