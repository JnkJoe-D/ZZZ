using UnityEngine;

namespace SkillEditor
{
    public interface ISkillMovementHandler
    {
        // 位移
        void Move(Vector3 targetPosition, float speed, float deltaTime);
        
        // 持续转向接口
        void Rotate(RotationTargetMode targetMode, float turnSpeed, float deltaTime);
    }
}
