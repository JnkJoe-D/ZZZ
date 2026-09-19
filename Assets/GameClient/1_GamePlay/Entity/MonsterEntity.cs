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

        public override void Init(CharacterConfigAsset config)
        {
            base.Init(config);
            var monsterConfig = (MonsterConfigAsset)config;

            AttributeResolver = new MonsterAttributeResolver(this);
            CommandBuffer ??= new CommandBuffer(BufferMode.SingleOverride);
            ActionController ??= new MonsterActionController(this);

            // 1. 注册运行时状态数据容器
            DataModule[typeof(MonSterBehaviorRuntimeData)] ??= new MonSterBehaviorRuntimeData();
            DataModule[typeof(HitReactionRuntimeData)] ??= new HitReactionRuntimeData();

            // 2. 组装大脑协同器与传感器
            BrainCoordinator = new MonsterBrainCoordinator();
            BrainCoordinator.Initialize(this);

            TargetFinder = new MonsterTargetFinder(monsterConfig.SensorConfig, transform);
            TargetFinder.Initialize(this);
            TacticalContext = new MonsterTacticalContext { LocomotionConfig = monsterConfig.locomotionConfig };

            // 3. 构建状态机与行为树
            StateMachine = MonsterFSMBuilder.Build(this);

            if (monsterConfig.ActionRoot != null)
                ActionController.PlayAction(monsterConfig.ActionRoot);

            if (monsterConfig.BehaviorTree != null && BTRunner != null)
            {
                BTRunner.Init(monsterConfig.BehaviorTree);
                BTRunner.StartTree();
                BrainCoordinator.SyncBlackboardSelfControl();
            }
        }

        protected override void OnSubLogicTick(float scaledDeltaTime)
        {
            base.OnSubLogicTick(scaledDeltaTime);
            DataModule.Get<MonSterBehaviorRuntimeData>()?.Tick(scaledDeltaTime);
            (HitReactionComponent as MonsterHitReactionComponent)?.OnLogicTick(scaledDeltaTime);
            BrainCoordinator?.OnLogicTick(scaledDeltaTime);
            BTRunner?.OnLogicTick(scaledDeltaTime);
            StateMachine?.Update(scaledDeltaTime);
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
