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

        void RotateTo(Vector3 worldDirection, float speed = -1f, Vector3 localOffset = default);
        void RotateToImmediately(Vector3 worldDirection, Vector3 localOffset = default);

        void FaceTo(Vector3 direction, float speed = -1f, Vector3 localOffset = default);
        void FaceToImmediately(Vector3 direction, Vector3 localOffset = default);
        void FaceToTarget(Transform target, float speed = -1f, Vector3 localOffset = default);
        void FaceToTargetImmediately(Transform target, Vector3 localOffset = default);

        Vector3 GetInputDirection(bool withCamera);
    }
}
