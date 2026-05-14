using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using Game.Camera;
using Game.Framework;
using Game.Logic.Action;
using Game.Logic.Action.Combo;
using Game.Logic.Action.Config;
using Game.Logic.Character.Config;
using Game.Logic.DebugTools;
using Game.Resource;
using UnityEngine;

namespace Game.Logic.Character
{
    public class CharcterManager : Singleton<CharcterManager>
    {
        private sealed class PartyMember
        {
            public int SlotIndex;
            public CharacterConfigAsset Config;
            public RoleEntity Entity;
            public int ActivationVersion;
        }

        private sealed class PreparedSwitchContext
        {
            public PartyMember Outgoing;
            public PartyMember Incoming;
            public Vector3 SourcePosition;
            public Quaternion SourceRotation;
            public Vector3 PreviousCameraPosition;
            public Quaternion PreviousCameraRotation;
            public Vector3 SwitchPosition;
            public Quaternion SwitchRotation;
            public bool IncomingShown;
            public bool CameraSwitched;
            public bool Executed;
        }

        private readonly List<PartyMember> _partyMembers = new();
        private readonly Collider[] _switchPositionOverlapBuffer = new Collider[32];
        private PartyConfigAsset _partyConfig;
        private GameObject _teamInstance;
        private CharacterTeamContext _teamContext;
        private GameObject _sharedPartyCameraInstance;
        private CinemachineVirtualCameraBase _sharedPartyVirtualCamera;
        private PreparedSwitchContext _preparedSwitch;
        private int _activeSlotIndex = -1;
        private bool _isSwitching;

        private const string LocalRoleTag = "LocalRole";
        private const string ShowIncomingRoleEventName = "ShowIncomingRole";
        private const string SwitchCameraEventName = "SwitchCamera";
        private const string SwitchExecuteEventName = "SwitchExecute";
        private const string HideOutgoingRoleEventName = "HideOutgoingRole";
        private const float DefaultSwitchProbeRadius = 0.3f;
        private const float DefaultSwitchProbeHeight = 1.6f;
        private static readonly Vector3 DefaultSwitchProbeCenter = new Vector3(0f, 0.88f, 0f);
        private const float SwitchProbePadding = 0.05f;
        private bool HasSharedPartyCamera => _sharedPartyVirtualCamera != null;

        public CharacterEntity LocalCharacter { get; private set; }
        public int PartySize => _partyMembers.Count;
        public int ActiveSlotIndex => _activeSlotIndex;

        public void Initialize()
        {
            Debug.Log("[CharacterManager] Initialized.");
        }

        public void Shutdown()
        {
            UnpossessCurrentCharacter();
        }

        public void Update(float deltaTime)
        {
            MaintainStandbyIdleActions();
        }

        public async Task<CharacterEntity> InitializePartyAsync(
            PartyConfigAsset partyConfig,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            if (partyConfig == null)
            {
                return null;
            }

            List<CharacterConfigAsset> members = partyConfig.BuildRuntimeMembers();
            return await InitializePartyAsync(members, partyConfig.InitialSlotIndex, spawnPos, spawnRot, partyConfig);
        }

        public async Task<CharacterEntity> PossessNewCharacterAsync(
            string characterPrefabPath,
            CharacterConfigAsset config,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            if (config == null)
            {
                return null;
            }

            UnpossessCurrentCharacter();
            DestroySharedPartyCamera();
            CreateTeamContext(null, spawnPos, spawnRot);

            GameObject prefab = await ResolveCharacterPrefabAsync(config, characterPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[CharacterManager] Failed to resolve prefab for '{config.RoleName}'.");
                return null;
            }

            if (ActionManager.Instance != null)
            {
                await ActionManager.Instance.PreloadCharacterActionsAsync(config);
            }

            RoleEntity entity = SpawnRoleEntity(config, prefab, spawnPos, spawnRot);
            if (entity == null)
            {
                return null;
            }

            PartyMember member = new PartyMember
            {
                SlotIndex = 0,
                Config = config,
                Entity = entity
            };

            _partyMembers.Add(member);
            ActivatePartyMember(member, spawnPos, spawnRot);

            Debug.Log($"[CharacterManager] Spawned single controllable role: {config.RoleName}");
            return LocalCharacter;
        }

        public void UnpossessCurrentCharacter()
        {
            foreach (PartyMember member in _partyMembers)
            {
                if (member?.Entity == null)
                {
                    continue;
                }

                ActionManager.Instance?.RemoveCache(member.Entity);
                Object.Destroy(member.Entity.gameObject);
            }

            _partyMembers.Clear();
            _partyConfig = null;
            DestroySharedPartyCamera();
            DestroyTeamContext();
            _activeSlotIndex = -1;
            _preparedSwitch = null;
            _isSwitching = false;
            LocalCharacter = null;
            GameCameraManager.Instance?.SetTarget(null);
        }

        public void SetInputEnable(bool enable)
        {
            if (LocalCharacter != null)
            {
                LocalCharacter.SetControlActive(enable, assignCameraTarget: enable);
            }
        }

        private async Task<CharacterEntity> InitializePartyAsync(
            IReadOnlyList<CharacterConfigAsset> members,
            int initialSlotIndex,
            Vector3 spawnPos,
            Quaternion spawnRot,
            PartyConfigAsset partyConfig)
        {
            UnpossessCurrentCharacter();

            if (members == null || members.Count == 0)
            {
                return null;
            }

            _partyConfig = partyConfig;

            List<CharacterConfigAsset> runtimeMembers = new List<CharacterConfigAsset>(3);
            for (int i = 0; i < members.Count && runtimeMembers.Count < 3; i++)
            {
                if (members[i] != null)
                {
                    runtimeMembers.Add(members[i]);
                }
            }

            if (runtimeMembers.Count == 0)
            {
                return null;
            }

            CreateTeamContext(partyConfig, spawnPos, spawnRot);
            CreateSharedPartyCamera(partyConfig);

            if (ActionManager.Instance != null)
            {
                List<Task> preloadTasks = new List<Task>(runtimeMembers.Count);
                foreach (CharacterConfigAsset config in runtimeMembers)
                {
                    preloadTasks.Add(ActionManager.Instance.PreloadCharacterActionsAsync(config));
                }

                await Task.WhenAll(preloadTasks);
            }

            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                CharacterConfigAsset config = runtimeMembers[i];
                GameObject prefab = await ResolveCharacterPrefabAsync(config, null);
                if (prefab == null)
                {
                    Debug.LogError($"[CharacterManager] Missing CharacterPrefab on '{config.RoleName}'.");
                    continue;
                }

                RoleEntity entity = SpawnRoleEntity(config, prefab, spawnPos, spawnRot);
                if (entity == null)
                {
                    continue;
                }

                _partyMembers.Add(new PartyMember
                {
                    SlotIndex = _partyMembers.Count,
                    Config = config,
                    Entity = entity
                });
            }

            if (_partyMembers.Count == 0)
            {
                return null;
            }

            int activeIndex = Mathf.Clamp(initialSlotIndex, 0, _partyMembers.Count - 1);
            for (int i = 0; i < _partyMembers.Count; i++)
            {
                PartyMember member = _partyMembers[i];
                if (i == activeIndex)
                {
                    ActivatePartyMember(member, spawnPos, spawnRot);
                }
                else
                {
                    SetMemberStandby(member.Entity);
                }
            }

            return LocalCharacter;
        }

        private RoleEntity SpawnRoleEntity(
            CharacterConfigAsset config,
            GameObject prefab,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            GameObject characterGo = Object.Instantiate(prefab, spawnPos, spawnRot);
            RoleEntity entity = characterGo.GetComponent<RoleEntity>();
            if (entity == null)
            {
                entity = characterGo.AddComponent<RoleEntity>();
            }

            AssignSharedPartyCamera(entity);
            AssignTeamContext(entity);
            entity.Init(config);
            entity.EnsureRuntimeInitialized();
            if (!HasSharedPartyCamera)
            {
                entity.SetCameraRigActive(false);
            }
            entity.SetControlActive(false, assignCameraTarget: false);
            entity.ResetSwitchState();
            return entity;
        }

        private async Task<GameObject> ResolveCharacterPrefabAsync(CharacterConfigAsset config, string prefabPathOverride)
        {
            if (config != null && config.CharacterPrefab != null)
            {
                return config.CharacterPrefab;
            }

            if (!string.IsNullOrEmpty(prefabPathOverride))
            {
                return await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabPathOverride);
            }

            return null;
        }

        private void CreateSharedPartyCamera(PartyConfigAsset partyConfig)
        {
            DestroySharedPartyCamera();

            if (_teamContext?.SharedVirtualCamera != null)
            {
                _sharedPartyVirtualCamera = _teamContext.SharedVirtualCamera;
                _sharedPartyVirtualCamera.gameObject.SetActive(false);
                return;
            }

            if (partyConfig == null || partyConfig.CameraPrefab == null)
            {
                return;
            }

            _sharedPartyCameraInstance = Object.Instantiate(partyConfig.CameraPrefab);
            _sharedPartyVirtualCamera = _sharedPartyCameraInstance.GetComponent<CinemachineVirtualCameraBase>()
                ?? _sharedPartyCameraInstance.GetComponentInChildren<CinemachineVirtualCameraBase>(true);

            if (_sharedPartyVirtualCamera == null)
            {
                Debug.LogWarning("[CharacterManager] Party camera prefab does not contain a CinemachineVirtualCameraBase.");
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
                return;
            }

            _sharedPartyCameraInstance.SetActive(false);
        }

        private void DestroySharedPartyCamera()
        {
            _sharedPartyVirtualCamera = null;

            if (_sharedPartyCameraInstance != null)
            {
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
            }
        }

        private void CreateTeamContext(PartyConfigAsset partyConfig, Vector3 spawnPos, Quaternion spawnRot)
        {
            DestroyTeamContext();

            GameObject teamPrefab = partyConfig != null ? partyConfig.TeamPrefab : null;
            _teamInstance = teamPrefab != null
                ? Object.Instantiate(teamPrefab, spawnPos, spawnRot)
                : new GameObject("[Runtime] Character Team");

            _teamInstance.transform.SetPositionAndRotation(spawnPos, spawnRot);
            _teamContext = _teamInstance.GetComponent<CharacterTeamContext>();
            if (_teamContext == null)
            {
                _teamContext = _teamInstance.AddComponent<CharacterTeamContext>();
            }

            _teamContext.Initialize();
        }

        private void DestroyTeamContext()
        {
            _teamContext = null;

            if (_teamInstance != null)
            {
                Object.Destroy(_teamInstance);
                _teamInstance = null;
            }
        }

        private void AssignTeamContext(RoleEntity entity)
        {
            if (entity == null || _teamContext == null)
            {
                return;
            }

            entity.AssignTeamContext(_teamContext);
        }

        private void AssignSharedPartyCamera(RoleEntity entity)
        {
            if (entity == null || _sharedPartyVirtualCamera == null)
            {
                return;
            }

            if (entity.CameraController is CharacterCameraController characterCameraController)
            {
                characterCameraController.AssignVirtualCamera(_sharedPartyVirtualCamera);
            }
        }

        private void ActivatePartyMember(PartyMember member, Vector3 position, Quaternion rotation, bool assignCameraTarget = true)
        {
            if (member?.Entity == null)
            {
                return;
            }

            RoleEntity entity = member.Entity;
            AssignSharedPartyCamera(entity);
            AssignTeamContext(entity);
            SynchronizePartyMemberTransform(entity, position, rotation);

            if (!entity.gameObject.activeSelf)
            {
                entity.gameObject.SetActive(true);
            }

            entity.EnsureRuntimeInitialized();
            SynchronizePartyMemberTransform(entity, position, rotation);
            entity.ResetSwitchState();
            entity.SetPresentationVisible(true);
            entity.SetCameraRigActive(true);
            _teamContext?.SetActiveRole(entity);
            entity.SetControlActive(true, assignCameraTarget);
            UpdatePartyDebugHudVisibility(entity);
            member.ActivationVersion++;

            LocalCharacter = entity;
            _activeSlotIndex = member.SlotIndex;
        }

        private void SetMemberStandby(RoleEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!entity.gameObject.activeSelf)
            {
                entity.gameObject.SetActive(true);
            }

            entity.EnsureRuntimeInitialized();
            entity.SetControlActive(false, assignCameraTarget: false);
            if (!HasSharedPartyCamera)
            {
                entity.SetCameraRigActive(false);
            }
            entity.ResetSwitchState();
            entity.SetPresentationVisible(false);
            SetDebugHudVisible(entity, false);
            if (entity.Config?.ActionRoot != null &&
                (entity.ActionPlayer?.CurrentAction != entity.Config.ActionRoot || entity.ActionPlayer?.IsPlaying != true))
            {
                entity.ActionController?.PlayAction(entity.Config.ActionRoot);
            }
        }

        private static void SynchronizePartyMemberTransform(RoleEntity entity, Vector3 position, Quaternion rotation)
        {
            if (entity == null)
            {
                return;
            }

            entity.transform.SetPositionAndRotation(position, rotation);
        }

        private void UpdatePartyDebugHudVisibility(RoleEntity visibleEntity)
        {
            for (int i = 0; i < _partyMembers.Count; i++)
            {
                RoleEntity memberEntity = _partyMembers[i]?.Entity;
                if (memberEntity == null)
                {
                    continue;
                }

                SetDebugHudVisible(memberEntity, ReferenceEquals(memberEntity, visibleEntity));
            }
        }

        private static void SetDebugHudVisible(RoleEntity entity, bool visible)
        {
            if (entity == null)
            {
                return;
            }

            CharacterDebugHUD[] huds = entity.GetComponentsInChildren<CharacterDebugHUD>(true);
            for (int i = 0; i < huds.Length; i++)
            {
                if (huds[i] != null)
                {
                    huds[i].enabled = visible;
                }
            }
        }

        public bool HandleSwitchStateEntered(RoleEntity outgoingEntity)
        {
            return PrepareSwitch(outgoingEntity);
        }

        public bool PrepareSwitch(RoleEntity outgoingEntity)
        {
            if (_isSwitching || _partyMembers.Count <= 1 || outgoingEntity == null)
            {
                return false;
            }

            PartyMember from = FindPartyMember(outgoingEntity);
            if (from == null || from.SlotIndex != _activeSlotIndex || !ReferenceEquals(LocalCharacter, outgoingEntity))
            {
                return false;
            }

            PartyMember to = _partyMembers[(_activeSlotIndex + 1) % _partyMembers.Count];
            RoleEntity fromEntity = from.Entity;
            RoleEntity toEntity = to.Entity;
            Vector3 sourcePosition = fromEntity.transform.position;
            Quaternion sourceRotation = fromEntity.transform.rotation;
            Transform mainCameraTransform = GameCameraManager.Instance?.MainCameraTransform;
            Vector3 previousCameraPosition = mainCameraTransform != null ? mainCameraTransform.position : Vector3.zero;
            Quaternion previousCameraRotation = mainCameraTransform != null ? mainCameraTransform.rotation : sourceRotation;
            Vector3 switchPosition = ComputeSwitchInPosition(fromEntity, toEntity, sourcePosition, mainCameraTransform, sourceRotation);

            _preparedSwitch = new PreparedSwitchContext
            {
                Outgoing = from,
                Incoming = to,
                SourcePosition = sourcePosition,
                SourceRotation = sourceRotation,
                PreviousCameraPosition = previousCameraPosition,
                PreviousCameraRotation = previousCameraRotation,
                SwitchPosition = switchPosition,
                SwitchRotation = sourceRotation
            };

            _isSwitching = true;
            fromEntity.SetControlActive(false, assignCameraTarget: false);
            return true;
        }

        public void CancelPreparedSwitch(RoleEntity outgoingEntity)
        {
            if (_preparedSwitch == null || _preparedSwitch.Executed)
            {
                return;
            }

            RoleEntity preparedOutgoing = _preparedSwitch.Outgoing?.Entity;
            if (outgoingEntity != null && !ReferenceEquals(preparedOutgoing, outgoingEntity))
            {
                return;
            }

            if (_preparedSwitch.Incoming?.Entity != null && _preparedSwitch.Incoming.SlotIndex != _activeSlotIndex)
            {
                SetMemberStandby(_preparedSwitch.Incoming.Entity);
            }

            if (preparedOutgoing != null && ReferenceEquals(LocalCharacter, preparedOutgoing))
            {
                if (!HasSharedPartyCamera)
                {
                    preparedOutgoing.SetCameraRigActive(true);
                }

                preparedOutgoing.SetPresentationVisible(true);
                preparedOutgoing.SetControlActive(true, assignCameraTarget: true);
                UpdatePartyDebugHudVisibility(preparedOutgoing);
            }

            ClearPreparedSwitch();
        }

        public bool HandleTimelineEvent(RoleEntity sourceEntity, string eventName)
        {
            if (sourceEntity == null || string.IsNullOrEmpty(eventName))
            {
                return false;
            }

            return eventName switch
            {
                ShowIncomingRoleEventName => ShowIncomingRole(sourceEntity),
                SwitchCameraEventName => SwitchPreparedCamera(sourceEntity),
                SwitchExecuteEventName => ExecutePreparedSwitch(sourceEntity),
                HideOutgoingRoleEventName => HideOutgoingRole(sourceEntity),
                _ => false
            };
        }

        private bool ShowIncomingRole(RoleEntity sourceEntity)
        {
            if (!TryGetPreparedSwitch(sourceEntity, out PreparedSwitchContext context))
            {
                return false;
            }

            RoleEntity incomingEntity = context.Incoming?.Entity;
            if (incomingEntity == null)
            {
                return false;
            }

            AssignSharedPartyCamera(incomingEntity);
            incomingEntity.EnsureRuntimeInitialized();
            SynchronizePartyMemberTransform(incomingEntity, context.SwitchPosition, context.SwitchRotation);
            incomingEntity.ResetSwitchState();
            incomingEntity.SetPresentationVisible(true);

            context.IncomingShown = true;
            _preparedSwitch = context;
            return true;
        }

        private bool SwitchPreparedCamera(RoleEntity sourceEntity)
        {
            if (!TryGetPreparedSwitch(sourceEntity, out PreparedSwitchContext context))
            {
                return false;
            }

            if (!context.IncomingShown && !ShowIncomingRole(sourceEntity))
            {
                return false;
            }

            RoleEntity outgoingEntity = context.Outgoing?.Entity;
            RoleEntity incomingEntity = context.Incoming?.Entity;
            if (outgoingEntity == null || incomingEntity == null)
            {
                return false;
            }

            GameCameraManager.Instance?.BeginInstantCut();
            try
            {
                if (!HasSharedPartyCamera)
                {
                    outgoingEntity.SetCameraRigActive(false);
                }

                incomingEntity.SetCameraRigActive(true);
                GameCameraManager.Instance?.SetTarget(incomingEntity.transform);
                SnapIncomingCameraPose(
                    incomingEntity,
                    context.SourcePosition,
                    context.SwitchPosition,
                    context.PreviousCameraPosition,
                    context.PreviousCameraRotation);
            }
            finally
            {
                GameCameraManager.Instance?.EndInstantCut();
            }

            context.CameraSwitched = true;
            _preparedSwitch = context;
            return true;
        }

        private bool ExecutePreparedSwitch(RoleEntity sourceEntity)
        {
            if (!TryGetPreparedSwitch(sourceEntity, out PreparedSwitchContext context))
            {
                return false;
            }

            if (!context.IncomingShown && !ShowIncomingRole(sourceEntity))
            {
                return false;
            }

            PartyMember incoming = context.Incoming;
            RoleEntity incomingEntity = incoming?.Entity;
            if (incomingEntity == null)
            {
                return false;
            }

            ActivatePartyMember(incoming, context.SwitchPosition, context.SwitchRotation, assignCameraTarget: false);
            context.Executed = true;
            _preparedSwitch = context;

            if (incomingEntity.ActionController?.TryTriggerEvent(RouteEventType.Switch) != true)
            {
                if (incomingEntity.Config?.ActionRoot != null)
                {
                    incomingEntity.ActionController?.PlayAction(incomingEntity.Config.ActionRoot);
                }

                if (incomingEntity.ActionController?.TryTriggerEvent(RouteEventType.Switch) != true)
                {
                    Debug.LogWarning($"[CharacterManager] SwitchIn event route not found for '{incomingEntity.Config?.RoleName}'.");
                    _isSwitching = false;
                }
            }

            return true;
        }

        private bool HideOutgoingRole(RoleEntity sourceEntity)
        {
            if (!TryGetPreparedSwitch(sourceEntity, out PreparedSwitchContext context))
            {
                return false;
            }

            RoleEntity outgoingEntity = context.Outgoing?.Entity;
            if (outgoingEntity == null)
            {
                return false;
            }

            SetMemberStandby(outgoingEntity);
            ClearPreparedSwitch();
            return true;
        }

        private bool TryGetPreparedSwitch(RoleEntity sourceEntity, out PreparedSwitchContext context)
        {
            context = _preparedSwitch;
            return context != null &&
                   context.Outgoing?.Entity != null &&
                   ReferenceEquals(context.Outgoing.Entity, sourceEntity);
        }

        private void ClearPreparedSwitch()
        {
            _preparedSwitch = null;
            _isSwitching = false;
        }

        private void MaintainStandbyIdleActions()
        {
            for (int i = 0; i < _partyMembers.Count; i++)
            {
                RoleEntity entity = _partyMembers[i]?.Entity;
                if (entity == null ||
                    ReferenceEquals(entity, LocalCharacter) ||
                    entity.IsControlActive ||
                    entity.IsPresentationVisible ||
                    entity.Config?.ActionRoot == null)
                {
                    continue;
                }

                if (entity.ActionPlayer == null)
                {
                    continue;
                }

                if (entity.ActionPlayer.CurrentAction == entity.Config.ActionRoot && entity.ActionPlayer.IsPlaying)
                {
                    continue;
                }

                entity.ActionController?.PlayAction(entity.Config.ActionRoot);
            }
        }

        private PartyMember FindPartyMember(RoleEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            for (int i = 0; i < _partyMembers.Count; i++)
            {
                PartyMember member = _partyMembers[i];
                if (member != null && ReferenceEquals(member.Entity, entity))
                {
                    return member;
                }
            }

            return null;
        }

        private void SnapIncomingCameraPose(
            RoleEntity targetEntity,
            Vector3 sourcePosition,
            Vector3 switchPosition,
            Vector3 previousCameraPosition,
            Quaternion previousCameraRotation)
        {
            Vector3 translatedCameraPosition = previousCameraPosition + (switchPosition - sourcePosition);
            targetEntity?.CameraController?.SnapToPose(translatedCameraPosition, previousCameraRotation);
            GameCameraManager.Instance?.ForceMainCameraPose(translatedCameraPosition, previousCameraRotation);
        }

        private Vector3 ComputeSwitchInPosition(
            RoleEntity outgoingEntity,
            RoleEntity incomingEntity,
            Vector3 sourcePosition,
            Transform cameraTransform,
            Quaternion sourceRotation)
        {
            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.ProjectOnPlane(sourceRotation * Vector3.forward, Vector3.up).normalized;

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(sourceRotation * Vector3.forward, Vector3.up).normalized;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = _partyConfig != null
                    ? _partyConfig.GetSwitchOffset(i)
                    : GetDefaultSwitchOffset(i);
                Vector3 candidatePosition = sourcePosition + right * offset.x + Vector3.up * offset.y + forward * offset.z;
                if (IsSwitchPositionAvailable(candidatePosition, sourceRotation, outgoingEntity, incomingEntity))
                {
                    return candidatePosition;
                }
            }

            return sourcePosition;
        }

        private bool IsSwitchPositionAvailable(
            Vector3 candidatePosition,
            Quaternion candidateRotation,
            RoleEntity outgoingEntity,
            RoleEntity incomingEntity)
        {
            GetSwitchProbeShape(incomingEntity, out Vector3 probeCenter, out float probeHeight, out float probeRadius);
            Vector3 worldCenter = candidatePosition + candidateRotation * probeCenter;
            float halfSegment = Mathf.Max(0f, probeHeight * 0.5f - probeRadius);
            Vector3 point1 = worldCenter + Vector3.up * halfSegment;
            Vector3 point2 = worldCenter - Vector3.up * halfSegment;
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                probeRadius + SwitchProbePadding,
                _switchPositionOverlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _switchPositionOverlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                if (ShouldIgnoreSwitchPositionHit(hit, outgoingEntity, incomingEntity))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void GetSwitchProbeShape(RoleEntity entity, out Vector3 center, out float height, out float radius)
        {
            CharacterController controller = entity != null ? entity.GetComponent<CharacterController>() : null;
            if (controller != null)
            {
                center = controller.center;
                height = Mathf.Max(controller.height, controller.radius * 2f);
                radius = Mathf.Max(0.05f, controller.radius);
                return;
            }

            center = DefaultSwitchProbeCenter;
            height = DefaultSwitchProbeHeight;
            radius = DefaultSwitchProbeRadius;
        }

        private static bool ShouldIgnoreSwitchPositionHit(Collider hit, RoleEntity outgoingEntity, RoleEntity incomingEntity)
        {
            Transform hitTransform = hit.transform;
            if (outgoingEntity != null && hitTransform.IsChildOf(outgoingEntity.transform))
            {
                return true;
            }

            if (incomingEntity != null && hitTransform.IsChildOf(incomingEntity.transform))
            {
                return true;
            }

            Transform root = hitTransform.root;
            return root != null && root.CompareTag(LocalRoleTag);
        }

        private static Vector3 GetDefaultSwitchOffset(int switchSequence)
        {
            return (Mathf.Abs(switchSequence) % 4) switch
            {
                0 => new Vector3(1.5f, 0f, -1.25f),
                1 => new Vector3(0f, 0f, -1.6f),
                2 => new Vector3(-1.5f, 0f, -1.25f),
                _ => Vector3.zero
            };
        }

    }
}
