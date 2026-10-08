using System;
using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    public class ActionWindowRouteAndDomainTests
    {
        private GameObject _roleGo;
        private RoleEntity _role;
        private GameObject _monsterGo;
        private MonsterEntity _monster;

        [SetUp]
        public void SetUp()
        {
            _roleGo = new GameObject("TestRole_WindowRoute");
            _role = _roleGo.AddComponent<RoleEntity>();
            _role.EnsureRuntimeInitialized();

            _monsterGo = new GameObject("TestMonster_WindowRoute");
            _monster = _monsterGo.AddComponent<MonsterEntity>();
            var monsterConfig = ScriptableObject.CreateInstance<MonsterConfigAsset>();
            _monster.Init(monsterConfig);
        }

        [TearDown]
        public void TearDown()
        {
            if (_roleGo != null) UnityEngine.Object.DestroyImmediate(_roleGo);
            if (_monsterGo != null) UnityEngine.Object.DestroyImmediate(_monsterGo);
        }

        [Test]
        public void Test01_WindowRouteTable_BucketingPartitioningCorrectness()
        {
            // 创建测试动作与路由
            var actionConfig = ScriptableObject.CreateInstance<RoleActionConfigAsset>();

            var windowA = new BufferRouteWindow { Tag = "LightAttack" };
            var windowB = new AutoRouteWindow { Tag = "AutoChain" };

            var route1 = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = windowA },
                ExecuteType = ExecuteTarget.Action,
                ExecuteAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>()
            };

            var route2 = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = windowB },
                ExecuteType = ExecuteTarget.Action,
                ExecuteAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>()
            };

            var globalRoute = new ActionRoute
            {
                TriggerStrategy = new DirectAssetTrigger { RequiredWindow = null },
                ExecuteType = ExecuteTarget.Action,
                ExecuteAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>()
            };

            actionConfig.Routes.Add(route1);
            actionConfig.Routes.Add(route2);
            actionConfig.Routes.Add(globalRoute);

            // 构建快照表
            ActionWindowRouteTable table = ActionRouteTableBuilder.BuildTable(actionConfig, null);
            Assert.IsNotNull(table, "ActionWindowRouteTable 必须成功构建");
            Assert.AreEqual(1, table.GlobalRoutes.Length, "未限制窗口的路由必须收集到 GlobalRoutes");
            Assert.AreEqual(globalRoute, table.GlobalRoutes[0]);

            // 查询 windowA 的路由分桶
            IReadOnlyList<ActionRoute> sliceA = table.GetRoutesForWindow(windowA);
            Assert.AreEqual(1, sliceA.Count, "windowA 分桶必须精确包含 1 条路由");
            Assert.AreEqual(route1, sliceA[0]);

            // 查询 windowB 的路由分桶
            IReadOnlyList<ActionRoute> sliceB = table.GetRoutesForWindow(windowB);
            Assert.AreEqual(1, sliceB.Count, "windowB 分桶必须精确包含 1 条路由");
            Assert.AreEqual(route2, sliceB[0]);

            // 运行时通过 ActionRouteRuntimeResolver 提取
            var effectiveList = new List<ActionRoute>();
            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(actionConfig, windowA, null, effectiveList);

            // 100% 窗口隔离铁律：解析结果只包含当前活跃窗口 windowA 的专属路由，绝不掺杂未绑定窗口的路由或其它窗口路由！
            Assert.AreEqual(1, effectiveList.Count, "解析结果必须精准且仅包含当前活跃窗口分桶路由");
            Assert.Contains(route1, effectiveList);
            Assert.IsFalse(effectiveList.Contains(globalRoute), "未挂载具体窗口的路由绝不在任何窗口中激活！");
            Assert.IsFalse(effectiveList.Contains(route2), "绝不包含其他不相关窗口的路由！");
        }

        [Test]
        public void Test02_AutoRouteWindow_CompletelyIsolatedFromInputWindows()
        {
            var actionConfig = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            var autoWindow = new AutoRouteWindow { Tag = "AutoFinish" };
            var bufferWindow = new BufferRouteWindow { Tag = "ComboBranch" };

            var autoRoute = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = autoWindow },
                ExecuteType = ExecuteTarget.Action
            };
            var comboRoute = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = bufferWindow },
                ExecuteType = ExecuteTarget.Action
            };

            actionConfig.Routes.Add(autoRoute);
            actionConfig.Routes.Add(comboRoute);

            var resolvedForAuto = new List<ActionRoute>();
            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(actionConfig, autoWindow, null, resolvedForAuto);

            Assert.AreEqual(1, resolvedForAuto.Count);
            Assert.AreEqual(autoRoute, resolvedForAuto[0], "AutoRouteWindow 仅且必须仅提取属于自身的自动流转路由");

            var resolvedForCombo = new List<ActionRoute>();
            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(actionConfig, bufferWindow, null, resolvedForCombo);

            Assert.AreEqual(1, resolvedForCombo.Count);
            Assert.AreEqual(comboRoute, resolvedForCombo[0], "按键指令窗口仅且必须仅提取属于自身的按键路由");
        }

        [Test]
        public void Test03_RouteInheritMode_ProjectsParentRoutesSafely()
        {
            // 父动作 ParentAction，拥有 WindowA
            var parentAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            parentAction.ID = 1001;
            var windowA = new BufferRouteWindow { Tag = "BranchA" };
            var parentRoute = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = windowA },
                ExecuteType = ExecuteTarget.Action
            };
            parentAction.Routes.Add(parentRoute);

            // 子动作 ChildAction，拥有 WindowA，且设置为 InheritPrioritizeSelf
            var childAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            childAction.ID = 1002;
            childAction.InheritMode = RouteInheritMode.InheritPrioritizeSelf;
            var childRoute = new ActionRoute
            {
                TriggerStrategy = new IntentCommandTrigger { RequiredWindow = windowA },
                ExecuteType = ExecuteTarget.Action
            };
            childAction.Routes.Add(childRoute);

            // 模拟运行时从 parent 转移到 child
            var data = _role.DataModule.Get<ActionRuntimeData>();
            data.Set("CurrentPlayingAction", parentAction);

            var resolved = new List<ActionRoute>();
            ActionRouteRuntimeResolver.ResolveEffectiveWindowRoutes(childAction, windowA, _role, resolved);

            // 自身优先，childRoute 在前，parentRoute 在后
            Assert.AreEqual(2, resolved.Count, "继承模式必须聚合前置动作与当前动作针对同窗口的路由");
            Assert.AreEqual(childRoute, resolved[0]);
            Assert.AreEqual(parentRoute, resolved[1]);
        }

        [Test]
        public void Test04_ImmediateArbitrationTiming_MarksCandidateImmediately()
        {
            var routeImmediate = new ActionRoute
            {
                TriggerStrategy = new DirectAssetTrigger(),
                ArbitrationTiming = RouteArbitrationTiming.Immediate,
                Priority = 100,
                ExecuteType = ExecuteTarget.Action
            };

            var routeDeferred = new ActionRoute
            {
                TriggerStrategy = new DirectAssetTrigger(),
                ArbitrationTiming = RouteArbitrationTiming.Deferred,
                Priority = 50,
                ExecuteType = ExecuteTarget.Action
            };

            var arbitrator = new RouteArbitrator();
            Assert.IsFalse(arbitrator.HasImmediateCandidate, "初始状态下不得存在即时候选");

            var candidateDeferred = new RouteCandidate
            {
                SourceRoute = routeDeferred,
                ArbitrationTiming = routeDeferred.ArbitrationTiming,
                Priority = routeDeferred.Priority,
                ExecuteType = routeDeferred.ExecuteType
            };
            arbitrator.Submit(candidateDeferred);
            Assert.IsFalse(arbitrator.HasImmediateCandidate, "提交延迟仲裁候选后 HasImmediateCandidate 仍为 false");

            var candidateImmediate = new RouteCandidate
            {
                SourceRoute = routeImmediate,
                ArbitrationTiming = routeImmediate.ArbitrationTiming,
                Priority = routeImmediate.Priority,
                ExecuteType = routeImmediate.ExecuteType
            };
            arbitrator.Submit(candidateImmediate);
            Assert.IsTrue(arbitrator.HasImmediateCandidate, "提交即时仲裁候选后 HasImmediateCandidate 必须为 true！");

            arbitrator.Clear();
            Assert.IsFalse(arbitrator.HasImmediateCandidate, "清空仲裁池后 HasImmediateCandidate 必须复位");
        }

        [Test]
        public void Test05_ActionDomain_PolymorphicTransitions()
        {
            var context = _role.DomainContext;
            Assert.IsNotNull(context, "RoleEntity 必须自动装配并持有 ActionDomainContextModule");

            var locoAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            locoAction.DomainId = ActionDomainId.Locomotion;
            Assert.AreEqual(ActionDomainId.Locomotion, locoAction.DomainId);

            var combatAction1 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            combatAction1.DomainId = ActionDomainId.Combat;
            Assert.AreEqual(ActionDomainId.Combat, combatAction1.DomainId);

            var combatAction2 = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            combatAction2.DomainId = ActionDomainId.Combat;

            // 1. 切入 Locomotion
            context.HandleActionChanged(locoAction);
            Assert.AreEqual(ActionDomainId.Locomotion, context.CurrentDomainId);
            Assert.IsInstanceOf<RoleLocomotionDomain>(context.CurrentDomain);

            // 2. 切入 Combat
            context.HandleActionChanged(combatAction1);
            Assert.AreEqual(ActionDomainId.Combat, context.CurrentDomainId);
            var combatDomain = context.GetDomain<RoleCombatDomain>(ActionDomainId.Combat);
            Assert.AreEqual(1, combatDomain.CurrentComboIndex);

            // 3. 同域切招 (Attack1 -> Attack2)，平滑推进而不销毁/重新进入
            context.HandleActionChanged(combatAction2);
            Assert.AreEqual(ActionDomainId.Combat, context.CurrentDomainId);
            Assert.AreEqual(2, combatDomain.CurrentComboIndex, "同域切招必须平滑累加 Combo 计数");

            // 4. 切入受击硬直
            var hitAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            hitAction.DomainId = ActionDomainId.HitReaction;
            context.HandleActionChanged(hitAction);
            Assert.AreEqual(ActionDomainId.HitReaction, context.CurrentDomainId);
            Assert.IsFalse(context.CanAcceptCommand(null), "受击硬直领域必须静默阻断外部输入");
        }

        [Test]
        public void Test06_MonsterEntity_PreservesPassiveDomainWithoutSideEffects()
        {
            // MonsterEntity 默认采用 PassiveActionDomain
            var monsterAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            monsterAction.DomainId = ActionDomainId.Combat;

            // MonsterEntity 的动作流转与 AI FSM 必须安全隔离
            Assert.IsNotNull(_monster.StateMachine, "怪物的 AI 状态机必须独立运行");
            Assert.IsNotNull(_monster.RouteArbitrator, "怪物的仲裁器必须独立工作");
        }

        [Test]
        public void Test07_NoActiveWindow_MustNeverRespondToInput()
        {
            var actionWithoutWindow = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            actionWithoutWindow.DomainId = ActionDomainId.Combat;

            var attackRoute = new ActionRoute
            {
                ExecuteType = ExecuteTarget.Action,
                ExecuteAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>(),
                Priority = 10,
                TriggerStrategy = new IntentCommandTrigger
                {
                    RequiredInput = HardwareInputType.BasicAttack,
                    RequiredPhase = CommandPhase.Started
                }
            };
            actionWithoutWindow.Routes = new List<ActionRoute> { attackRoute };

            var roleConfig = ScriptableObject.CreateInstance<RoleConfigAsset>();
            roleConfig.ActionRoot = actionWithoutWindow;
            _role.Init(roleConfig);

            // 确认当前无活跃窗口
            Assert.AreEqual(0, _role.ActionController.ActiveRouteWindowsCount);

            // 压入攻击指令
            var cmd = CharacterCommandFactory.Create(HardwareInputType.BasicAttack, CommandPhase.Started, null);
            _role.ActionController.OnInput(cmd);

            // 验证断言：无活跃窗口时绝对不响应任何指令，仲裁池中不得存在任何候选！
            Assert.IsFalse(_role.RouteArbitrator.HasCandidates, "无活跃窗口时绝对不允许将指令录入仲裁池！");
        }

        [Test]
        public void Test08_UnifiedOnInput_AutomaticallyResolvesImmediateRoutes()
        {
            var currentAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            currentAction.DomainId = ActionDomainId.Combat;

            var hitStunAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            hitStunAction.DomainId = ActionDomainId.HitReaction;

            // 配置一条 Immediate 即时抢占的受击路由挂在当前窗口下
            var hitRoute = new ActionRoute
            {
                ExecuteType = ExecuteTarget.Action,
                ExecuteAction = hitStunAction,
                Priority = 100,
                ArbitrationTiming = RouteArbitrationTiming.Immediate, // 资产配置为立即裁决！
                TriggerStrategy = new DirectAssetTrigger
                {
                    RequiredWindow = new ExecuteRouteWindow { Tag = "StunWindow" }
                }
            };
            currentAction.Routes = new List<ActionRoute> { hitRoute };

            var roleConfig = ScriptableObject.CreateInstance<RoleConfigAsset>();
            roleConfig.ActionRoot = currentAction;
            _role.Init(roleConfig);

            _role.ActionController.OnWindowEnter(new ExecuteRouteWindow { Tag = "StunWindow" });

            // 外部统一只调用 OnInput！
            var hitCmd = CharacterCommandFactory.CreateDirectAssetCommand(hitStunAction);
            _role.ActionController.OnInput(hitCmd);

            // 验证断言：OnInput 内部通过 HasImmediateCandidate 自动触发即时决选，角色立即切入受击领域！
            Assert.AreEqual(ActionDomainId.HitReaction, _role.DomainContext.CurrentDomainId, "仅调用 OnInput 即可通过配置的 Immediate 路由自动完成即时决选！");
        }

        [Test]
        public void Test09_PlayRootAction_CanExecuteWithoutWindows()
        {
            var rootAction = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            rootAction.DomainId = ActionDomainId.Locomotion;

            var config = ScriptableObject.CreateInstance<RoleConfigAsset>();
            config.ActionRoot = rootAction;
            _role.Init(config);

            Assert.AreEqual(ActionDomainId.Locomotion, _role.DomainContext.CurrentDomainId);
            Assert.AreEqual(rootAction, _role.ActionController.CurrentPlayingAction);
        }
    }
}
