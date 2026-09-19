using Cinemachine;
using Game.GamePlay;
using UnityEngine;

namespace Game.GamePlay
{
    public sealed class RoleTeamContext : MonoBehaviour
    {
        [SerializeField]
        private MonoBehaviour inputProviderComponent;
        [SerializeField]
        private CinemachineVirtualCameraBase sharedVirtualCamera;
        private RoleEntity _activeRole;
        private RoleTargetFinder _targetFinder;

        public IInputProvider InputProvider { get; private set; }
        public ITargetFinder TargetFinder => _targetFinder;
        public CinemachineVirtualCameraBase SharedVirtualCamera => sharedVirtualCamera;
        public RoleEntity ActiveRole => _activeRole;

        public void Initialize(RoleTargetFinder.RoleTargetFinderCfg targetConfig = null)
        {
            ResolveInputProvider();
            ResolveSharedVirtualCamera();
            _targetFinder = new RoleTargetFinder(targetConfig);
            if (_activeRole != null)
            {
                _targetFinder.Initialize(_activeRole);
                if (InputProvider is LocalPlayerInputProvider localInput)
                {
                    localInput.SetRoleConfig(_activeRole.Config);
                }
            }
        }

        public void SetActiveRole(RoleEntity activeRole)
        {
            _activeRole = activeRole;
            _targetFinder?.Initialize(activeRole);
            if (InputProvider is LocalPlayerInputProvider localInput)
            {
                localInput.SetRoleConfig(activeRole?.Config);
            }
            SyncTransformToActiveRole();
        }

        private void OnDestroy()
        {
            _targetFinder?.Dispose();
            _targetFinder = null;
        }

        private void LateUpdate()
        {
            SyncTransformToActiveRole();
        }

        private void ResolveInputProvider()
        {
            InputProvider = inputProviderComponent as IInputProvider;
            if (InputProvider == null)
            {
                MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is IInputProvider provider)
                    {
                        inputProviderComponent = components[i];
                        InputProvider = provider;
                        break;
                    }
                }
            }

            if (InputProvider == null)
            {
                LocalPlayerInputProvider localInputProvider = gameObject.AddComponent<LocalPlayerInputProvider>();
                inputProviderComponent = localInputProvider;
                InputProvider = localInputProvider;
            }

            if (inputProviderComponent != null)
            {
                inputProviderComponent.enabled = true;
            }
        }



        private void ResolveSharedVirtualCamera()
        {
            if (sharedVirtualCamera == null)
            {
                sharedVirtualCamera = GetComponentInChildren<CinemachineVirtualCameraBase>(true);
            }
        }

        private void SyncTransformToActiveRole()
        {
            if (_activeRole == null)
            {
                return;
            }

            transform.SetPositionAndRotation(_activeRole.transform.position, _activeRole.transform.rotation);
        }
    }
}
