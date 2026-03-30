using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// Camera service interface used by skill runtime processes.
    /// </summary>
    public interface ISkillCameraHandler
    {

        void GenerateImpulse();
        void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration);

        GameObject CreateCamera(GameObject prefab);
        void DestroyCamera(GameObject cameraInstance);
        void PlayCameraTimeline(GameObject cameraInstance, CameraControlParams paramsObj);
    }

    public class CameraControlParams
    {
        public UnityEngine.Playables.PlayableAsset timelineAsset;
        public string followBoneName;
        public string lookAtBoneName;
        public bool overrideSettings;
        public Color backgroundColor;
        public LayerMask cullingMask;
    }
}
