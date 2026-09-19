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
        public bool IsRuntimeInitialized { get; private set; }

        public void BindPresentation(IRolePresentation presentation)
        {
            Presentation = presentation;
            Presentation?.OnComponentInit(this);
        }

        protected override void Awake()
        {
            base.Awake();

            AttributeResolver = new RoleAttributeResolver(this);
            CommandBuffer ??= new CommandBuffer();
            ActionController ??= new RoleActionController(this);

            InputAdapter = new RoleInputAdapterModule();
            InputAdapter.Initialize(this);

            // 统一单处初始化运行时数据
            DataModule[typeof(EvadeRuntimeData)] ??= new EvadeRuntimeData();
            DataModule[typeof(ComboRouteRuntimeData)] ??= new ComboRouteRuntimeData();
            DataModule[typeof(SwitchRuntimeData)] ??= new SwitchRuntimeData();
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

        public override void Init(CharacterConfigAsset config)
        {
            base.Init(config);
            var charId = (cfg.ZZZ.CharacterId)config.ID;
            StatusModule?.Init(this, new RoleStatusDataProvider(charId), 1);
            
            TargetFinder = TeamContext?.TargetFinder;
            TargetFinder?.Initialize(this);
        }

        public void EnsureRuntimeInitialized()
        {
            if (IsRuntimeInitialized || Config == null) return;
            StateMachine = RoleFSMBuilder.Build(this);

            if (Config.ActionRoot != null)
            {
                ActionController?.PlayAction(Config.ActionRoot);
            }
            else
            {
                StateMachine?.ChangeState<CharacterGroundState>();
            }

            IsRuntimeInitialized = true;
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
