using Game.Framework;
using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 玩家角色实体聚合根。
    /// 纯领域组合根：仅负责子系统组件（表现、输入、状态机、数据）的装配与生命周期分发，
    /// 绝无任何中间人透传、输入适配细节或跨层全局相机调度。
    /// </summary>
    public class RoleEntity : CharacterEntity
    {
        public new RoleConfigAsset Config => (RoleConfigAsset)base.Config;

        // ── 内部装配的核心模块与组件 ──
        public IRolePresentation Presentation { get; private set; }
        public RoleInputAdapterModule InputAdapter { get; private set; }
        public FSMSystem<RoleEntity> StateMachine { get; private set; }
        public RoleTeamContext TeamContext { get; private set; }

        public IInputProvider InputProvider => InputAdapter?.BoundInputProvider ?? TeamContext?.InputProvider;
        public ICameraController CameraController => Presentation?.CameraController;
        public bool IsControlActive { get; private set; }

        public void BindPresentation(IRolePresentation presentation)
        {
            Presentation = presentation;
            Presentation?.OnComponentInit(this);
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void InitRequiredComponents()
        {
            MovementComponent = GetComponent<MovementComponent>() 
                ?? gameObject.AddComponent<MovementComponent>();

            HitReactionComponent = GetComponent<RoleHitReactionComponent>() 
                ?? gameObject.AddComponent<RoleHitReactionComponent>();

            LifecycleComponent = GetComponent<LifecycleComponent>() 
                ?? gameObject.AddComponent<LifecycleComponent>();

            Presentation = GetComponent<IRolePresentation>();
            if (Presentation == null)
            {
                Presentation = RolePresentationRegistry.Bind(gameObject, this);
            }
            Presentation?.OnComponentInit(this);
        }

        protected override void SetupConfig(CharacterConfigAsset config)
        {
            base.SetupConfig(config);
        }

        protected override void SetupRuntimeData()
        {
            base.SetupRuntimeData();

            // 阶段 2: 集中注册玩家特有运行时状态数据容器
            DataModule[typeof(EvadeRuntimeData)] ??= new EvadeRuntimeData();
            DataModule[typeof(ComboRouteRuntimeData)] ??= new ComboRouteRuntimeData();
            DataModule[typeof(SwitchRuntimeData)] ??= new SwitchRuntimeData();
        }

        protected override void SetupComponents()
        {
            base.SetupComponents();

            // 阶段 3: 确保表现层防腐绑定已就绪 (底层先行)
            Presentation ??= GetComponent<IRolePresentation>();
            if (Presentation == null)
            {
                Presentation = RolePresentationRegistry.Bind(gameObject, this);
            }
            Presentation?.OnComponentInit(this);
        }

        protected override void SetupDomainModulesAndFSM()
        {
            // 阶段 4: 领域模块、控制器中枢与状态机 (第四优先级)
            AttributeResolver = new RoleAttributeResolver(this);

            var charId = (cfg.ZZZ.CharacterId)Config.ID;
            StatusModule ??= EntityModuleFactory.Create<StatusModule>(this);
            StatusModule.Init(this, new RoleStatusDataProvider(charId), 1);

            RouteArbitrator ??= new RouteArbitrator();

            // 1. 先构建状态机 (因为 ActionController.Initialize 内部自驱播放 RootAction 时会触发 OnActionPlaySucceed 驱动状态机进入 RoleGroundState)
            StateMachine = RoleFSMBuilder.Build(this);

            // 2. 装配并初始化 ActionController (内部自驱播放 ActionRoot，此时 StateMachine 已就绪，能正确响应 OnActionPlaySucceed)
            ActionController ??= EntityControllerFactory.Create<RoleActionController>(this);

            // 3. 构建并装配输入适配器与目标查找
            InputAdapter ??= EntityModuleFactory.Create<RoleInputAdapterModule>(this);
            if (TeamContext != null)
            {
                InputAdapter.UpdateTeamContext(TeamContext);
            }

            TargetFinder = TeamContext?.TargetFinder;
            TargetFinder?.Initialize(this);

            // 4. 若 ActionRoot 播放后状态机尚未切入状态，兜底切入 RoleGroundState 确保输入处理器就绪
            if (StateMachine.CurrentState == null)
            {
                StateMachine.ChangeState<RoleGroundState>();
            }
        }

        public void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized) return;

            if (Config != null)
            {
                Init(Config);
                return;
            }

            // 防御性兜底：在纯单元测试环境下未注入 Config 即调用 EnsureRuntimeInitialized 时，组装最小可用领域模块
            EnsureMinimalDomainModules();
            IsRuntimeInitialized = true;
            ActionController?.PlayRootAction();
        }

        private void EnsureMinimalDomainModules()
        {
            RouteArbitrator ??= new RouteArbitrator();
            StateMachine ??= RoleFSMBuilder.Build(this);
            if (ActionController == null)
            {
                ActionController = EntityControllerFactory.Create<RoleActionController>(this);
            }
            InputAdapter ??= EntityModuleFactory.Create<RoleInputAdapterModule>(this);
            StatusModule ??= EntityModuleFactory.Create<StatusModule>(this);
            if (StateMachine.CurrentState == null)
            {
                StateMachine.ChangeState<RoleGroundState>();
            }
        }

        protected override void Start()
        {
            base.Start();
            EnsureRuntimeInitialized();
        }

        protected override void OnSubLogicTick(float logicDeltaTime)
        {
            base.OnSubLogicTick(logicDeltaTime);
            DataModule.Get<EvadeRuntimeData>()?.Tick(logicDeltaTime);
            StateMachine?.Update(logicDeltaTime);
        }

        public void AssignTeamContext(RoleTeamContext teamContext)
        {
            TeamContext = teamContext;
            TargetFinder = teamContext?.TargetFinder;
            TargetFinder?.Initialize(this);
            InputAdapter?.UpdateTeamContext(teamContext);
        }

        public void SetTargetFinder(ITargetFinder targetFinder)
        {
            TargetFinder = targetFinder;
            TargetFinder?.Initialize(this);
        }

        public void SetControlActive(bool active)
        {
            EnsureRuntimeInitialized();
            IsControlActive = active;
            InputAdapter?.SetInputActive(active);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InputAdapter?.Dispose();
            InputAdapter = null;
            CombatWarningManager.UnregisterContractsByRole(this);

            StateMachine?.Destroy();
            StateMachine = null;
        }
    }
}
