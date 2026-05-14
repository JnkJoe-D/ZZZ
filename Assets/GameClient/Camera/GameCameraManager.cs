using System;
using System.Threading.Tasks;
using Cinemachine;
using UnityEngine;

namespace Game.Camera
{
    public class GameCameraManager : Game.Framework.Singleton<GameCameraManager>
    {
        public UnityEngine.Camera MainCamera { get; private set; }
        public Transform MainCameraTransform => MainCamera != null ? MainCamera.transform : null;
        public Transform CurrentTarget { get; private set; }
        public CinemachineBrain Brain { get; private set; }

        private CinemachineBlendDefinition _cachedDefaultBlend;
        private int _instantCutOverrideDepth;

        public void Initialize()
        {
            ResolveMainCamera();

            if (MainCamera == null)
            {
                Debug.LogWarning("[GameCameraManager] MainCamera was not found in the scene.");
            }

            Debug.Log("[GameCameraManager] Initialized.");
        }

        public void Update(float deltaTime)
        {
            if (MainCamera == null)
            {
                ResolveMainCamera();
            }
        }

        public void Shutdown()
        {
            CurrentTarget = null;
            MainCamera = null;
            Brain = null;
            _instantCutOverrideDepth = 0;
            Debug.Log("[GameCameraManager] Shutdown.");
        }

        public void SetTarget(Transform target)
        {
            CurrentTarget = target;

            if (MainCamera == null)
            {
                ResolveMainCamera();
            }

            if (target != null)
            {
                Debug.Log($"[GameCameraManager] Camera target set to: {target.name}");
            }
        }

        public bool IsBlending()
        {
            return Brain != null && Brain.IsBlending;
        }

        public void BeginInstantCut()
        {
            if (MainCamera == null || Brain == null)
            {
                ResolveMainCamera();
            }

            if (Brain == null)
            {
                return;
            }

            if (_instantCutOverrideDepth == 0)
            {
                _cachedDefaultBlend = Brain.m_DefaultBlend;
                Brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
            }

            _instantCutOverrideDepth++;
        }

        public void EndInstantCut()
        {
            if (Brain == null || _instantCutOverrideDepth <= 0)
            {
                _instantCutOverrideDepth = 0;
                return;
            }

            _instantCutOverrideDepth--;
            if (_instantCutOverrideDepth == 0)
            {
                Brain.m_DefaultBlend = _cachedDefaultBlend;
            }
        }

        public void ForceMainCameraPose(Vector3 position, Quaternion rotation)
        {
            if (MainCamera == null)
            {
                ResolveMainCamera();
            }

            if (MainCameraTransform != null)
            {
                MainCameraTransform.SetPositionAndRotation(position, rotation);
            }
        }

        public void DoShake(Vector3 impulseVelocity)
        {
            Debug.Log($"[GameCameraManager] Camera shake impulse: {impulseVelocity}");
        }

        public void DoShake(float intensity, float time)
        {
        }

        private void ResolveMainCamera()
        {
            MainCamera = UnityEngine.Camera.main;
            Brain = MainCamera != null ? MainCamera.GetComponent<CinemachineBrain>() : null;
        }

        public async Task<GameObject> LoadCameraAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            GameObject cam = await Game.Resource.ResourceManager.Instance.LoadAssetAsync<GameObject>(path);
            return cam;
        }
    }
}
