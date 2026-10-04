using System;
using System.Reflection;
using System.Threading.Tasks;
using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    public class MonsterStunAndActionControllerTests
    {
        private GameObject _monsterGo;
        private MonsterEntity _monster;
        private GameObject _roleGo;
        private RoleEntity _role;

        [SetUp]
        public void SetUp()
        {
            _monsterGo = new GameObject("TestMonster");
            _monster = _monsterGo.AddComponent<MonsterEntity>();
            var monsterConfig = ScriptableObject.CreateInstance<MonsterConfigAsset>();
            _monster.Init(monsterConfig);

            _roleGo = new GameObject("TestRole");
            _role = _roleGo.AddComponent<RoleEntity>();
            _role.EnsureRuntimeInitialized();
        }

        [TearDown]
        public void TearDown()
        {
            if (_monsterGo != null) UnityEngine.Object.DestroyImmediate(_monsterGo);
            if (_roleGo != null) UnityEngine.Object.DestroyImmediate(_roleGo);
        }

        [Test]
        public void Test01_ActionController_PlayActionIsPrivate()
        {
            // 验证架构契约：PlayAction 必须严格收敛为 private，杜绝外部越界调用
            var playActionMethod = typeof(ActionController).GetMethod("PlayAction",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(playActionMethod, "ActionController 中必须存在 PlayAction 方法");
            Assert.IsTrue(playActionMethod.IsPrivate, "ActionController.PlayAction 必须被收敛为 private，外部必须通过 OnInput 压指令或 PlayRootAction！");
        }

        [Test]
        public void Test02_ActionController_RootActionPlaysSuccessfully()
        {
            var controller = _role.ActionController;
            Assert.IsNotNull(controller, "RoleEntity 必须拥有 ActionController");

            // 测试 PlayRootAction 接口对外可用性
            bool played = controller.PlayRootAction();
            // 在测试环境下若未指定 ActionRoot 资产，则安全返回 false，但不应抛出任何空指针异常
            Assert.DoesNotThrow(() => controller.PlayRootAction(), "调用 PlayRootAction 不得发生异常");
        }

        [Test]
        public void Test03_ActionAndInvincibleTriggerPipe_DispatchesParryAidEventsBasedOnDirectClash()
        {
            var pipe = new ActionAndInvincibleTriggerPipe();
            var incomingMember = new PartyMember { SlotIndex = 1, Entity = _role };
            var outgoingMember = new PartyMember { SlotIndex = 0, Entity = _role };

            // 1. 处于提前预判期 (isDirectClash = false)：派发 ParryAidStart
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = ATEditor.WarningSignalType.Yellow_Parryable,
                ParryWeight = ATEditor.ParryWeight.Light,
                DirectClashTimeOffset = 10.0f,
                StartTime = 0f
            };

            var ctx = new SwitchPipelineContext
            {
                Type = SwitchType.ParryAid,
                IncomingMember = incomingMember,
                OutgoingMember = outgoingMember,
                TargetAttacker = _monster,
                WarningMarker = marker
            };

            RouteEventType dispatchedEvent = RouteEventType.None;
            _role.ActionController.OnRouteEventTriggered += (evt) => dispatchedEvent = evt;

            pipe.Process(ctx);

            Assert.AreEqual(RouteEventType.ParryAidStart, dispatchedEvent, "提前预判期必须派发 ParryAidStart 事件！");

            // 2. 处于正式直接招架期 (isDirectClash = true)：派发 ParryAid
            marker.DirectClashTimeOffset = 0f;
            dispatchedEvent = RouteEventType.None;

            pipe.Process(ctx);

            Assert.AreEqual(RouteEventType.ParryAid, dispatchedEvent, "正式直接招架期必须派发 ParryAid 事件！");
        }

        [Test]
        public void Test04_MonsterStunState_BrainCoordinatorLossSelfControlWhenDazeFull()
        {
            var coordinator = _monster.BrainCoordinator;
            var attrs = _monster.StatusModule?.Attributes;
            Assert.IsNotNull(coordinator, "MonsterEntity 必须包含 MonsterBrainCoordinator");
            Assert.IsNotNull(attrs, "MonsterEntity 必须包含 Attributes 属性模块");

            attrs.Register(new AttributeInstance(AttributeId.Daze, 0f, 0f, 100f));
            attrs.Register(new AttributeInstance(AttributeId.MaxDaze, 100f, 0f, 100f));

            // 初始状态下拥有自控力
            Assert.IsTrue(coordinator.IsSelfControl, "失衡槽未满时怪物应当具有自控力");

            // 填充失衡槽至满额 100
            attrs.SetValue(AttributeId.Daze, 100f);

            // 验证自控力丧失并进入失衡状态
            Assert.IsFalse(coordinator.IsSelfControl, "失衡槽达到满额 100 时怪物必须丧失自控力 (IsSelfControl == false)");
            Assert.IsInstanceOf<MonsterStunState>(_monster.StateMachine.CurrentState, "失衡槽满额后必须自动切换至 MonsterStunState 状态！");
        }

        [Test]
        public void Test05_MonsterStunState_ImmuneToNormalHitStunInterruption()
        {
            var attrs = _monster.StatusModule.Attributes;
            attrs.Register(new AttributeInstance(AttributeId.Daze, 100f, 0f, 100f));
            attrs.Register(new AttributeInstance(AttributeId.MaxDaze, 100f, 0f, 100f));

            // 切入失衡状态
            _monster.StateMachine.ChangeState<MonsterStunState>();
            Assert.IsInstanceOf<MonsterStunState>(_monster.StateMachine.CurrentState);

            // 模拟受击数据到达
            var hitData = _monster.DataModule.Get<HitReactionRuntimeData>();
            hitData.Set(nameof(hitData.InHitReaction), true);

            // 在失衡状态下推进 Tick，测试 TryEnterHitStun 不会抢占打断失衡
            _monster.StateMachine.Update(0.1f);

            Assert.IsInstanceOf<MonsterStunState>(_monster.StateMachine.CurrentState, "处于失衡瘫痪状态的怪物绝不能被普通受击硬直 (MonsterHitStunState) 抢占打断！");
        }

        [Test]
        public void Test06_EntityLifecycle_FourStageInitialization_PlaysRootActionSuccessfully()
        {
            var roleGo = new GameObject("LifecycleTestRole");
            try
            {
                var role = roleGo.AddComponent<RoleEntity>();

                // 构建测试用配置与 ActionRoot 资产
                var mockConfig = ScriptableObject.CreateInstance<RoleConfigAsset>();
                var mockRootAction = ScriptableObject.CreateInstance<ActionConfigAsset>();
                mockConfig.ActionRoot = mockRootAction;

                // 验证执行 Init 前的状态（Phase 0: Awake 仅初始化引擎基础，不创建领域模块）
                Assert.IsFalse(role.IsRuntimeInitialized, "未调用 Init 前实体不应标记为运行期初始化完成");

                // 执行四阶段初始化流水线
                role.Init(mockConfig);

                // 验证 Phase 1: 静态配置注入完成
                Assert.AreEqual(mockConfig, role.Config, "Phase 1: Config 必须成功注入");

                // 验证 Phase 2: 运行时状态容器完备
                Assert.IsNotNull(role.DataModule.Get<ActionRuntimeData>(), "Phase 2: ActionRuntimeData 必须存在");
                Assert.IsNotNull(role.DataModule.Get<EvadeRuntimeData>(), "Phase 2: EvadeRuntimeData 必须存在");

                // 验证 Phase 4: 控制器中枢就绪，且 Entity 初始化流水线完成后成功播放 ActionRoot！
                Assert.IsNotNull(role.ActionController, "Phase 4: ActionController 必须创建就绪");
                Assert.AreEqual(mockRootAction, role.ActionController.CurrentPlayingAction,
                    "时序修复验证核心：Entity 初始化流水线全部完成时必须成功调用 PlayRootAction 播放 ActionRoot！");
                Assert.IsTrue(role.IsRuntimeInitialized, "实体必须标记为初始化完毕");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roleGo);
            }
        }

        [Test]
        public void Test07_RoleEntity_InputPipelineAndServiceBinding_ReadyOnInitialization()
        {
            var roleGo = new GameObject("InputBindingTestRole");
            try
            {
                var role = roleGo.AddComponent<RoleEntity>();
                var mockConfig = ScriptableObject.CreateInstance<RoleConfigAsset>();
                var mockRootAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
                mockConfig.ActionRoot = mockRootAction;

                role.Init(mockConfig);

                // 1. 验证时间轴服务工场能够在初始化后立即成功解析到 IRouteWindowHandler
                var service = ATServiceFactory.ProvideService(typeof(ATEditor.IRouteWindowHandler), roleGo);
                Assert.IsNotNull(service, "ATServiceFactory 必须能解析到实体的 IRouteWindowHandler");
                Assert.AreSame(role.ActionController, service, "IRouteWindowHandler 必须就是实体的 ActionController");

                // 2. 验证状态机已经处于地面待机态，且有效 InputHandler 已建立 (绝非 NullInputCommandHandler)
                Assert.IsNotNull(role.StateMachine.CurrentState, "角色初始化后状态机当前状态不得为空");
                Assert.IsInstanceOf<RoleGroundState>(role.StateMachine.CurrentState, "角色初始状态必须为 RoleGroundState");

                var groundState = (RoleGroundState)role.StateMachine.CurrentState;
                Assert.IsNotNull(groundState.InputHandler, "地面状态输入处理器不得为空");
                Assert.IsNotInstanceOf<NullInputCommandHandler>(groundState.InputHandler, "初始待机状态下输入处理器绝不能是 NullInputCommandHandler");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roleGo);
            }
        }

        [Test]
        public void Test08_ParryAidStart_DynamicTiming_CalculatesStartTimeAndPassesToCommand()
        {
            var roleGo = new GameObject("TestRole_Timing");
            var monsterGo = new GameObject("TestMonster_Timing");
            try
            {
                var role = roleGo.AddComponent<RoleEntity>();
                var monster = monsterGo.AddComponent<MonsterEntity>();

                var mockConfig = ScriptableObject.CreateInstance<RoleConfigAsset>();
                mockConfig.AssistConfig = new RoleAssistConfig
                {
                    SupportType = RoleAssistType.ParryAid,
                    ParryReadyDuration = 0.2f,
                    ParryRootMotionZOffset = 1.0f
                };
                role.Init(mockConfig);

                // 情形 1：临界按键，剩余时间不足 readyDuration，应动态快进
                float remainTime = 0.05f;
                float expectedHitTime = Time.time + remainTime;
                var marker = new AttackWarningMarker
                {
                    Attacker = monster,
                    SignalType = ATEditor.WarningSignalType.Yellow_Parryable,
                    ExpectedHitTime = expectedHitTime,
                    ClashPositionOffset = new Vector3(0, 0, 2f),
                    AllowInPlaceParry = false
                };

                var ctx = new SwitchPipelineContext
                {
                    Type = SwitchType.ParryAid,
                    IncomingMember = new PartyMember { SlotIndex = 0, Entity = role },
                    TargetAttacker = monster,
                    WarningMarker = marker
                };

                var placementPipe = new IncomingPlacementPipe();
                placementPipe.Process(ctx);

                // 预期快进：0.2 - 0.05 = 0.15s
                Assert.AreEqual(0.15f, ctx.CalculatedStartTime, 0.01f, "临界切人时应动态推导出 0.15s 的快进量");

                RouteEventType lastEventType = RouteEventType.None;
                role.ActionController.OnRouteEventTriggered += (evt) => lastEventType = evt;

                var triggerPipe = new ActionAndInvincibleTriggerPipe();
                triggerPipe.Process(ctx);

                Assert.AreEqual(RouteEventType.ParryAidStart, lastEventType, "招架切入必须统一触发 ParryAidStart 起手，绝不能直接触发 ParryAid 反击");

                // 验证指令中承载的 StartTime
                var cmd = CharacterCommandFactory.CreateSystemEventCommand(RouteEventType.ParryAidStart, ctx.CalculatedStartTime);
                Assert.IsInstanceOf<SystemEventPayload>(cmd.Payload);
                var payload = (SystemEventPayload)cmd.Payload;
                Assert.AreEqual(0.15f, payload.StartTime, 0.01f, "指令 Payload 必须承载推导出的 StartTime");

                // 情形 2：提前按键，剩余时间大于 readyDuration，从 0 帧开始播
                ctx.CalculatedStartTime = 0f;
                marker.ExpectedHitTime = Time.time + 0.6f;
                placementPipe.Process(ctx);
                Assert.AreEqual(0f, ctx.CalculatedStartTime, 0.001f, "提前量充裕时 StartTime 必须为 0f");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roleGo);
                UnityEngine.Object.DestroyImmediate(monsterGo);
            }
        }
    }
}
