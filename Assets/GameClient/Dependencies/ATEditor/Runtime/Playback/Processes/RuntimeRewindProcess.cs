using System;
using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(RewindClip), PlayMode.Runtime)]
    public class RuntimeRewindProcess : ProcessBase<RewindClip>
    {
        private int currentRewindCount = 0;
        private bool isFlaggedForRewind = false;
        private Action<string> _eventCallback;

        public override void OnEnable()
        {
            currentRewindCount = 0;
            isFlaggedForRewind = false;
            
            _eventCallback = OnEventReceived;
            context.OnTimelineMessage += _eventCallback;
        }

        public override void OnStop()
        {
            if (_eventCallback != null)
            {
                context.OnTimelineMessage -= _eventCallback;
                _eventCallback = null;
            }
        }

        public override void OnEnter()
        {
            // 进入片段区间时重置标记
            isFlaggedForRewind = false;
        }

        private void OnEventReceived(string msg)
        {
            if (msg == "TimelineRewind")
            {
                isFlaggedForRewind = true;
            }
        }

        public override void OnExit()
        {
            if (isFlaggedForRewind && currentRewindCount < clip.MaxRewindCount)
            {
                currentRewindCount++;
                isFlaggedForRewind = false; // 消费回溯标记

                // 根据播放器当前时间与片段结束时间的差值计算超出时间
                // 在 OnExit 时，context.CurrentTime 可能已经略微超过片段结束时间
                float overshoot = Mathf.Max(0, context.CurrentTime - clip.EndTime);
                float targetTime = clip.StartTime + overshoot;
                
                // 请求跳转时间轴 (Seek)
                var runnerService = context.UserData as IActionRunnerProvider;
                if (runnerService != null)
                {
                    var runner = runnerService.GetRunner();
                    if (runner != null)
                    {
                        // 由于 Seek 为延迟执行，在此处调用是安全的
                        runner.Seek(targetTime, 0f);
                                            }
                }
                else
                {
                    ATLog.Warning("[RewindProcess] ISkillRunnerProvider service not found in context. Cannot rewind.");
                }
            }
            else
            {
                // 若未触发回溯正常退出，则重置回溯计数器
                currentRewindCount = 0;
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // OnUpdate 阶段无需执行任何操作
        }
    }
    
    // 需要通过接口或上下文向进程提供 Runner 引用
    public interface IActionRunnerProvider
    {
        ActionRunner GetRunner();
    }
}
