using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(TimelineSkipClip), PlayMode.Runtime)]
    public class RuntimeSkipProcess : ProcessBase<TimelineSkipClip>
    {
        public override void OnEnter()
        {
            if (context == null || clip == null) return;

            // 检查上下文中是否包含对应的拦截 Flag
            bool hasCancelFlag = false;
            if (!string.IsNullOrEmpty(clip.CancelFlag) && context.Flags.Contains(clip.CancelFlag))
            {
                hasCancelFlag = true;
                // 获取到标记后，将其消耗掉（重置），以免影响后续其他可能的同名片段
                context.Flags.Remove(clip.CancelFlag);
            }

            if (!hasCancelFlag)
            {
                // 如果没有拦截标记，执行默认的跳跃（跳到片段的末尾时间）
                var runnerProvider = context.UserData as IActionRunnerProvider;
                var runner = runnerProvider?.GetRunner();

                if (runner != null)
                {
                    // 使用 deltaTime = 0f 保证跳跃瞬间不流失时间
                    runner.Seek(clip.EndTime, 0f);
                }
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // Do nothing
        }
    }
}
