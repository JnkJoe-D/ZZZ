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
        private TargetLocker _targetLocker;
        private RoleTargetFinder _targetFinder;

        public IInputProvider InputProvider { get; private set; }
        public TargetLocker TargetLocker => _targetLocker;
        public ITargetFinder TargetFinder => _targetFinder;
        public CinemachineVirtualCameraBase SharedVirtualCamera => sharedVirtualCamera;
        public RoleEntity ActiveRole => _activeRole;

        public void Initialize(RoleTargetFinder.RoleTargetFinderCfg targetConfig = null, TargetLockerConfig lockerConfig = null)
        {
            ResolveInputProvider();
            ResolveSharedVirtualCamera();
            _targetLocker = new TargetLocker(lockerConfig);
            _targetFinder = new RoleTargetFinder(targetConfig, _targetLocker);
            if (_activeRole != null)
            {
                _targetLocker.Initialize(_activeRole);
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
            _targetLocker?.Initialize(activeRole);
            _targetFinder?.Initialize(activeRole);
            if (InputProvider is LocalPlayerInputProvider localInput)
            {
                localInput.SetRoleConfig(activeRole?.Config);
            }
            SyncTransformToActiveRole();
        }

        private bool _isTickedThisFrame;

        public void LogicTick(float logicDeltaTime)
        {
            _isTickedThisFrame = true;
            _targetLocker?.LogicTick(logicDeltaTime);
            _targetFinder?.LogicTick(logicDeltaTime);
        }

        private void Update()
        {
            // 防呆兜底：如果外部架构（如纯测试场景或未接入 TimeManager 的流程）未显式调用 LogicTick，自愈推进
            if (!_isTickedThisFrame)
            {
                _targetLocker?.LogicTick(Time.deltaTime);
                _targetFinder?.LogicTick(Time.deltaTime);
            }
            _isTickedThisFrame = false;
        }

        private void OnDestroy()
        {
            _targetLocker?.Dispose();
            _targetLocker = null;
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
