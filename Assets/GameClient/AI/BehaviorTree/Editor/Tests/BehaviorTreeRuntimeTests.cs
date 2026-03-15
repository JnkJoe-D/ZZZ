// using System.Collections.Generic;
// using Game.AI;
// using NUnit.Framework;
// using UnityEngine;

// namespace Game.AI.Tests
// {
//     public sealed class BehaviorTreeRuntimeTests
//     {
//         [Test]
//         public void Blackboard_SetValue_NormalizesAndRaisesEvent()
//         {
//             BehaviorTreeBlackboard blackboard = new BehaviorTreeBlackboard(new[]
//             {
//                 CreateBlackboardEntry("IsEnabled", BehaviorTreeBlackboardValueType.Bool, true),
//                 CreateBlackboardEntry("Health", BehaviorTreeBlackboardValueType.Int, 10)
//             });

//             BehaviorTreeBlackboardChange? lastChange = null;
//             blackboard.ValueChanged += change => lastChange = change;

//             bool changed = blackboard.SetValue("Health", "25");

//             Assert.That(changed, Is.True);
//             Assert.That(blackboard.TryGetValue("Health", out int health), Is.True);
//             Assert.That(health, Is.EqualTo(25));
//             Assert.That(blackboard.TryGetValueData("Health", out BehaviorTreeValueData valueData), Is.True);
//             Assert.That(valueData.ValueType, Is.EqualTo(BehaviorTreeBlackboardValueType.Int));
//             Assert.That(lastChange.HasValue, Is.True);
//             Assert.That(lastChange.Value.Key, Is.EqualTo("Health"));
//             Assert.That((int)lastChange.Value.OldValue, Is.EqualTo(10));
//             Assert.That((int)lastChange.Value.NewValue, Is.EqualTo(25));
//         }

//         [Test]
//         public void Runtime_ActionAndServiceLifecycle_AreInvoked()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>()
//             };

//             definition.Nodes.Add(CreateRootNode("root", "service"));
//             definition.Nodes.Add(CreateServiceNode("service", "TickSense", 0.5f, "action"));
//             definition.Nodes.Add(CreateActionNode("action", "Attack"));

//             BehaviorTreeRuntimeBindings bindings = new BehaviorTreeRuntimeBindings();
//             CountingServiceHandler serviceHandler = new CountingServiceHandler();
//             CountingActionHandler actionHandler = new CountingActionHandler(
//                 BehaviorTreeNodeStatus.Running,
//                 BehaviorTreeNodeStatus.Running,
//                 BehaviorTreeNodeStatus.Success);

//             bindings.RegisterServiceHandler("TickSense", serviceHandler);
//             bindings.RegisterActionHandler("Attack", actionHandler);

//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Success));

//             Assert.That(serviceHandler.EnterCount, Is.EqualTo(1));
//             Assert.That(serviceHandler.TickCount, Is.EqualTo(1));
//             Assert.That(serviceHandler.ExitCount, Is.EqualTo(1));
//             Assert.That(serviceHandler.LastStopReason, Is.EqualTo(BehaviorTreeNodeStopReason.Completed));

//             Assert.That(actionHandler.EnterCount, Is.EqualTo(1));
//             Assert.That(actionHandler.TickCount, Is.EqualTo(3));
//             Assert.That(actionHandler.ExitCount, Is.EqualTo(1));
//             Assert.That(actionHandler.LastExitStatus, Is.EqualTo(BehaviorTreeNodeStatus.Success));
//             Assert.That(actionHandler.LastStopReason, Is.EqualTo(BehaviorTreeNodeStopReason.Completed));
//         }

//         [Test]
//         public void Selector_LowerPriorityAbort_SwitchesToEarlierConditionBranch()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>
//                 {
//                     CreateBlackboardEntry("CanAttack", BehaviorTreeBlackboardValueType.Bool, false)
//                 }
//             };

//             definition.Nodes.Add(CreateRootNode("root", "selector"));
//             definition.Nodes.Add(CreateCompositeNode("selector", BehaviorTreeCompositeMode.Selector, "condition_high", "fallback_low"));
//             definition.Nodes.Add(CreateConditionNode(
//                 "condition_high",
//                 "CanAttack",
//                 BehaviorTreeComparisonOperator.Equals,
//                 BehaviorTreeAbortMode.LowerPriority,
//                 "high_action",
//                 expectedValue: true));
//             definition.Nodes.Add(CreateActionNode("high_action", "High"));
//             definition.Nodes.Add(CreateActionNode("fallback_low", "Low"));

//             BehaviorTreeRuntimeBindings bindings = new BehaviorTreeRuntimeBindings();
//             CountingActionHandler highHandler = new CountingActionHandler(BehaviorTreeNodeStatus.Running);
//             CountingActionHandler lowHandler = new CountingActionHandler(BehaviorTreeNodeStatus.Running);
//             bindings.RegisterActionHandler("High", highHandler);
//             bindings.RegisterActionHandler("Low", lowHandler);

//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(lowHandler.EnterCount, Is.EqualTo(1));
//             Assert.That(highHandler.EnterCount, Is.EqualTo(0));

//             instance.Context.Blackboard.SetValue("CanAttack", true);
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));

//             Assert.That(highHandler.EnterCount, Is.EqualTo(1));
//             Assert.That(lowHandler.ExitCount, Is.EqualTo(1));
//             Assert.That(lowHandler.LastStopReason, Is.EqualTo(BehaviorTreeNodeStopReason.Aborted));
//         }

//         [Test]
//         public void Condition_CanCompareAgainstAnotherBlackboardKey()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>
//                 {
//                     CreateBlackboardEntry("Health", BehaviorTreeBlackboardValueType.Int, 10),
//                     CreateBlackboardEntry("Threshold", BehaviorTreeBlackboardValueType.Int, 8)
//                 }
//             };

//             definition.Nodes.Add(CreateRootNode("root", "compare_condition"));
//             definition.Nodes.Add(CreateConditionNode(
//                 "compare_condition",
//                 "Health",
//                 BehaviorTreeComparisonOperator.GreaterOrEqual,
//                 BehaviorTreeAbortMode.Self,
//                 "attack_action",
//                 expectedBlackboardKey: "Threshold"));
//             definition.Nodes.Add(CreateActionNode("attack_action", "Attack"));

//             BehaviorTreeRuntimeBindings bindings = new BehaviorTreeRuntimeBindings();
//             CountingActionHandler actionHandler = new CountingActionHandler(BehaviorTreeNodeStatus.Success);
//             bindings.RegisterActionHandler("Attack", actionHandler);

//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Success));
//             Assert.That(actionHandler.EnterCount, Is.EqualTo(1));

//             instance.Context.Blackboard.SetValue("Threshold", 12);
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Failure));
//             Assert.That(actionHandler.EnterCount, Is.EqualTo(1));
//         }

//         [Test]
//         public void CharacterSyncService_WritesBlackboardState()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>()
//             };

//             definition.Nodes.Add(CreateRootNode("root", "service"));
//             definition.Nodes.Add(CreateServiceNode("service", BehaviorTreeCharacterServiceKeys.SyncState, 0f, "idle"));
//             definition.Nodes.Add(CreateActionNode("idle", BehaviorTreeCharacterTaskKeys.Idle));

//             FakeCharacterFacade facade = new FakeCharacterFacade
//             {
//                 MoveInput = new Vector2(0.25f, -0.5f),
//                 IsDashHeld = true,
//                 HasMoveInput = true,
//                 CanEvade = true,
//                 IsGrounded = true,
//                 IsGroundState = true,
//                 CurrentStateName = "CharacterGroundState"
//             };

//             BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(facade);
//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.CurrentMoveX), Is.EqualTo(0.25f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.CurrentMoveY), Is.EqualTo(-0.5f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<bool>(BehaviorTreeCharacterBlackboardKeys.HasMoveInput), Is.True);
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<bool>(BehaviorTreeCharacterBlackboardKeys.CanEvade), Is.True);
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<bool>(BehaviorTreeCharacterBlackboardKeys.IsGroundState), Is.True);
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<string>(BehaviorTreeCharacterBlackboardKeys.CurrentState), Is.EqualTo("CharacterGroundState"));
//         }

//         [Test]
//         public void CharacterMoveAction_ReadsBlackboardAndDrivesFacade()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>
//                 {
//                     CreateBlackboardEntry(BehaviorTreeCharacterBlackboardKeys.MoveX, BehaviorTreeBlackboardValueType.Float, 0.5f),
//                     CreateBlackboardEntry(BehaviorTreeCharacterBlackboardKeys.MoveY, BehaviorTreeBlackboardValueType.Float, 0.25f),
//                 }
//             };

//             definition.Nodes.Add(CreateRootNode("root", "move"));
//             definition.Nodes.Add(CreateActionNode("move", BehaviorTreeCharacterTaskKeys.Move));

//             FakeCharacterFacade facade = new FakeCharacterFacade();
//             BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(facade);

//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(facade.LastMovement.x, Is.EqualTo(0.5f).Within(0.0001f));
//             Assert.That(facade.LastMovement.y, Is.EqualTo(0.25f).Within(0.0001f));
//             Assert.That(facade.LastDashHeld, Is.True);
//             Assert.That(facade.SetMovementCallCount, Is.EqualTo(1));
//         }

//         [Test]
//         public void CharacterAttackAction_TriggersOnceAndCompletesAfterLeavingSkillState()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>()
//             };

//             definition.Nodes.Add(CreateRootNode("root", "attack"));
//             definition.Nodes.Add(CreateActionNode("attack", BehaviorTreeCharacterTaskKeys.BasicAttack));

//             FakeCharacterFacade facade = new FakeCharacterFacade
//             {
//                 OnBasicAttackTriggered = target => target.IsSkillState = true
//             };

//             BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(facade);
//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(facade.BasicAttackTriggerCount, Is.EqualTo(1));
//             Assert.That(facade.IsSkillState, Is.True);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(facade.BasicAttackTriggerCount, Is.EqualTo(1));

//             facade.IsSkillState = false;
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Success));
//             Assert.That(facade.BasicAttackTriggerCount, Is.EqualTo(1));
//         }

//         [Test]
//         public void CharacterTargetSyncService_WritesTargetState()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>()
//             };

//             definition.Nodes.Add(CreateRootNode("root", "service"));
//             definition.Nodes.Add(CreateServiceNode("service", BehaviorTreeCharacterServiceKeys.SyncTarget, 0f, "idle"));
//             definition.Nodes.Add(CreateActionNode("idle", BehaviorTreeCharacterTaskKeys.Idle));

//             FakeCharacterFacade facade = new FakeCharacterFacade
//             {
//                 WorldPosition = new Vector3(1f, 0f, 2f),
//                 Forward = Vector3.forward
//             };
//             FakeTargetProvider targetProvider = new FakeTargetProvider
//             {
//                 HasTarget = true,
//                 TargetData = new BehaviorTreeTargetData(new Vector3(4f, 0f, 6f), "Hero", 42)
//             };

//             BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(facade, targetProvider);
//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<bool>(BehaviorTreeCharacterBlackboardKeys.HasTarget), Is.True);
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionX), Is.EqualTo(4f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionY), Is.EqualTo(0f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionZ), Is.EqualTo(6f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetDistance), Is.EqualTo(5f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetHorizontalDistance), Is.EqualTo(5f).Within(0.0001f));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<string>(BehaviorTreeCharacterBlackboardKeys.TargetName), Is.EqualTo("Hero"));
//             Assert.That(instance.Context.Blackboard.GetValueOrDefault<int>(BehaviorTreeCharacterBlackboardKeys.TargetInstanceId), Is.EqualTo(42));
//         }

//         [Test]
//         public void CharacterChaseTargetAction_MovesUntilWithinStopDistance()
//         {
//             BehaviorTreeDefinition definition = new BehaviorTreeDefinition
//             {
//                 RootNodeId = "root",
//                 Blackboard = new List<BehaviorTreeBlackboardEntry>
//                 {
//                     CreateBlackboardEntry(BehaviorTreeCharacterBlackboardKeys.TargetStopDistance, BehaviorTreeBlackboardValueType.Float, 0.5f),
//                 }
//             };

//             definition.Nodes.Add(CreateRootNode("root", "chase"));
//             definition.Nodes.Add(CreateActionNode("chase", BehaviorTreeCharacterTaskKeys.ChaseTarget));

//             FakeCharacterFacade facade = new FakeCharacterFacade
//             {
//                 WorldPosition = Vector3.zero,
//                 Forward = Vector3.forward
//             };
//             FakeTargetProvider targetProvider = new FakeTargetProvider
//             {
//                 HasTarget = true,
//                 TargetData = new BehaviorTreeTargetData(new Vector3(3f, 0f, 4f), "Hero", 7)
//             };

//             BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(facade, targetProvider);
//             using BehaviorTreeInstance instance = new BehaviorTreeInstance(definition, runtimeBindings: bindings);

//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Running));
//             Assert.That(facade.LastMovement.x, Is.EqualTo(0.6f).Within(0.0001f));
//             Assert.That(facade.LastMovement.y, Is.EqualTo(0.8f).Within(0.0001f));
//             Assert.That(facade.LastDashHeld, Is.True);

//             facade.WorldPosition = new Vector3(2.8f, 0f, 3.8f);
//             Assert.That(instance.Tick(0.1f), Is.EqualTo(BehaviorTreeNodeStatus.Success));
//             Assert.That(facade.LastMovement, Is.EqualTo(Vector2.zero));
//             Assert.That(facade.HasMoveInput, Is.False);
//         }

//         [Test]
//         public void TargetSelector_SelectsClosestCharacterWithinRange()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = false,
//                 RetainDistanceMultiplier = 1.25f
//             };

//             BehaviorTreeTargetData[] candidates =
//             {
//                 new BehaviorTreeTargetData(new Vector3(6f, 0f, 0f), "Far", 1),
//                 new BehaviorTreeTargetData(new Vector3(2f, 0f, 1f), "Near", 2),
//                 new BehaviorTreeTargetData(new Vector3(20f, 0f, 0f), "OutOfRange", 3)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 null,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(2));
//             Assert.That(selectedTarget.Name, Is.EqualTo("Near"));
//         }

//         [Test]
//         public void TargetSelector_PrefersLocalPlayerWhenConfigured()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.LocalPlayerPreferred,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = false,
//                 RetainDistanceMultiplier = 1.25f
//             };

//             BehaviorTreeTargetData[] candidates =
//             {
//                 new BehaviorTreeTargetData(new Vector3(2f, 0f, 0f), "Enemy", 1, false),
//                 new BehaviorTreeTargetData(new Vector3(7f, 0f, 0f), "Player", 2, true)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 null,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(2));
//             Assert.That(selectedTarget.IsPlayerControlled, Is.True);
//         }

//         [Test]
//         public void TargetSelector_RetainsCurrentTargetWithinRetainDistance()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 8f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = true,
//                 RetainDistanceMultiplier = 1.5f
//             };

//             BehaviorTreeTargetData currentTarget = new BehaviorTreeTargetData(new Vector3(7f, 0f, 0f), "Current", 5);
//             BehaviorTreeTargetData[] candidates =
//             {
//                 currentTarget,
//                 new BehaviorTreeTargetData(new Vector3(2f, 0f, 0f), "Closer", 6)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 currentTarget,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(5));
//         }

//         [Test]
//         public void TargetSelector_RetainedTargetUsesLatestCandidatePosition()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = true,
//                 RetainDistanceMultiplier = 1.5f
//             };

//             BehaviorTreeTargetData previousTarget = new BehaviorTreeTargetData(new Vector3(2f, 0f, 0f), "Current", 5);
//             BehaviorTreeTargetData movedTarget = new BehaviorTreeTargetData(new Vector3(6f, 0f, 0f), "Current", 5);
//             BehaviorTreeTargetData[] candidates =
//             {
//                 movedTarget
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 previousTarget,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(5));
//             Assert.That(selectedTarget.Position, Is.EqualTo(movedTarget.Position));
//         }

//         [Test]
//         public void TargetSelector_FiltersByDifferentFaction()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.DifferentFaction,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = false,
//                 RetainDistanceMultiplier = 1.25f
//             };

//             BehaviorTreeTargetData[] candidates =
//             {
//                 new BehaviorTreeTargetData(new Vector3(2f, 0f, 0f), "Ally", 1, false, 2),
//                 new BehaviorTreeTargetData(new Vector3(4f, 0f, 0f), "Enemy", 2, false, 3)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 null,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(2));
//             Assert.That(selectedTarget.FactionId, Is.EqualTo(3));
//         }

//         [Test]
//         public void TargetSelector_FiltersByPlayerOnly()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.PlayerOnly,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 360f,
//                 RetainCurrentTarget = false,
//                 RetainDistanceMultiplier = 1.25f
//             };

//             BehaviorTreeTargetData[] candidates =
//             {
//                 new BehaviorTreeTargetData(new Vector3(1f, 0f, 0f), "NPC", 1, false),
//                 new BehaviorTreeTargetData(new Vector3(5f, 0f, 0f), "Player", 2, true)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 null,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(2));
//             Assert.That(selectedTarget.IsPlayerControlled, Is.True);
//         }

//         [Test]
//         public void TargetSelector_FiltersByFieldOfView()
//         {
//             BehaviorTreeTargetSelectionOptions options = new BehaviorTreeTargetSelectionOptions
//             {
//                 SelectionMode = BehaviorTreeTargetSelectionMode.ClosestCharacter,
//                 FactionFilter = BehaviorTreeTargetFactionFilter.Any,
//                 ControlFilter = BehaviorTreeTargetControlFilter.Any,
//                 MinDistance = 0f,
//                 MaxDistance = 10f,
//                 FieldOfViewDegrees = 90f,
//                 RetainCurrentTarget = false,
//                 RetainDistanceMultiplier = 1.25f
//             };

//             BehaviorTreeTargetData[] candidates =
//             {
//                 new BehaviorTreeTargetData(new Vector3(0f, 0f, -2f), "Behind", 1),
//                 new BehaviorTreeTargetData(new Vector3(2f, 0f, 2f), "Front", 2)
//             };

//             bool found = BehaviorTreeTargetSelector.TrySelectTarget(
//                 Vector3.zero,
//                 Vector3.forward,
//                 2,
//                 options,
//                 candidates,
//                 null,
//                 out BehaviorTreeTargetData selectedTarget);

//             Assert.That(found, Is.True);
//             Assert.That(selectedTarget.InstanceId, Is.EqualTo(2));
//         }

//         private static BehaviorTreeBlackboardEntry CreateBlackboardEntry(string key, BehaviorTreeBlackboardValueType valueType, object value)
//         {
//             BehaviorTreeValueData valueData = BehaviorTreeValueData.CreateDefault(valueType);
//             valueData.SetFromObject(value);
//             return new BehaviorTreeBlackboardEntry
//             {
//                 Key = key,
//                 DisplayName = key,
//                 ValueType = valueType,
//                 SerializedTypeName = valueType.ToString(),
//                 DefaultValueData = valueData
//             };
//         }

//         private static BehaviorTreeDefinitionNode CreateRootNode(string nodeId, string childId)
//         {
//             return new BehaviorTreeDefinitionNode
//             {
//                 NodeId = nodeId,
//                 NodeKind = BehaviorTreeNodeKind.Root,
//                 Children = new List<string> { childId }
//             };
//         }

//         private static BehaviorTreeDefinitionNode CreateCompositeNode(string nodeId, BehaviorTreeCompositeMode compositeMode, params string[] children)
//         {
//             return new BehaviorTreeDefinitionNode
//             {
//                 NodeId = nodeId,
//                 NodeKind = BehaviorTreeNodeKind.Composite,
//                 CompositeMode = compositeMode,
//                 Children = new List<string>(children)
//             };
//         }

//         private static BehaviorTreeDefinitionNode CreateServiceNode(string nodeId, string serviceKey, float intervalSeconds, string childId)
//         {
//             return new BehaviorTreeDefinitionNode
//             {
//                 NodeId = nodeId,
//                 NodeKind = BehaviorTreeNodeKind.Service,
//                 ServiceKey = serviceKey,
//                 IntervalSeconds = intervalSeconds,
//                 Children = new List<string> { childId }
//             };
//         }

//         private static BehaviorTreeDefinitionNode CreateActionNode(string nodeId, string taskKey)
//         {
//             return new BehaviorTreeDefinitionNode
//             {
//                 NodeId = nodeId,
//                 NodeKind = BehaviorTreeNodeKind.Action,
//                 TaskKey = taskKey
//             };
//         }

//         private static BehaviorTreeDefinitionNode CreateConditionNode(
//             string nodeId,
//             string blackboardKey,
//             BehaviorTreeComparisonOperator comparison,
//             BehaviorTreeAbortMode abortMode,
//             string childId,
//             bool? expectedValue = null,
//             string expectedBlackboardKey = null)
//         {
//             BehaviorTreeValueData expectedValueData = BehaviorTreeValueData.CreateDefault(BehaviorTreeBlackboardValueType.Bool);
//             expectedValueData.SetFromObject(expectedValue ?? false);
//             return new BehaviorTreeDefinitionNode
//             {
//                 NodeId = nodeId,
//                 NodeKind = BehaviorTreeNodeKind.Condition,
//                 BlackboardKey = blackboardKey,
//                 Comparison = comparison,
//                 AbortMode = abortMode,
//                 ExpectedValueSource = string.IsNullOrWhiteSpace(expectedBlackboardKey)
//                     ? BehaviorTreeConditionValueSource.Constant
//                     : BehaviorTreeConditionValueSource.BlackboardKey,
//                 ExpectedBlackboardKey = expectedBlackboardKey ?? string.Empty,
//                 ExpectedValueData = expectedValueData,
//                 Children = new List<string> { childId }
//             };
//         }

//         private sealed class CountingActionHandler : IBehaviorTreeActionHandler
//         {
//             private readonly Queue<BehaviorTreeNodeStatus> statusQueue;
//             private readonly BehaviorTreeNodeStatus fallbackStatus;

//             public CountingActionHandler(params BehaviorTreeNodeStatus[] statuses)
//             {
//                 statusQueue = new Queue<BehaviorTreeNodeStatus>(statuses);
//                 fallbackStatus = statuses.Length > 0 ? statuses[statuses.Length - 1] : BehaviorTreeNodeStatus.Running;
//             }

//             public int EnterCount { get; private set; }
//             public int TickCount { get; private set; }
//             public int ExitCount { get; private set; }
//             public BehaviorTreeNodeStatus LastExitStatus { get; private set; }
//             public BehaviorTreeNodeStopReason LastStopReason { get; private set; }

//             public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
//             {
//                 EnterCount++;
//             }

//             public BehaviorTreeNodeStatus Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
//             {
//                 TickCount++;
//                 return statusQueue.Count > 0 ? statusQueue.Dequeue() : fallbackStatus;
//             }

//             public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStatus lastStatus, BehaviorTreeNodeStopReason stopReason)
//             {
//                 ExitCount++;
//                 LastExitStatus = lastStatus;
//                 LastStopReason = stopReason;
//             }
//         }

//         private sealed class CountingServiceHandler : IBehaviorTreeServiceHandler
//         {
//             public int EnterCount { get; private set; }
//             public int TickCount { get; private set; }
//             public int ExitCount { get; private set; }
//             public BehaviorTreeNodeStopReason LastStopReason { get; private set; }

//             public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
//             {
//                 EnterCount++;
//             }

//             public void Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
//             {
//                 TickCount++;
//             }

//             public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStopReason stopReason)
//             {
//                 ExitCount++;
//                 LastStopReason = stopReason;
//             }
//         }

//         private sealed class FakeCharacterFacade : IBehaviorTreeCharacterFacade
//         {
//             public Vector2 MoveInput { get; set; }
//             public bool IsDashHeld { get; set; }
//             public bool HasMoveInput { get; set; }
//             public bool CanEvade { get; set; }
//             public bool IsGrounded { get; set; }
//             public bool IsGroundState { get; set; }
//             public bool IsAirborneState { get; set; }
//             public bool IsSkillState { get; set; }
//             public bool IsEvadeState { get; set; }
//             public string CurrentStateName { get; set; } = string.Empty;
//             public Vector3 WorldPosition { get; set; }
//             public Vector3 Forward { get; set; } = Vector3.forward;
//             public Vector2 LastMovement { get; private set; }
//             public bool LastDashHeld { get; private set; }
//             public int SetMovementCallCount { get; private set; }
//             public int ClearMovementCallCount { get; private set; }
//             public int BasicAttackTriggerCount { get; private set; }
//             public int SpecialAttackTriggerCount { get; private set; }
//             public int UltimateTriggerCount { get; private set; }
//             public int EvadeTriggerCount { get; private set; }
//             public System.Action<FakeCharacterFacade> OnBasicAttackTriggered { get; set; }
//             public System.Action<FakeCharacterFacade> OnSpecialAttackTriggered { get; set; }
//             public System.Action<FakeCharacterFacade> OnUltimateTriggered { get; set; }
//             public System.Action<FakeCharacterFacade> OnEvadeTriggered { get; set; }

//             public void SetMovement(Vector2 direction)
//             {
//                 LastMovement = direction;
//                 MoveInput = direction;
//                 HasMoveInput = direction.sqrMagnitude > 0.0001f;
//                 WorldPosition += new Vector3(direction.x, 0f, direction.y);
//                 SetMovementCallCount++;
//             }

//             public void ClearMovement()
//             {
//                 LastMovement = Vector2.zero;
//                 LastDashHeld = false;
//                 MoveInput = Vector2.zero;
//                 IsDashHeld = false;
//                 HasMoveInput = false;
//                 ClearMovementCallCount++;
//             }

//             public bool TriggerBasicAttack()
//             {
//                 BasicAttackTriggerCount++;
//                 OnBasicAttackTriggered?.Invoke(this);
//                 return true;
//             }

//             public bool TriggerSpecialAttack()
//             {
//                 SpecialAttackTriggerCount++;
//                 OnSpecialAttackTriggered?.Invoke(this);
//                 return true;
//             }

//             public bool TriggerUltimate()
//             {
//                 UltimateTriggerCount++;
//                 OnUltimateTriggered?.Invoke(this);
//                 return true;
//             }

//             public bool TriggerEvadeBack()
//             {
//                 EvadeTriggerCount++;
//                 OnEvadeTriggered?.Invoke(this);
//                 return true;
//             }
//         }

//         private sealed class FakeTargetProvider : IBehaviorTreeTargetProvider
//         {
//             public bool HasTarget { get; set; }
//             public BehaviorTreeTargetData TargetData { get; set; }

//             public bool TryGetTarget(out BehaviorTreeTargetData targetData)
//             {
//                 targetData = TargetData;
//                 return HasTarget;
//             }
//         }
//     }
// }
