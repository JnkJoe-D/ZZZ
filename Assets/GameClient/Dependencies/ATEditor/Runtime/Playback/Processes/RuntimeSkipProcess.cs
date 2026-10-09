using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(TimelineSkipClip), PlayMode.Runtime)]
    public class RuntimeSkipProcess : ProcessBase<TimelineSkipClip>
    {
        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (context == null || clip == null) return;

            // 检查上下文中是否包含对应的Flag
            bool timeSkipFlag = false;
            if (!string.IsNullOrEmpty(clip.TimeSkipFlag) && context.Flags.Contains(clip.TimeSkipFlag))
            {
                timeSkipFlag = true;
                // 获取到标记后，将其消耗掉（重置），以免影响后续其他可能的同名片段
                context.Flags.Remove(clip.TimeSkipFlag);
            }

            if (timeSkipFlag)
            {
                // 如果有拦截标记，执行默认的跳跃（跳到片段的末尾时间）
                var runnerProvider = context.UserData as IActionRunnerProvider;
                var runner = runnerProvider?.GetRunner();

                if (runner != null)
                {
                    // 使用 deltaTime = 0f 保证跳跃瞬间不流失时间
                    runner.Seek(clip.EndTime, 0f);
                }
            }
        }
    }
}
