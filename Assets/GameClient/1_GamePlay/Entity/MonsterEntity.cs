using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物实体聚合根。
    /// 仅负责各子系统组件/模块的初始化装配与生命周期中继，不包含特定战斗/AI业务逻辑。
    /// </summary>
    public class MonsterEntity : CharacterEntity
    {
        public new MonsterConfigAsset Config => (MonsterConfigAsset)base.Config;

        public BTRunner BTRunner { get; private set; }
        public FSMSystem<MonsterEntity> StateMachine { get; private set; }
        public MonsterTacticalContext TacticalContext { get; private set; }
        public MonsterBrainCoordinator BrainCoordinator { get; private set; }

        public bool IsSelfControl => BrainCoordinator?.IsSelfControl ?? false;

        protected override void InitRequiredComponents()
        {
            MovementComponent = GetComponent<MovementComponent>()
                ?? gameObject.AddComponent<MovementComponent>();

            HitReactionComponent = GetComponent<MonsterHitReactionComponent>()
                ?? gameObject.AddComponent<MonsterHitReactionComponent>();

            BTRunner = GetComponent<BTRunner>() ?? gameObject.AddComponent<BTRunner>();
            BTRunner.OnComponentInit(this);

            LifecycleComponent = GetComponent<MonsterLifecycleComponent>()
                ?? gameObject.AddComponent<MonsterLifecycleComponent>();
        }

        protected override void SetupConfig(CharacterConfigAsset config)
        {
            base.SetupConfig(config);
        }

        protected override void SetupRuntimeData()
        {
            base.SetupRuntimeData();

            // 阶段 2: 注册怪物专属行为运行时数据容器
            DataModule[typeof(MonSterBehaviorRuntimeData)] ??= new MonSterBehaviorRuntimeData();
        }

        protected override void SetupComponents()
        {
            base.SetupComponents();
        }

        protected override void SetupDomainModulesAndFSM()
        {
            // 阶段 4: 领域模块、控制器中枢与状态机 (第四优先级)
            var monsterConfig = Config;

            StatusModule ??= EntityModuleFactory.Create<StatusModule>(this);
            AttributeResolver = new MonsterAttributeResolver(this);
            RouteArbitrator ??= new RouteArbitrator();

            BrainCoordinator ??= new MonsterBrainCoordinator();
            BrainCoordinator.Initialize(this);

            TargetFinder = new MonsterTargetFinder(monsterConfig.SensorConfig, transform);
            TargetFinder.Initialize(this);
            TacticalContext = new MonsterTacticalContext { LocomotionConfig = monsterConfig.locomotionConfig };

            // 1. 先构建状态机 (确保 ActionController 内部自驱播放 RootAction 时状态机已就绪)
            StateMachine = MonsterFSMBuilder.Build(this);

            // 2. 核心时序：此时 Config 与 StateMachine 已完备注入！ActionController.Initialize 内部自驱播放 PlayRootAction
            if (ActionController == null)
            {
                ActionController = EntityControllerFactory.Create<MonsterActionController>(this);
            }
            else
            {
                ActionController.Initialize(this);
            }

            if (monsterConfig.BehaviorTree != null && BTRunner != null)
            {
                BTRunner.Init(monsterConfig.BehaviorTree);
                BTRunner.StartTree();
                BrainCoordinator.SyncBlackboardSelfControl();
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
            StatusModule ??= EntityModuleFactory.Create<StatusModule>(this);
            AttributeResolver ??= new MonsterAttributeResolver(this);
            RouteArbitrator ??= new RouteArbitrator();
            if (ActionController == null)
            {
                ActionController = EntityControllerFactory.Create<MonsterActionController>(this);
            }
            BrainCoordinator ??= new MonsterBrainCoordinator();
            BrainCoordinator.Initialize(this);
            StateMachine ??= MonsterFSMBuilder.Build(this);
        }

        protected override void OnSubLogicTick(float scaledDeltaTime)
        {
            base.OnSubLogicTick(scaledDeltaTime);
            DataModule.Get<MonSterBehaviorRuntimeData>()?.Tick(scaledDeltaTime);
            (HitReactionComponent as MonsterHitReactionComponent)?.OnLogicTick(scaledDeltaTime);
            BrainCoordinator?.LogicTick(scaledDeltaTime);
            BTRunner?.OnLogicTick(scaledDeltaTime);
            StateMachine?.Update(scaledDeltaTime);
        }

        /// <summary>
        /// 重置怪物全部运行时数据与战斗状态（出池/入池时调用，防止复用残留脏数据）
        /// </summary>
        public void ResetRuntimeState()
        {
            // 1. 恢复生命周期状态标记
            var lifecycleData = DataModule.Get<LifecycleRuntimeData>();
            if (lifecycleData != null)
            {
                lifecycleData.Set(nameof(LifecycleRuntimeData.IsDead), false);
            }

            // 2. 恢复物理控制器
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
            }

            // 3. 重置血量为满额，失衡值归零，清除 Buff
            if (StatusModule?.Attributes != null)
            {
                float maxHp = StatusModule.Attributes.Has(AttributeId.MaxHp) ? StatusModule.Attributes.GetCurrent(AttributeId.MaxHp) : 99999f;
                StatusModule.Attributes.SetValue(AttributeId.HP, maxHp);
                StatusModule.Attributes.SetValue(AttributeId.Daze, 0f);
                StatusModule.Buffs?.Clear();
            }

            // 4. 重置受击硬直与格挡运行时数据
            DataModule.Get<HitReactionRuntimeData>()?.Set(nameof(HitReactionRuntimeData.InHitReaction), false);
            DataModule.Get<ParryRuntimeData>()?.Reset();

            // 5. 中断动作与行为树
            ActionPlayer?.StopAction();
            BTRunner?.StopTree();

            // 6. 重置状态机回待机态
            if (StateMachine != null && StateMachine.CurrentState is not MonsterIdleState)
            {
                StateMachine.ChangeState<MonsterIdleState>();
            }

            // 7. 同步自控力
            BrainCoordinator?.SyncBlackboardSelfControl();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            BrainCoordinator?.Dispose();
            BrainCoordinator = null;

            if (BTRunner != null)
            {
                BTRunner.StopTree();
            }

            StateMachine?.Destroy();
            StateMachine = null;

            TacticalContext?.Reset();
            TacticalContext = null;
            TargetFinder?.Dispose();
            TargetFinder = null;
        }
    }
}
