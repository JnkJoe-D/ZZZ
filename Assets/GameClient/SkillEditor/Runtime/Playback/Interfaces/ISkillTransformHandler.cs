using UnityEngine;

namespace SkillEditor
{
    public interface ISkillTransformHandler
    {
        void Move(Vector3 delta);
        void SetPosition(Vector3 position);
        Vector3 GetPosition();
        
        Transform GetTarget();
        float GetRadius();
        float GetTargetRadius();

        void SetExcludeLayers(LayerMask mask);
        LayerMask GetExcludeLayers();

        void SetRotation(Quaternion rotation);
        void RotateTowards(Quaternion targetRotation, float speed);
        Quaternion GetRotation();

        Vector3 GetInputDirection(bool withCamera);
    }
}
