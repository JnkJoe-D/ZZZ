using UnityEngine;

namespace SkillEditor.Editor
{
    [ProcessBinding(typeof(MovementClip), PlayMode.EditorPreview)]
    public class EditorMovementProcess : ProcessBase<MovementClip>
    {
        public override void OnEnter()
        {
            // TODO: Implement editor preview movement logic
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // TODO: Implement editor preview movement logic
        }

        public override void OnExit()
        {
            // TODO: Cleanup if needed
        }
    }
}
