using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// Camera service interface used by skill runtime processes.
    /// </summary>
    public interface ISkillCameraHandler
    {
        void SetCamera(int cameraId, GameObject target);
        void SetPathPosition(float position);
        void ReleaseCamera();

        int AcquireSkillCameraState(string stateName, int priority = 0);
        void ReleaseSkillCameraState(int token);
        void SetSkillCameraState(string stateName);
        void ClearSkillCameraState();

        void GenerateImpulse();
        void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration);
    }
}
