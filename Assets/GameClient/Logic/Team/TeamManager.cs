using System.Collections.Generic;
using System.Threading.Tasks;
using cfg;
using Cinemachine;
using Game.Camera;
using Game.Framework;
using Game.Logic;
using Game.Resource;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 队伍成员数据封装，用于在多角色编队中跟踪特定插槽的运行时状态与配置信息。
    /// </summary>
    public class PartyMember
    {
        /// <summary> 角色在队伍中的插槽索引 (0-indexed)。 </summary>
        public int SlotIndex;

        /// <summary> 角色的静态配置资源，包含角色属性、动画配置及资源引用。 </summary>
        public CharacterConfigAsset Config;

        /// <summary> 实例化到场景中的角色运行时 Entity 实例。 </summary>
        public RoleEntity Entity;

        /// <summary> 
        /// 角色激活版本号。每次该插槽角色切入激活状态时自增，
        /// 用于在异步操作中校验状态的时效性，防止过期回调产生逻辑冲突。
        /// </summary>
        public int ActivationVersion;
    }

    /// <summary>
    /// 角色管理器 (TeamManager)
    /// 架构设计说明:
    /// 1. 职责划分 (SRP)：本类主要负责多角色小队的生命周期管理、实体容器维护、相机与队伍上下文挂载。
    /// 2. 状态机解耦：将具体的换人状态管理、换人轨迹与物理检测、以及 Timeline 关键帧过渡动画的执行逻辑
    ///    剥离到了纯 C# 工具类 <see cref="SwitchExecutor"/> 中，从而保持本类的架构纯洁与单一职责。
    /// </summary>
    public class TeamManager : Singleton<TeamManager>
    {
        /// <summary> 队伍中当前所有的成员列表容器。 </summary>
        private readonly List<PartyMember> _partyMembers = new();

        /// <summary> 当前生效的队伍全局配置资源（定义了成员、出生配置、相机器件等）。 </summary>
        private TeamConfigAsset _teamConfig;

        /// <summary> 小队上下文组件所在的 GameObject 运行时实例。 </summary>
        private GameObject _teamInstance;

        /// <summary> 小队运行时共享的逻辑上下文，控制小队内的状态同步与通信。 </summary>
        private CharacterTeamContext _teamContext;

        /// <summary> 换人过程中多角色共享的虚拟相机 GameObject 实例。 </summary>
        private GameObject _sharedPartyCameraInstance;

        /// <summary> 队伍共享虚拟相机的 Cinemachine 组件接口，用于接管镜头控制。 </summary>
        private CinemachineVirtualCameraBase _sharedPartyVirtualCamera;

        /// <summary> 当前正处于控制/激活状态下的角色插槽索引。 </summary>
        private int _activeSlotIndex = -1;

        /// <summary> 负责执行具体切人逻辑的纯 C# 执行器实例。 </summary>
        private readonly SwitchExecutor _switchExecutor;

        /// <summary> 获取当前队伍是否正处于换人动作的过渡状态中。 </summary>
        public bool IsSwitching => _switchExecutor != null && _switchExecutor.IsSwitching;

        /// <summary> 暴露队伍成员列表的只读视图，提供安全的外部查询。 </summary>
        public IReadOnlyList<PartyMember> PartyMembers => _partyMembers;

        /// <summary> 暴露当前生效的队伍全局配置。 </summary>
        public TeamConfigAsset TeamConfig => _teamConfig;

        /// <summary> 获取是否已配置并挂载了共享的队伍虚拟相机。 </summary>
        public bool HasSharedPartyCamera => _sharedPartyVirtualCamera != null;

        /// <summary> 获取当前小队的运行时逻辑上下文。 </summary>
        public CharacterTeamContext TeamContext => _teamContext;

        /// <summary> 队伍共享的索敌组件。 </summary>
        public ITargetFinder TargetFinder { get; private set; }

        /// <summary> 当前被玩家直接操作并占有的主控角色 Entity。 </summary>
        public RoleEntity LocalCharacter { get; private set; }

        /// <summary> 队伍的实际成员数量。 </summary>
        public int PartySize => _partyMembers.Count;

        /// <summary> 当前正处于控制/激活状态下的角色插槽索引。 </summary>
        public int ActiveSlotIndex => _activeSlotIndex;

        /// <summary>
        /// 构造函数，创建换人执行器并执行基础系统初始化。
        /// </summary>
        public TeamManager()
        {
            _switchExecutor = new SwitchExecutor(this);
            Initialize();
        }

        /// <summary>
        /// 查询指定角色是否正在切出队列中（供外部系统跳过处理）。
        /// </summary>
        public bool IsInSwitchOutQueue(RoleEntity entity)
        {
            return _switchExecutor != null && _switchExecutor.IsInSwitchOutQueue(entity);
        }

        /// <summary>
        /// 系统初始化，订阅角色动画及行为 Timeline 换人关键帧事件。
        /// </summary>
        public void Initialize()
        {
            EventCenter.Subscribe<CharacterTimelineEvent>(OnCharacterTimelineEvent);
            Debug.Log("[TeamManager] Initialized.");
        }

        /// <summary>
        /// 系统关闭卸载，取消事件订阅并安全销毁当前所有小队成员。
        /// </summary>
        public void Shutdown()
        {
            EventCenter.Unsubscribe<CharacterTimelineEvent>(OnCharacterTimelineEvent);
            _switchExecutor?.Unsubscribe();
            UnpossessCurrentCharacter();
        }

        /// <summary>
        /// 轮询更新，负责驱动非主控角色的挂机/待机动作状态维持。
        /// </summary>
        public void Update(float deltaTime)
        {
            _switchExecutor?.Update(deltaTime);

        }

        /// <summary>
        /// 内部 Timeline 动画事件触发回调，委托给换人执行器识别执行特定的换人步骤（显隐、运镜、路由激活等）。
        /// </summary>
        private void OnCharacterTimelineEvent(CharacterTimelineEvent evt)
        {
            HandleTimelineEvent(evt.SourceEntity, evt.EventName);
        }

        /// <summary>
        /// 异步初始化编队小队。
        /// 1. 解析小队配置中的所有运行时成员；
        /// 2. 调用内部重载执行底层异步资源预载与实体实例化流程。
        /// </summary>
        public async Task<RoleEntity> InitializePartyAsync(
            TeamConfigAsset teamConfig,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            if (teamConfig == null)
            {
                return null;
            }

            List<CharacterConfigAsset> members = teamConfig.BuildRuntimeMembers();
            return await InitializePartyAsync(members, teamConfig.InitialSlotIndex, spawnPos, spawnRot, teamConfig);
        }

        /// <summary>
        /// 异步占有并控制一个新的单体角色（非换人，属于全量重置单控角色）。
        /// 1. 清理当前小队内所有其他角色的运行时实体；
        /// 2. 预载当前角色的全部 Action 配置动作数据以防止运行卡顿；
        /// 3. 生成并激活新角色 Entity，重置控制和相机参数。
        /// </summary>
        public async Task<RoleEntity> PossessNewCharacterAsync(
            string characterPrefabPath,
            CharacterConfigAsset config,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            if (config == null)
            {
                return null;
            }

            // 清理旧的角色和相机器件
            UnpossessCurrentCharacter();
            DestroySharedPartyCamera();
            CreateTeamContext(null, spawnPos, spawnRot);

            // 加载角色 Prefab 预制件
            GameObject prefab = await ResolveCharacterPrefabAsync(config, characterPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[TeamManager] Failed to resolve prefab for '{config.Name}'.");
                return null;
            }

            // 预载动作数据
            if (ActionManager.Instance != null)
            {
                await ActionManager.Instance.PreloadCharacterActionsAsync(config);
            }

            // 实例化运行时 Entity
            RoleEntity entity = SpawnRoleEntity(config, prefab, spawnPos, spawnRot);
            if (entity == null)
            {
                return null;
            }

            // 包装小队成员并存入索引 0 的默认插槽
            PartyMember member = new PartyMember
            {
                SlotIndex = 0,
                Config = config,
                Entity = entity
            };

            _partyMembers.Add(member);
            ActivatePartyMember(member, spawnPos, spawnRot);

            Debug.Log($"[TeamManager] Spawned single controllable role: {config.Name}");
            return LocalCharacter;
        }

        /// <summary>
        /// 卸载并销毁当前小队中所有的角色实体，彻底重置整个小队的运行时容器状态与相机锁定。
        /// </summary>
        public void UnpossessCurrentCharacter()
        {
            foreach (PartyMember member in _partyMembers)
            {
                if (member?.Entity == null)
                {
                    continue;
                }

                // 卸载动作缓存并物理销毁 GameObject 实例
                ActionManager.Instance?.RemoveCache(member.Entity);
                Object.Destroy(member.Entity.gameObject);
            }

            _partyMembers.Clear();
            _teamConfig = null;
            DestroySharedPartyCamera();
            DestroyTeamContext();
            _activeSlotIndex = -1;
            
            // 安全复位纯 C# 切人执行器的过渡状态机
            _switchExecutor?.Reset();
            
            LocalCharacter = null;
            TargetFinder = null;
            GameCameraManager.Instance?.SetTarget(null);
        }

        /// <summary>
        /// 开启或禁用当前正在控制角色的玩家输入响应与相机旋转追踪目标锁定。
        /// </summary>
        public void SetInputEnable(bool enable)
        {
            if (LocalCharacter != null)
            {
                LocalCharacter.SetControlActive(enable, assignCameraTarget: enable);
            }
        }

        /// <summary>
        /// 核心多角色异步队伍初始化的底层私有实现。
        /// 1. 并发并行预载所有队伍角色的动画与路由动作数据（Task.WhenAll）；
        /// 2. 依次生成物理 Entity 实例，将初始插槽成员置于激活态，其余成员置于 Standby 隐藏备用状态。
        /// </summary>
        private async Task<RoleEntity> InitializePartyAsync(
            IReadOnlyList<CharacterConfigAsset> members,
            int initialSlotIndex,
            Vector3 spawnPos,
            Quaternion spawnRot,
            TeamConfigAsset teamConfig)
        {
            UnpossessCurrentCharacter();

            if (members == null || members.Count == 0)
            {
                return null;
            }

            _teamConfig = teamConfig;

            // 初始化队伍共享的索敌组件
            TargetFinder = new RoleTargetFinder(teamConfig.TargetSearchConfig);

            // 限制最多加载并生成 3 名编队成员
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

            // 建立小队逻辑上下文及共享虚拟相机
            CreateTeamContext(teamConfig, spawnPos, spawnRot);
            CreateSharedPartyCamera(teamConfig);

            // 并行并发预载动作包以防在战斗中切人发生 IO 顿卡
            if (ActionManager.Instance != null)
            {
                List<Task> preloadTasks = new List<Task>(runtimeMembers.Count);
                foreach (CharacterConfigAsset config in runtimeMembers)
                {
                    preloadTasks.Add(ActionManager.Instance.PreloadCharacterActionsAsync(config));
                }

                await Task.WhenAll(preloadTasks);
            }

            // 串行生成所有角色的实例化 Entity 实例
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                CharacterConfigAsset config = runtimeMembers[i];
                GameObject prefab = await ResolveCharacterPrefabAsync(config, null);
                if (prefab == null)
                {
                    Debug.LogError($"[TeamManager] Missing CharacterPrefab on '{config.Name}'.");
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

            // 激活初始插槽，并将备用插槽成员设为隐藏 Standby 状态
            int activeIndex = Mathf.Clamp(initialSlotIndex, 0, _partyMembers.Count - 1);
            for (int i = 0; i < _partyMembers.Count; i++)
            {
                if (i != activeIndex)
                {
                    SetMemberStandby(_partyMembers[i].Entity);
                }
            }

            ActivatePartyMember(_partyMembers[activeIndex], spawnPos, spawnRot);

            return LocalCharacter;
        }

        /// <summary>
        /// 物理生成并实例化角色的 RoleEntity。
        /// 挂载并关联小队相机和队伍全局逻辑上下文，并重置控制激活标志。
        /// </summary>
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

        /// <summary>
        /// 异步解析并载入配置的角色 Prefab，支持路径重写。
        /// </summary>
        private async Task<GameObject> ResolveCharacterPrefabAsync(CharacterConfigAsset config, string prefabPathOverride)
        {
            if (config != null && config.Prefab != null)
            {
                return config.Prefab;
            }

            if (!string.IsNullOrEmpty(prefabPathOverride))
            {
                return await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabPathOverride);
            }

            return null;
        }

        /// <summary>
        /// 实例化并建立共享的编队相机实例。如果 Context 自身已携带共享相机则直接复用。
        /// </summary>
        private void CreateSharedPartyCamera(TeamConfigAsset teamConfig)
        {
            DestroySharedPartyCamera();

            if (_teamContext?.SharedVirtualCamera != null)
            {
                _sharedPartyVirtualCamera = _teamContext.SharedVirtualCamera;
                _sharedPartyVirtualCamera.gameObject.SetActive(false);
                return;
            }

            if (teamConfig == null || teamConfig.CameraPrefab == null)
            {
                return;
            }

            _sharedPartyCameraInstance = Object.Instantiate(teamConfig.CameraPrefab);
            _sharedPartyVirtualCamera = _sharedPartyCameraInstance.GetComponent<CinemachineVirtualCameraBase>()
                ?? _sharedPartyCameraInstance.GetComponentInChildren<CinemachineVirtualCameraBase>(true);

            if (_sharedPartyVirtualCamera == null)
            {
                Debug.LogWarning("[TeamManager] Party camera prefab does not contain a CinemachineVirtualCameraBase.");
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
                return;
            }

            _sharedPartyCameraInstance.SetActive(false);
        }

        /// <summary>
        /// 销毁运行时生成的共享虚拟相机实例，释放内存占用。
        /// </summary>
        private void DestroySharedPartyCamera()
        {
            _sharedPartyVirtualCamera = null;

            if (_sharedPartyCameraInstance != null)
            {
                Object.Destroy(_sharedPartyCameraInstance);
                _sharedPartyCameraInstance = null;
            }
        }

        /// <summary>
        /// 创建或实例化小队的核心逻辑上下文组件及其承载的 GameObject 容器。
        /// </summary>
        private void CreateTeamContext(TeamConfigAsset teamConfig, Vector3 spawnPos, Quaternion spawnRot)
        {
            DestroyTeamContext();

            GameObject teamPrefab = teamConfig != null ? teamConfig.TeamPrefab : null;
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

        /// <summary>
        /// 销毁运行时生成的小队上下文实例。
        /// </summary>
        private void DestroyTeamContext()
        {
            _teamContext = null;

            if (_teamInstance != null)
            {
                Object.Destroy(_teamInstance);
                _teamInstance = null;
            }
        }

        /// <summary>
        /// 为指定的角色实体绑定并同步小队运行时逻辑上下文，实现伤害分发及属性同步。
        /// </summary>
        internal void AssignTeamContext(RoleEntity entity)
        {
            if (entity == null || _teamContext == null)
            {
                return;
            }

            entity.AssignTeamContext(_teamContext);
        }

        /// <summary>
        /// 为指定的角色实体绑定并关联队伍共享虚拟相机。
        /// </summary>
        internal void AssignSharedPartyCamera(RoleEntity entity)
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

        /// <summary>
        /// 激活特定的队伍成员并接管控制权。
        /// 1. 挂载小队共享虚拟相机及逻辑上下文；
        /// 2. 同步并定位到指定的切入坐标与旋转；
        /// 3. 设置模型显示可见、开启控制输入、激活主控相机锁定并标记 ActiveSlotIndex；
        /// 4. 自增激活版本号，实现异步行为的时效防夹校验。
        /// </summary>
        internal void ActivatePartyMember(PartyMember member, Vector3 position, Quaternion rotation, bool assignCameraTarget = true)
        {
            if (member?.Entity == null)
            {
                return;
            }

            RoleEntity entity = member.Entity;
            AssignSharedPartyCamera(entity);
            AssignTeamContext(entity);

            // 计算安全的切入位置和旋转，防止切入新角色时穿墙或卡入障碍物
            Vector3 spawnPos = position;
            Quaternion spawnRot = rotation;

            RoleEntity outgoing = _teamContext?.ActiveRole;
            if (outgoing != null && outgoing != entity)
            {
                GetInvalidPosSwitchIn(outgoing.transform, entity, out spawnPos, out spawnRot);
            }
            else
            {
                GetInvalidPosItself(position, rotation, entity, out spawnPos, out spawnRot);
            }

            SynchronizePartyMemberTransform(entity, spawnPos, spawnRot);

            if (!entity.gameObject.activeSelf)
            {
                entity.gameObject.SetActive(true);
            }

            entity.EnsureRuntimeInitialized();
            entity.SetColliderActive(true);
            SynchronizePartyMemberTransform(entity, spawnPos, spawnRot);
            entity.ResetSwitchState();
            entity.SetPresentationVisible(true);
            entity.SetCameraRigActive(true);
            _teamContext?.SetActiveRole(entity);
            entity.SetControlActive(true, assignCameraTarget);
            UpdatePartyDebugHudVisibility(entity);
            member.ActivationVersion++;
            
            LocalCharacter = entity;
            int oldSlotIndex = _activeSlotIndex;
            _activeSlotIndex = member.SlotIndex;

            if (oldSlotIndex != _activeSlotIndex)
            {
                EventCenter.Publish(new ActiveCharacterChangedEvent 
                {
                    OldSlotIndex = oldSlotIndex,
                    NewSlotIndex = _activeSlotIndex,
                    NewEntity = entity
                });
            }
        }

        /// <summary>
        /// 将指定的队伍角色设为 Standby（待机隐藏备用）状态。
        /// 1. 禁用玩家控制输入，剥离主控相机（非共享相机时关闭 CameraRig）；
        /// 2. 模型视觉渲染置为不可见，关闭 Debug 界面；
        /// 3. 驱动其行为控制器执行挂机待机闲置动作（ActionRoot）。
        /// </summary>
        internal void SetMemberStandby(RoleEntity entity)
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
            entity.SetColliderActive(false);
            SetDebugHudVisible(entity, false);
            if (entity.Config?.ActionRoot != null &&
                (entity.ActionPlayer?.CurrentAction != entity.Config.ActionRoot || entity.ActionPlayer?.IsPlaying != true))
            {
                entity.ActionController?.PlayAction(entity.Config.ActionRoot);
            }
        }

        /// <summary>
        /// 同步角色实体的空间三维坐标与旋转朝向。
        /// </summary>
        private void SynchronizePartyMemberTransform(RoleEntity entity, Vector3 position, Quaternion rotation)
        {
            if (entity == null)
            {
                return;
            }

            var cc = entity.GetComponent<CharacterController>();
            if (cc != null)
            {
                bool wasEnabled = cc.enabled;
                cc.enabled = false;
                entity.transform.SetPositionAndRotation(position, rotation);
                cc.enabled = wasEnabled;
            }
            else
            {
                entity.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private void GetInvalidPosSwitchIn(Transform originTransform, RoleEntity switchInEntity, out Vector3 targetPos, out Quaternion targetRot)
        {
            Vector3 originPos = originTransform.position;
            Quaternion originRot = originTransform.rotation;

            // 1. 优先依次检测配置的偏移量位置，寻找首选且无阻挡的切入点
            var offsets = _teamConfig != null ? _teamConfig.SwitchInOffset : null;
            if (offsets != null)
            {
                for (int i = 0; i < offsets.Count; ++i)
                {
                    Vector3 testPos = originTransform.TransformPoint(offsets[i]);
                    if (!IsPositionBlocked(testPos, switchInEntity))
                    {
                        targetPos = testPos;
                        targetRot = originRot;
                        return;
                    }
                }
            }

            // 2. 如果配置的所有偏移点都被阻挡，再用原点位置进行无阻挡检测和首要兜底
            if (!IsPositionBlocked(originPos, switchInEntity))
            {
                targetPos = originPos;
                targetRot = originRot;
                return;
            }

            // 3. 若全部候选位置都被阻挡，则绝对兜底回到原点位置
            targetPos = originPos;
            targetRot = originRot;
        }

        private void GetInvalidPosItself(Vector3 position, Quaternion rotation, RoleEntity switchInEntity, out Vector3 targetPos, out Quaternion targetRot)
        {
            targetPos = position;
            targetRot = rotation;

            // 如果没有 outgoing（如初始化时），仅对目标点本身做防夹阻挡检测
            if (IsPositionBlocked(position, switchInEntity))
            {
                var offsets = _teamConfig != null ? _teamConfig.SwitchInOffset : null;
                if (offsets != null)
                {
                    for (int i = 0; i < offsets.Count; ++i)
                    {
                        Vector3 testPos = position + rotation * offsets[i];
                        if (!IsPositionBlocked(testPos, switchInEntity))
                        {
                            targetPos = testPos;
                            break;
                        }
                    }
                }
            }
        }

        private bool IsPositionBlocked(Vector3 pos, RoleEntity entity)
        {
            if (entity == null) return false;

            // 获取角色的真实碰撞半径（含 skinWidth）
            float radius = entity.GetCharcterRadius();
            
            // 投射胶囊体的半径是碰撞半径乘 _teamConfig.blockRadiusMultipier 系数
            float checkRadius = radius * (_teamConfig != null ? _teamConfig.blockRadiusMultipier : 1.0f);

            // 获取角色高度（用于确定胶囊体的顶部和底部球心）
            float height = 2.0f;
            var cc = entity.GetComponent<CharacterController>();
            if (cc != null)
            {
                height = cc.height;
            }
            else
            {
                var capsule = entity.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    height = capsule.height;
                }
            }

            // 胶囊体在 pos 位置对应的底部和顶部球心
            Vector3 pointBottom = pos + Vector3.up * checkRadius;
            Vector3 pointTop = pos + Vector3.up * Mathf.Max(height - checkRadius, checkRadius);

            // 层级读取 _teamConfig.blockLayer 配置
            LayerMask mask = _teamConfig != null ? _teamConfig.blockLayer : (LayerMask)0;

            // 通过 OverlapCapsule 投射胶囊体并检测是否有碰撞阻挡
            Collider[] colliders = Physics.OverlapCapsule(pointBottom, pointTop, checkRadius, mask, QueryTriggerInteraction.Ignore);
            return colliders != null && colliders.Length > 0;
        }

        /// <summary>
        /// 刷新队伍中所有角色的调试 HUD 面板显示可见性（仅对当前活跃控制角色显示）。
        /// </summary>
        internal void UpdatePartyDebugHudVisibility(RoleEntity visibleEntity)
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

        /// <summary>
        /// 开启或禁用特定角色的调试 UI HUD。
        /// </summary>
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

        /// <summary>
        /// 进入换人行为过渡状态的回调入口（委托至 SwitchExecutor 异步检测）。
        /// </summary>
        public bool HandleSwitchStateEntered(RoleEntity outgoingEntity)
        {
            return _switchExecutor != null && _switchExecutor.PrepareSwitch(outgoingEntity);
        }

        /// <summary>
        /// 准备切人。锁定当前主控角色的输入并估算切入点的物理可用性（委托至 SwitchExecutor）。
        /// </summary>
        public bool PrepareSwitch(RoleEntity outgoingEntity)
        {
            return _switchExecutor != null && _switchExecutor.PrepareSwitch(outgoingEntity);
        }

        /// <summary>
        /// 响应 Timeline 切人关键帧动作过渡事件的转发分发接口（委托至 SwitchExecutor 状态机）。
        /// </summary>
        public bool HandleTimelineEvent(RoleEntity sourceEntity, string eventName)
        {
            return _switchExecutor != null && _switchExecutor.HandleTimelineEvent(sourceEntity, eventName);
        }

        /// <summary>
        /// 维持非主控处于备用（Standby）状态的各角色的挂机闲置循环动作。
        /// </summary>
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

                // 跳过正在切出队列中的角色，避免干扰其退场流程
                if (_switchExecutor != null && _switchExecutor.IsInSwitchOutQueue(entity))
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

        /// <summary>
        /// 依据角色的运行时实体引用，查询并返回对应的编队插槽成员包装结构。
        /// </summary>
        internal PartyMember FindPartyMember(RoleEntity entity)
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
    }
}

