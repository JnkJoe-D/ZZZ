using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(MovementClip), PlayMode.Runtime)]
    public class RuntimeMovementProcess : ProcessBase<MovementClip>
    {
        private ISkillMovementHandler _movementHandler;

        public override void OnEnable()
        {
            _movementHandler = context.GetService<ISkillMovementHandler>();
        }

        public override void OnEnter()
        {
            if (_movementHandler != null && clip.movementType == MovementType.Rotation && clip.rotationExecuteMode == RotationExecuteMode.Once)
            {
                _movementHandler.Rotate(clip.rotationTargetMode, clip.turnSpeed, 0); 
            }
        }
        
        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (_movementHandler == null || context.OwnerTransform == null) return;

            if (clip.movementType == MovementType.Translation)
            {
                // 将本地的相对偏移位置换算为实际在世界所要到达的目标坐标
                Vector3 worldTarget = context.OwnerTransform.position + context.OwnerTransform.rotation * clip.targetPosition;
                _movementHandler.Move(worldTarget, clip.speed, deltaTime);
            }
            else if (clip.movementType == MovementType.Rotation && clip.rotationExecuteMode == RotationExecuteMode.Continuous)
            {
                _movementHandler.Rotate(clip.rotationTargetMode, clip.turnSpeed, deltaTime);
            }
        }

        public override void OnExit()
        {
        }
    }
}
