using Game.Logic;
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

        public override void OnDisable()
        {
            if (_eventCallback != null)
            {
                context.OnTimelineMessage -= _eventCallback;
                _eventCallback = null;
            }
        }

        public override void OnEnter()
        {
            // Reset flag when entering the clip segment
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
                isFlaggedForRewind = false; // Consume flag

                // Calculate overshoot based on the runner's current time vs clip's end time
                // Since this is OnExit, context.CurrentTime might already be slightly past EndTime.
                float overshoot = Mathf.Max(0, context.CurrentTime - clip.EndTime);
                float targetTime = clip.StartTime + overshoot;
                
                // Request seek
                var runnerService = context.UserData as IActionRunnerProvider;
                if (runnerService != null)
                {
                    var runner = runnerService.GetRunner();
                    if (runner != null)
                    {
                        // Because Seek is deferred, it is safe to call it here
                        runner.Seek(targetTime, 0f);
                        // Debug.Log($"[RewindProcess] Rewinding to {targetTime}. Iteration: {currentRewindCount}/{clip.MaxRewindCount}");
                    }
                }
                else
                {
                    Debug.LogWarning("[RewindProcess] ISkillRunnerProvider service not found in context. Cannot rewind.");
                }
            }
            else
            {
                // Reset counter if we naturally exit without rewinding
                currentRewindCount = 0;
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // Nothing needed on update
        }
    }
    
    // We need an interface to provide the runner to the process
    public interface IActionRunnerProvider
    {
        ActionRunner GetRunner();
    }
}
