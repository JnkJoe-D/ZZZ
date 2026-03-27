using UnityEngine;

namespace SkillEditor.Editor
{
    [ProcessBinding(typeof(RotationClip), PlayMode.EditorPreview)]
    public class EditorRotationProcess : ProcessBase<RotationClip>
    {
        public override void OnEnter()
        {
            // TODO: Implement editor preview rotation logic
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // TODO: Implement editor preview rotation logic
        }

        public override void OnExit()
        {
            // TODO: Cleanup if needed
        }
    }
}
