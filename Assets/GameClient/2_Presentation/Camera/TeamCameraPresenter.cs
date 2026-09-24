using Cinemachine;
using Game.Framework;
using Game.GamePlay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 编队共享相机表现层适配器。
    /// 响应小队生命周期事件 (PartyCreatedEvent, PartyMemberSpawnedEvent, PartyDestroyedEvent, ActiveCharacterChangedEvent)，
    /// 负责队伍共享虚拟相机的实例化、销毁与角色挂载绑定，完全解耦玩法层。
    /// </summary>
    public class TeamCameraPresenter : Singleton<TeamCameraPresenter>
    {
        private GameObject _sharedPartyCameraInstance;
        private CinemachineVirtualCameraBase _sharedPartyVirtualCamera;
        private bool _isInitialized;

        public bool HasSharedPartyCamera => _sharedPartyVirtualCamera != null;
        public CinemachineVirtualCameraBase SharedVirtualCamera => _sharedPartyVirtualCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            Instance.Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EventCenter.Subscribe<PartyCreatedEvent>(OnPartyCreated);
            EventCenter.Subscribe<PartyMemberSpawnedEvent>(OnPartyMemberSpawned);
            EventCenter.Subscribe<PartyDestroyedEvent>(OnPartyDestroyed);
            EventCenter.Subscribe<ActiveRoleChangedEvent>(OnActiveCharacterChanged);
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            EventCenter.Unsubscribe<PartyCreatedEvent>(OnPartyCreated);
            EventCenter.Unsubscribe<PartyMemberSpawnedEvent>(OnPartyMemberSpawned);
            EventCenter.Unsubscribe<PartyDestroyedEvent>(OnPartyDestroyed);
            EventCenter.Unsubscribe<ActiveRoleChangedEvent>(OnActiveCharacterChanged);

            DestroySharedPartyCamera();
        }

        private void OnPartyCreated(PartyCreatedEvent evt)
        {
            CreateSharedPartyCamera(evt.TeamConfig, evt.TeamContext);
        }

        private void OnPartyMemberSpawned(PartyMemberSpawnedEvent evt)
        {
            AssignSharedPartyCamera(evt.Entity);
        }

        private void OnPartyDestroyed(PartyDestroyedEvent evt)
        {
            DestroySharedPartyCamera();
        }

        private void OnActiveCharacterChanged(ActiveRoleChangedEvent evt)
        {
            if (evt.NewEntity != null && _sharedPartyVirtualCamera != null)
            {
                AssignSharedPartyCamera(evt.NewEntity);
                if (_sharedPartyCameraInstance != null && !_sharedPartyCameraInstance.activeSelf)
                {
                    _sharedPartyCameraInstance.SetActive(true);
                }
            }
        }

        public void CreateSharedPartyCamera(TeamConfigAsset teamConfig, RoleTeamContext teamContext)
        {
            DestroySharedPartyCamera();

            if (teamContext?.SharedVirtualCamera != null)
            {
                _sharedPartyVirtualCamera = teamContext.SharedVirtualCamera;
                _sharedPartyVirtualCamera.gameObject.SetActive(false);
                return;
            }

            if (teamConfig == null || teamConfig.CameraPrefab == null)
            {
                GLog.Warning(LogTags.Team, "TeamConfig or CameraPrefab is null. Shared party camera will not be created.");
                return;
            }

            _sharedPartyCameraInstance = Object.Instantiate(teamConfig.CameraPrefab);
            _sharedPartyVirtualCamera = _sharedPartyCameraInstance.GetComponent<CinemachineVirtualCameraBase>()
                ?? _sharedPartyCameraInstance.GetComponentInChildren<CinemachineVirtualCameraBase>(true);

            if (_sharedPartyVirtualCamera == null)
            {
                GLog.Warning(LogTags.Team, "Party camera prefab does not contain a CinemachineVirtualCameraBase.");
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
                return;
            }

            _sharedPartyCameraInstance.SetActive(false);
        }

        public void DestroySharedPartyCamera()
        {
            _sharedPartyVirtualCamera = null;

            if (_sharedPartyCameraInstance != null)
            {
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
            }
        }

        public void AssignSharedPartyCamera(RoleEntity entity)
        {
            if (entity == null || _sharedPartyVirtualCamera == null)
            {
                return;
            }

            entity.CameraController?.AssignVirtualCamera(_sharedPartyVirtualCamera);
        }
    }
}
