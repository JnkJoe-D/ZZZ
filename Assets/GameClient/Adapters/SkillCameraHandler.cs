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
    public class SkillCameraHandler : ISkillCameraHandler, System.IDisposable
    {
        readonly Dictionary<int, CinemachineVirtualCamera> _cameraRegistry
            = new Dictionary<int, CinemachineVirtualCamera>();

        readonly HashSet<int> _ownedSkillCameraStateTokens = new HashSet<int>();
        CinemachineTrackedDolly _activeDolly;
        CinemachineVirtualCamera _activeCamera;
        CinemachineSkillDrivenCamera _skillDrivenCamera;
        bool _ownsManualSkillCameraState;
        bool _defaultCameraInitialized;

        private CharacterEntity _entity;
        public SkillCameraHandler(CharacterEntity entity)
        {
            _entity = entity;
        }
        public void Dispose()
        {
            ReleaseOwnedSkillCameraStates();

            foreach (var kvp in _cameraRegistry)
            {
                if (kvp.Value != null)
                    Object.DestroyImmediate(kvp.Value.transform.root.gameObject);
            }

            ClearRegistry();
        }

        public void RegisterCamera(int cameraId, CinemachineVirtualCamera vcam)
        {
            if (vcam == null)
                return;

            _cameraRegistry[cameraId] = vcam;
        }

        public void ClearRegistry()
        {
            ReleaseOwnedSkillCameraStates();
            _cameraRegistry.Clear();
            _activeDolly = null;
            _activeCamera = null;
            _skillDrivenCamera = null;
            _defaultCameraInitialized = false;
        }

        public void SetCamera(int cameraIndex, GameObject target)
        {
            EnsureDefaultCameraRegistered();

            _activeDolly = null;
            _activeCamera = null;

            if (!_cameraRegistry.TryGetValue(cameraIndex, out var vcam))
            {
                Debug.LogWarning($"[SkillCameraHandler] No virtual camera registered for cameraIndex={cameraIndex}.");
                return;
            }

            _activeCamera = vcam;
            if (target != null)
            {
                vcam.Follow = target.transform;
                var aim = target.transform.Find("Aim");
                vcam.LookAt = aim ?? target.transform;
            }

            _activeDolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (_activeDolly == null)
            {
                Debug.LogWarning($"[SkillCameraHandler] cameraIndex={cameraIndex} has no CinemachineTrackedDolly body component.");
                return;
            }

            vcam.gameObject.SetActive(true);
        }

        public void SetPathPosition(float position)
        {
            if (_activeDolly != null)
                _activeDolly.m_PathPosition = position;

            if (_activeCamera != null)
            {
                _activeCamera.InternalUpdateCameraState(Vector3.up, -1f);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    SceneView.RepaintAll();
#endif
            }
        }

        public void ReleaseCamera()
        {
            if (_activeCamera != null)
                _activeCamera.gameObject.SetActive(false);

            _activeDolly = null;
            _activeCamera = null;
        }

        public int AcquireSkillCameraState(string stateName, int priority = 0)
        {
            var camera = ResolveSkillDrivenCamera();
            if (camera == null)
                return 0;

            int token = camera.AcquireState(stateName, priority);
            if (token != 0)
                _ownedSkillCameraStateTokens.Add(token);
            RefreshSkillDrivenCamera(camera);
            return token;
        }

        public void ReleaseSkillCameraState(int token)
        {
            if (token == 0)
                return;

            var camera = ResolveSkillDrivenCamera();
            if (camera == null)
                return;

            camera.ReleaseState(token);
            _ownedSkillCameraStateTokens.Remove(token);
            RefreshSkillDrivenCamera(camera);
        }

        public void SetSkillCameraState(string stateName)
        {
            var camera = ResolveSkillDrivenCamera();
            if (camera == null)
                return;

            camera.SetState(stateName);
            _ownsManualSkillCameraState = true;
            RefreshSkillDrivenCamera(camera);
        }

        public void ClearSkillCameraState()
        {
            var camera = ResolveSkillDrivenCamera();
            if (camera == null)
                return;

            camera.ClearState();
            _ownsManualSkillCameraState = false;
            RefreshSkillDrivenCamera(camera);
        }

        void EnsureDefaultCameraRegistered()
        {
            if (_defaultCameraInitialized)
                return;

            _defaultCameraInitialized = true;
            CameraConfig config = null;

#if UNITY_EDITOR
            config = AssetDatabase.LoadAssetAtPath<CameraConfig>(
                "Assets/Resources/Serializations/ScriptableObjects/10001.asset");
#else
            config = ResourceManager.Instance.LoadAsset<CameraConfig>(
                "Assets/Resources/Serializations/ScriptableObjects/10001.asset");
#endif

            if (config == null || config.prefab == null)
                return;

            var prefab = Object.Instantiate(config.prefab);
            RegisterCamera(10001, prefab.GetComponentInChildren<CinemachineVirtualCamera>());
        }

        CinemachineSkillDrivenCamera ResolveSkillDrivenCamera()
        {
            if (_skillDrivenCamera != null)
                return _skillDrivenCamera;

            var cameras = Resources.FindObjectsOfTypeAll<CinemachineSkillDrivenCamera>();
            foreach (var camera in cameras)
            {
                if (camera != null && camera.gameObject.scene.IsValid())
                {
                    _skillDrivenCamera = camera;
                    break;
                }
            }

            return _skillDrivenCamera;
        }

        void RefreshSkillDrivenCamera(CinemachineSkillDrivenCamera camera)
        {
            if (camera == null)
                return;

            camera.InternalUpdateCameraState(Vector3.up, -1f);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                SceneView.RepaintAll();
#endif
        }

        void ReleaseOwnedSkillCameraStates()
        {
            var camera = ResolveSkillDrivenCamera();
            if (camera == null)
                return;

            foreach (int token in _ownedSkillCameraStateTokens)
                camera.ReleaseState(token);
            _ownedSkillCameraStateTokens.Clear();

            if (_ownsManualSkillCameraState)
            {
                camera.ClearState();
                _ownsManualSkillCameraState = false;
            }

            RefreshSkillDrivenCamera(camera);
        }

        public void GenerateImpulse()
        {
            _entity?.CameraController?.GenerateImpulse();
        }

        public void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration)
        {
            _entity?.CameraController?.GenerateImpulseWithVelocity(velocity, force ,duration);
        }
    }
}
