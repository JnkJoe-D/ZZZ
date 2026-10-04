using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(ParryWindowClip), PlayMode.Runtime)]
    public class RuntimeParryWindowProcess : ProcessBase<ParryWindowClip>
    {
        private IParryWindowHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<IParryWindowHandler>();
        }

        public override void OnEnter()
        {
            if (_handler != null && clip != null)
            {
                _handler.OnParryWindowEnter(clip);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            if (_handler != null && clip != null)
            {
                _handler.OnParryWindowExit(clip, false);
            }
        }

        public override void OnStop()
        {
            if (_handler != null && clip != null)
            {
                _handler.OnParryWindowExit(clip, true);
            }
        }
    }
}
