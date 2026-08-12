using Cinemachine;
using Game.Input;
using UnityEngine;

namespace Game.Logic
{
    public sealed class CharacterTeamContext : MonoBehaviour
    {
        [SerializeField]
        private MonoBehaviour inputProviderComponent;



        [SerializeField]
        private CinemachineVirtualCameraBase sharedVirtualCamera;

        private RoleEntity _activeRole;

        public IInputProvider InputProvider { get; private set; }
        public ITargetFinder TargetFinder => TeamManager.Instance?.TargetFinder;
        public CinemachineVirtualCameraBase SharedVirtualCamera => sharedVirtualCamera;
        public RoleEntity ActiveRole => _activeRole;

        public void Initialize()
        {
            ResolveInputProvider();

            ResolveSharedVirtualCamera();
        }

        public void SetActiveRole(RoleEntity activeRole)
        {
            _activeRole = activeRole;
            SyncTransformToActiveRole();
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
