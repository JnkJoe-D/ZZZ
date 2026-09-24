using System.Collections.Generic;
using System.Threading.Tasks;
using cfg;
using Cinemachine;
using Game.Framework;
using Game.GamePlay;
using UnityEngine;

namespace Game.GamePlay
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
        private RoleTeamContext _teamContext;

        /// <summary> 角色生成装配工厂服务。 </summary>
        private readonly TeamCharacterSpawner _spawner = new();

        /// <summary> 换人空间坐标解算与防卡死避障服务。 </summary>
        private readonly TeamPlacementService _placementService = new();

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

        /// <summary> 获取当前小队的运行时逻辑上下文。 </summary>
        public RoleTeamContext TeamContext => _teamContext;

        /// <summary> 队伍共享的索敌组件。 </summary>
        public ITargetFinder TargetFinder => _teamContext?.TargetFinder;

        /// <summary> 当前被玩家直接操作并占有的主控角色 Entity。 </summary>
        public RoleEntity LocalCharacter { get; internal set; }

        /// <summary> 换人执行器微内核适配器。 </summary>
        public SwitchExecutor SwitchExecutor => _switchExecutor;

        /// <summary> 队伍的实际成员数量。 </summary>
        public int PartySize => _partyMembers.Count;

        /// <summary> 当前正处于控制/激活状态下的角色插槽索引。 </summary>
        public int ActiveSlotIndex => _activeSlotIndex;
        internal void SetActiveSlotIndex(int index) => _activeSlotIndex = index;

        /// <summary>
        /// 构造函数，创建换人执行器并执行基础系统初始化。
        /// </summary>
        public TeamManager()
        {
            _switchExecutor = new SwitchExecutor(this);
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
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick += Update;
            }
            GLog.Info(LogTags.Team, "Initialized.");
        }

        /// <summary>
        /// 系统关闭卸载，取消事件订阅并安全销毁当前所有小队成员。
        /// </summary>
        public void Shutdown()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick -= Update;
            }
            EventCenter.Unsubscribe<CharacterTimelineEvent>(OnCharacterTimelineEvent);
            _switchExecutor?.Unsubscribe();
            UnpossessCurrentCharacter();
        }

        /// <summary>
        /// 轮询更新，负责单向驱动小队角色的逻辑 Tick 以及换人执行器状态。
        /// </summary>
        public void Update(float deltaTime)
        {
            _switchExecutor?.Update(deltaTime);

            for (int i = 0; i < _partyMembers.Count; i++)
            {
                var member = _partyMembers[i];
                if (member != null && member.Entity != null && member.Entity.gameObject.activeInHierarchy)
                {
                    member.Entity.OnLogicTick(deltaTime);
                }
            }
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
            CreateTeamContext(null, spawnPos, spawnRot);
            EventCenter.Publish(new PartyCreatedEvent { TeamConfig = null, TeamContext = _teamContext });

            // 加载角色 Prefab 预制件
            GameObject prefab = await _spawner.ResolveCharacterPrefabAsync(config, characterPrefabPath);
            if (prefab == null)
            {
                GLog.Error(LogTags.Team, $"Failed to resolve prefab for '{config.Name}'.");
                return null;
            }

            // 预载动作数据
            await _spawner.PreloadPartyActionsAsync(new[] { config });

            // 实例化运行时 Entity
            RoleEntity entity = _spawner.SpawnRoleEntity(config, prefab, spawnPos, spawnRot, _teamContext);
            if (entity == null)
            {
                return null;
            }

            EventCenter.Publish(new PartyMemberSpawnedEvent { Entity = entity });

            // 包装小队成员并存入索引 0 的默认插槽
            PartyMember member = new PartyMember
            {
                SlotIndex = 0,
                Config = config,
                Entity = entity
            };

            _partyMembers.Add(member);
            ActivatePartyMember(member, spawnPos, spawnRot);

            GLog.Info(LogTags.Team, $"Spawned single controllable role: {config.Name}");
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

                // 物理销毁 GameObject 实例（动作上下文已在 Entity.OnDestroy 中自动释放）
                Object.Destroy(member.Entity.gameObject);
            }

            _partyMembers.Clear();
            _teamConfig = null;
            EventCenter.Publish(new PartyDestroyedEvent());
            DestroyTeamContext();
            _activeSlotIndex = -1;
            
            // 安全复位纯 C# 切人执行器的过渡状态机
            _switchExecutor?.Reset();
            CombatWarningManager.Clear();
            
            LocalCharacter = null;
            GameCameraManager.Instance?.SetTarget(null);
        }

        /// <summary>
        /// 开启或禁用当前正在控制角色的玩家输入响应与相机旋转追踪目标锁定。
        /// </summary>
        public void SetInputEnable(bool enable)
        {
            if (LocalCharacter != null)
            {
                LocalCharacter.SetControlActive(enable);
                if (enable)
                {
                    GameCameraManager.Instance?.SetTarget(LocalCharacter.transform);
                }
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

            // 建立小队逻辑上下文并发布小队创建事件
            CreateTeamContext(teamConfig, spawnPos, spawnRot);
            EventCenter.Publish(new PartyCreatedEvent { TeamConfig = teamConfig, TeamContext = _teamContext });

            // 并行并发预载动作包以防在战斗中切人发生 IO 顿卡
            await _spawner.PreloadPartyActionsAsync(runtimeMembers);

            // 串行生成所有角色的实例化 Entity 实例
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                CharacterConfigAsset config = runtimeMembers[i];
                GameObject prefab = await _spawner.ResolveCharacterPrefabAsync(config, null);
                if (prefab == null)
                {
                    GLog.Error(LogTags.Team, $"Missing CharacterPrefab on '{config.Name}'.");
                    continue;
                }

                RoleEntity entity = _spawner.SpawnRoleEntity(config, prefab, spawnPos, spawnRot, _teamContext);
                if (entity == null)
                {
                    continue;
                }

                EventCenter.Publish(new PartyMemberSpawnedEvent { Entity = entity });

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
            _teamContext = _teamInstance.GetComponent<RoleTeamContext>();
            if (_teamContext == null)
            {
                _teamContext = _teamInstance.AddComponent<RoleTeamContext>();
            }

            _teamContext.Initialize(teamConfig?.TargetSearchConfig);
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
            if (entity != null)
            {
                EventCenter.Publish(new PartyMemberSpawnedEvent { Entity = entity });
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
                _placementService.ResolveSwitchInPlacement(outgoing.transform, entity, _teamConfig, out spawnPos, out spawnRot);
            }
            else
            {
                _placementService.ResolvePlacementAtPosition(position, rotation, entity, _teamConfig, out spawnPos, out spawnRot);
            }

            SynchronizePartyMemberTransform(entity, spawnPos, spawnRot);

            if (!entity.gameObject.activeSelf)
            {
                entity.gameObject.SetActive(true);
            }

            entity.EnsureRuntimeInitialized();
            entity.Presentation?.SetColliderActive(true);
            SynchronizePartyMemberTransform(entity, spawnPos, spawnRot);
            entity.RouteArbitrator?.Clear();
            entity.Presentation?.SetPresentationVisible(true);
            entity.Presentation?.SetCameraActive(true);
            _teamContext?.SetActiveRole(entity);
            if (assignCameraTarget)
            {
                GameCameraManager.Instance?.SetTarget(entity.transform);
            }
            entity.SetControlActive(true);
            UpdatePartyDebugHudVisibility(entity);
            member.ActivationVersion++;
            
            LocalCharacter = entity;
            int oldSlotIndex = _activeSlotIndex;
            _activeSlotIndex = member.SlotIndex;

            if (oldSlotIndex != _activeSlotIndex)
            {
                EventCenter.Publish(new ActiveRoleChangedEvent 
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
            entity.SetControlActive(false);
            entity.Presentation?.SetCameraActive(false);
            entity.RouteArbitrator?.Clear();
            entity.Presentation?.SetPresentationVisible(false);
            entity.Presentation?.SetColliderActive(false);
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
        internal void SynchronizePartyMemberTransform(RoleEntity entity, Vector3 position, Quaternion rotation)
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

        internal void CalculateSafeSwitchInTransform(Transform originTransform, RoleEntity switchInEntity, out Vector3 targetPos, out Quaternion targetRot)
        {
            _placementService.ResolveSwitchInPlacement(originTransform, switchInEntity, _teamConfig, out targetPos, out targetRot);
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
        /// 响应 Timeline 切人关键帧动作过渡事件的转发分发接口（委托至 SwitchExecutor 状态机）。
        /// </summary>
        public bool HandleTimelineEvent(RoleEntity sourceEntity, string eventName)
        {
            return _switchExecutor != null && _switchExecutor.HandleTimelineEvent(sourceEntity, eventName);
        }

        /// <summary>
        /// 依据角色的运行时实体引用，查询并返回对应的编队插槽成员包装结构。
        /// </summary>
        public PartyMember FindPartyMember(RoleEntity entity)
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

        /// <summary>
        /// 获取指定成员按编队轮转规则的下一个存活且有效的队友。
        /// </summary>
        public PartyMember GetNextPartyMember(PartyMember current)
        {
            if (_partyMembers.Count <= 1 || current == null) return null;
            int startIndex = (current.SlotIndex + 1) % _partyMembers.Count;
            for (int attempt = 0; attempt < _partyMembers.Count; attempt++)
            {
                int index = (startIndex + attempt) % _partyMembers.Count;
                PartyMember candidate = _partyMembers[index];
                if (candidate != current && candidate.Entity != null && !candidate.Entity.LifecycleComponent.IsDead)
                    return candidate;
            }
            return null;
        }

        public PartyMember GetNextPartyMember(RoleEntity currentEntity)
        {
            return GetNextPartyMember(FindPartyMember(currentEntity));
        }
    }

    /// <summary>
    /// 小队全局共享战斗运行时数据（支援点数等）
    /// </summary>
    public class TeamRuntimeData
    {
        private static TeamRuntimeData _instance;
        public static TeamRuntimeData Instance => _instance ??= new TeamRuntimeData();

        public int CurrentAssistPoints { get; private set; } = 6;
        public int MaxAssistPoints { get; set; } = 6;

        public event System.Action<int, int> OnAssistPointsChanged;

        public void SetAssistPoints(int points)
        {
            int old = CurrentAssistPoints;
            CurrentAssistPoints = Mathf.Clamp(points, 0, MaxAssistPoints);
            if (old != CurrentAssistPoints)
            {
                OnAssistPointsChanged?.Invoke(CurrentAssistPoints, MaxAssistPoints);
                EventCenter.Publish(new AssistPointsChangedEvent
                {
                    CurrentPoints = CurrentAssistPoints,
                    MaxPoints = MaxAssistPoints
                });
            }
        }

        public bool ModifyAssistPoints(int delta)
        {
            if (delta < 0 && CurrentAssistPoints + delta < 0)
            {
                return false;
            }
            SetAssistPoints(CurrentAssistPoints + delta);
            return true;
        }

        public bool HasAssistPoints(int required)
        {
            return CurrentAssistPoints >= required;
        }

        /// <summary>
        /// 统一的小队属性读取接口
        /// </summary>
        public float GetAttribute(AttributeId attrId)
        {
            return attrId switch
            {
                AttributeId.AssistPoint => CurrentAssistPoints,
                _ => 0f
            };
        }

        /// <summary>
        /// 统一的小队属性修改接口
        /// </summary>
        public void ModifyAttribute(AttributeId attrId, float delta)
        {
            if (attrId == AttributeId.AssistPoint)
            {
                ModifyAssistPoints((int)delta);
            }
        }

        /// <summary>
        /// 统一的小队属性存在性检查
        /// </summary>
        public bool HasAttribute(AttributeId attrId)
        {
            return attrId == AttributeId.AssistPoint;
        }

        public void Reset()
        {
            CurrentAssistPoints = MaxAssistPoints;
        }
    }
}

