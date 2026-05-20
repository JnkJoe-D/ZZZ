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
        private TargetFinder targetFinder;

        [SerializeField]
        private CinemachineVirtualCameraBase sharedVirtualCamera;

        private RoleEntity _activeRole;

        public IInputProvider InputProvider { get; private set; }
        public TargetFinder TargetFinder => targetFinder;
        public CinemachineVirtualCameraBase SharedVirtualCamera => sharedVirtualCamera;

        public void Initialize()
        {
            ResolveInputProvider();
            ResolveTargetFinder();
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

        private void ResolveTargetFinder()
        {
            if (targetFinder == null)
            {
                targetFinder = GetComponentInChildren<TargetFinder>(true);
            }

            if (targetFinder == null)
            {
                targetFinder = gameObject.AddComponent<TargetFinder>();
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
