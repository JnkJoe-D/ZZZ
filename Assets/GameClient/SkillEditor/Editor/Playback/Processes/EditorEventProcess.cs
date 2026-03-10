using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// Editor preview dispatch for EventClip.
    /// </summary>
    [ProcessBinding(typeof(EventClip), PlayMode.EditorPreview)]
    public class EditorEventProcess : ProcessBase<EventClip>
    {

        public override void OnEnable()
        {

        }

        public override void OnEnter()
        {
            Debug.Log($"[SkillEditor Preview] <color=cyan>Event Dispatched!</color> Name: {clip.eventName}");
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
