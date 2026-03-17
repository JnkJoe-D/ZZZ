using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(MovementClip), PlayMode.EditorPreview)]
    public class EditorMovementProcess : ProcessBase<MovementClip>
    {
        public override void OnEnter()
        {
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.movementType == MovementType.Translation)
            {
                // TODO: 编辑器下有关位移的预览逻辑。
                // 暂时不借助 handler 进行简单模拟或者不模拟
            }
            else if (clip.movementType == MovementType.Rotation)
            {
                // 对于锁定目标的位移与转向，直接留空，等待后续专用工具开发
            }
        }

        public override void OnExit()
        {
        }
    }
}
