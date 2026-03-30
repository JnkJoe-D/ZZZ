using System.Collections.Generic;
using Cinemachine;
using Game.Camera;
using Game.Resource;
using SkillEditor;
using UnityEngine;
using Game.Logic.Character;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Adapters
{
    /// <summary>
    /// Adapter used by skill runtime camera-related processes.
    /// </summary>
    public class SkillCameraHandler : ISkillCameraHandler
    {
        private CharacterEntity _entity;
        public SkillCameraHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        
        public void GenerateImpulse()
        {
            _entity?.CameraController?.GenerateImpulse();
        }

        public void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration)
        {
            _entity?.CameraController?.GenerateImpulseWithVelocity(velocity, force ,duration);
        }

        public GameObject CreateCamera(GameObject prefab)
        {
            return _entity?.CameraController?.CreateCamera(prefab);
        }

        public void DestroyCamera(GameObject cameraInstance)
        {
            _entity?.CameraController?.DestroyCamera(cameraInstance);
        }

        public void PlayCameraTimeline(GameObject cameraInstance, CameraControlParams paramsObj)
        {
            _entity?.CameraController?.PlayCameraTimeline(cameraInstance, paramsObj);
        }
    }
}
