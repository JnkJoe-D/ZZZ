using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// EventClip 事件片段的编辑器预览分派处理。
    /// </summary>
    [ProcessBinding(typeof(EventClip), PlayMode.EditorPreview)]
    public class EditorEventProcess : ProcessBase<EventClip>
    {

        public override void OnEnable()
        {

        }

        public override void OnEnter()
        {
            ATLog.Info($"<color=cyan>Event Dispatched!</color> Name: {clip.eventName}");
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
