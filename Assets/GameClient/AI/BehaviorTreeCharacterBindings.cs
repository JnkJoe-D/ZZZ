using System;
using Game.Input;
using Game.Logic.Character;
using Game.Logic.Player;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 角色相关黑板键常量表。
    /// </summary>
    public static class BehaviorTreeCharacterBlackboardKeys
    {
        public const string MoveX = "MoveX";
        public const string MoveY = "MoveY";
        public const string CurrentMoveX = "CurrentMoveX";
        public const string CurrentMoveY = "CurrentMoveY";
        public const string HasMoveInput = "HasMoveInput";
        public const string CanEvade = "CanEvade";
        public const string IsGrounded = "IsGrounded";
        public const string IsGroundState = "IsGroundState";
        public const string IsAirborneState = "IsAirborneState";
        public const string IsSkillState = "IsSkillState";
        public const string IsEvadeState = "IsEvadeState";
        public const string CurrentState = "CurrentState";
        public const string HasTarget = "HasTarget";
        public const string TargetPositionX = "TargetPositionX";
        public const string TargetPositionY = "TargetPositionY";
        public const string TargetPositionZ = "TargetPositionZ";
        public const string TargetDistance = "TargetDistance";
        public const string TargetHorizontalDistance = "TargetHorizontalDistance";
        public const string TargetName = "TargetName";
        public const string TargetInstanceId = "TargetInstanceId";
        public const string TargetStopDistance = "TargetStopDistance";
        public const string TargetDashDistance = "TargetDashDistance";
        public const string TargetMinDistance = "TargetMinDistance";
        public const string TargetSearchRadius = "TargetSearchRadius";
        public const string TargetFieldOfView = "TargetFieldOfView";
        public const string TargetRetainCurrent = "TargetRetainCurrent";
        public const string TargetRetainDistanceMultiplier = "TargetRetainDistanceMultiplier";
    }

    /// <summary>
    /// 角色相关任务键常量表。
    /// </summary>
    public static class BehaviorTreeCharacterTaskKeys
    {
        public const string Idle = "character.idle";
        public const string Move = "character.move";
        public const string ChaseTarget = "character.chase_target";
        public const string BasicAttack = "character.basic_attack";
        public const string SpecialAttack = "character.special_attack";
        public const string Ultimate = "character.ultimate";
        public const string EvadeFront = "character.evade_front";
        public const string EvadeBack = "character.evade_back";
    }

    /// <summary>
    /// 角色相关服务键常量表。
    /// </summary>
    public static class BehaviorTreeCharacterServiceKeys
    {
        public const string SyncState = "character.sync_state";
        public const string SyncTarget = "character.sync_target";
    }

    /// <summary>
    /// 运行时目标快照。
    /// </summary>
    public readonly struct BehaviorTreeTargetData
    {
        /// <summary>
        /// 构造目标快照。
        /// </summary>
        /// <param name="position">目标位置。</param>
        /// <param name="name">目标名。</param>
        /// <param name="instanceId">目标实例 ID。</param>
        /// <param name="isPlayerControlled">是否为玩家控制。</param>
        /// <param name="factionId">目标阵营 ID。</param>
        public BehaviorTreeTargetData(
            Vector3 position,
            string name = "",
            int instanceId = 0,
            bool isPlayerControlled = false,
            int factionId = 0)
        {
            Position = position;
            Name = name ?? string.Empty;
            InstanceId = instanceId;
            IsPlayerControlled = isPlayerControlled;
            FactionId = factionId;
        }

        public Vector3 Position { get; }
        public string Name { get; }
        public int InstanceId { get; }
        public bool IsPlayerControlled { get; }
        public int FactionId { get; }
    }

    /// <summary>
    /// 目标提供接口。
    /// </summary>
    public interface IBehaviorTreeTargetProvider
    {
        /// <summary>
        /// 尝试获取当前目标。
        /// </summary>
        /// <param name="targetData">输出的目标数据。</param>
        /// <returns>是否获取成功。</returns>
        bool TryGetTarget(out BehaviorTreeTargetData targetData);
    }

    /// <summary>
    /// 行为树与角色系统之间的门面接口。
    /// </summary>
    public interface IBehaviorTreeCharacterFacade
    {
        Vector2 MoveInput { get; }
        bool HasMoveInput { get; }
        bool CanEvade { get; }
        bool IsGrounded { get; }
        bool IsGroundState { get; }
        bool IsAirborneState { get; }
        bool IsSkillState { get; }
        bool IsEvadeState { get; }
        bool IsDashState { get; }
        string CurrentStateName { get; }
        Vector3 WorldPosition { get; }
        Vector3 Forward { get; }
        void SetMovement(Vector2 direction);
        void SetWorldMovement(Vector3 worldDirection);
        void ClearMovement();
        bool TriggerBasicAttack();
        bool TriggerSpecialAttack();
        bool TriggerUltimate();
        bool TriggerEvadeFront();
        bool TriggerEvadeBack();
    }

    /// <summary>
    /// 只把本地玩家作为目标的简单目标提供器。
    /// </summary>
    public sealed class BehaviorTreePlayerTargetProvider : IBehaviorTreeTargetProvider
    {
        private readonly CharacterEntity owner;

        /// <summary>
        /// 构造玩家目标提供器。
        /// </summary>
        /// <param name="owner">发起索敌的 owner。</param>
        public BehaviorTreePlayerTargetProvider(CharacterEntity owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 尝试把本地玩家作为目标返回。
        /// </summary>
        /// <param name="targetData">输出的目标数据。</param>
        /// <returns>是否获取成功。</returns>
        public bool TryGetTarget(out BehaviorTreeTargetData targetData)
        {
            CharacterEntity target = ResolveLocalPlayerCharacter();
            if (target == null || target == owner)
            {
                targetData = default;
                return false;
            }

            targetData = new BehaviorTreeTargetData(
                target.transform.position,
                target.name,
                target.GetInstanceID(),
                true);
            return true;
        }

        /// <summary>
        /// 兼容两套管理器解析本地玩家角色。
        /// </summary>
        private static CharacterEntity ResolveLocalPlayerCharacter()
        {
            return PlayerManager.Instance?.LocalCharacter ?? Game.Logic.Character.CharcterManager.Instance?.LocalCharacter;
        }
    }

    /// <summary>
    /// 角色运行时门面，把行为树动作转换为输入和状态查询。
    /// </summary>
    public sealed class BehaviorTreeCharacterFacade : IBehaviorTreeCharacterFacade
    {
        private readonly CharacterEntity character;
        private readonly AIInputProvider inputProvider;

        /// <summary>
        /// 构造角色门面。
        /// </summary>
        /// <param name="character">目标角色。</param>
        /// <param name="inputProvider">AI 输入代理。</param>
        public BehaviorTreeCharacterFacade(CharacterEntity character, AIInputProvider inputProvider)
        {
            this.character = character;
            this.inputProvider = inputProvider;
        }

        public CharacterEntity Character => character;
        public AIInputProvider InputProvider => inputProvider;
        public Vector2 MoveInput => inputProvider != null ? inputProvider.GetMovementDirection() : Vector2.zero;
        public bool HasMoveInput => inputProvider != null && inputProvider.HasMovementInput();
        public bool CanEvade => character != null && character.CanEvade();
        public bool IsGrounded => character?.MovementController != null && character.MovementController.IsGrounded;
        public bool IsGroundState => character?.StateMachine?.CurrentState is CharacterGroundState;
        public bool IsAirborneState => character?.StateMachine?.CurrentState is CharacterAirborneState;
        public bool IsSkillState => character?.StateMachine?.CurrentState is CharacterSkillState;
        public bool IsEvadeState => character?.StateMachine?.CurrentState is CharacterEvadeState;

        public bool IsDashState => character?.StateMachine?.CurrentState is CharacterGroundState groundState &&
                                   groundState.CurrentSubState == groundState.DashState;

        public string CurrentStateName => character?.StateMachine?.CurrentState?.GetType().Name ?? string.Empty;
        public Vector3 WorldPosition => character != null ? character.transform.position : Vector3.zero;
        public Vector3 Forward => character != null ? character.transform.forward : Vector3.forward;

        /// <summary>设置移动输入。</summary>
        public void SetMovement(Vector2 direction)
        {
            if (inputProvider == null)
            {
                return;
            }

            inputProvider.SetMovementDirection(direction);
        }

        /// <summary>
        /// 以世界空间方向设置移动输入，避免 AI 再被相机方向二次换算。
        /// </summary>
        /// <param name="worldDirection">世界空间方向。</param>
        public void SetWorldMovement(Vector3 worldDirection)
        {
            if (inputProvider == null)
            {
                return;
            }

            inputProvider.SetWorldMovement(worldDirection);
        }

        /// <summary>清空移动输入。</summary>
        public void ClearMovement()
        {
            if (inputProvider == null)
            {
                return;
            }

            inputProvider.ClearMovement();
        }

        /// <summary>尝试触发普攻。</summary>
        public bool TriggerBasicAttack()
        {
            if (inputProvider == null )return false;
            inputProvider.TriggerBasicAttack();
            return true;
        }

        /// <summary>尝试触发特殊技。</summary>
        public bool TriggerSpecialAttack()
        {
            if (inputProvider == null) return false;

            inputProvider.TriggerSpecialAttack();
            return true;
        }

        /// <summary>尝试触发终结技。</summary>
        public bool TriggerUltimate()
        {
            if (inputProvider == null) return false;

            inputProvider.TriggerUltimate();
            return true;
        }

        /// <summary>尝试触发前闪避。</summary>
        public bool TriggerEvadeFront()
        {
            if (inputProvider == null) return false;

            inputProvider.TriggerEvadeFront();
            return true;
        }
        /// <summary>尝试触发后闪避。</summary>
        public bool TriggerEvadeBack()
        {
            if (inputProvider == null) return false;

            inputProvider.TriggerEvadeBack();
            return true;
        }
    }

    /// <summary>
    /// 角色行为树默认绑定工厂。
    /// </summary>
    public static class BehaviorTreeCharacterBindingsFactory
    {
        /// <summary>
        /// 构造一套默认的角色行为树绑定。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        /// <param name="targetProvider">目标提供器。</param>
        /// <returns>默认绑定表。</returns>
        public static BehaviorTreeRuntimeBindings CreateDefault(
            IBehaviorTreeCharacterFacade facade,
            IBehaviorTreeTargetProvider targetProvider = null)
        {
            BehaviorTreeRuntimeBindings bindings = new BehaviorTreeRuntimeBindings();
            if (facade == null)
            {
                return bindings;
            }

            bindings.RegisterServiceHandler(
                BehaviorTreeCharacterServiceKeys.SyncState,
                new SyncCharacterStateServiceHandler(facade));

            if (targetProvider != null)
            {
                bindings.RegisterServiceHandler(
                    BehaviorTreeCharacterServiceKeys.SyncTarget,
                    new SyncCharacterTargetServiceHandler(facade, targetProvider));
            }

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.Idle,
                new CharacterIdleActionHandler(facade));

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.Move,
                new CharacterMoveActionHandler(facade));

            if (targetProvider != null)
            {
                bindings.RegisterActionHandler(
                    BehaviorTreeCharacterTaskKeys.ChaseTarget,
                    new CharacterChaseTargetActionHandler(facade, targetProvider));
            }

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.BasicAttack,
                new TriggerCharacterCommandActionHandler(
                    facade,
                    target => target.TriggerBasicAttack(),
                    target => target.IsSkillState));

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.SpecialAttack,
                new TriggerCharacterCommandActionHandler(
                    facade,
                    target => target.TriggerSpecialAttack(),
                    target => target.IsSkillState));

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.Ultimate,
                new TriggerCharacterCommandActionHandler(
                    facade,
                    target => target.TriggerUltimate(),
                    target => target.IsSkillState));

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.EvadeBack,
                new TriggerCharacterCommandActionHandler(
                    facade,
                    target => target.TriggerEvadeBack(),
                    target => target.IsEvadeState));

            bindings.RegisterActionHandler(
                BehaviorTreeCharacterTaskKeys.EvadeFront,
                new TriggerCharacterCommandActionHandler(
                    facade,
                    target => target.TriggerEvadeFront(),
                    target => target.IsEvadeState));

            return bindings;
        }
    }

    /// <summary>
    /// 把角色当前状态同步到黑板的服务处理器。
    /// </summary>
    internal sealed class SyncCharacterStateServiceHandler : IBehaviorTreeServiceHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;

        /// <summary>
        /// 构造状态同步服务。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        public SyncCharacterStateServiceHandler(IBehaviorTreeCharacterFacade facade)
        {
            this.facade = facade;
        }

        /// <summary>服务进入时的空实现。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
        }

        /// <summary>把当前角色状态写入黑板。</summary>
        public void Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.CurrentMoveX, facade.MoveInput.x);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.CurrentMoveY, facade.MoveInput.y);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.HasMoveInput, facade.HasMoveInput);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.CanEvade, facade.CanEvade);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.IsGrounded, facade.IsGrounded);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.IsGroundState, facade.IsGroundState);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.IsAirborneState, facade.IsAirborneState);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.IsSkillState, facade.IsSkillState);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.IsEvadeState, facade.IsEvadeState);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.CurrentState, facade.CurrentStateName);
        }

        /// <summary>服务退出时的空实现。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStopReason stopReason)
        {
        }
    }

    /// <summary>
    /// 把当前目标同步到黑板的服务处理器。
    /// </summary>
    internal sealed class SyncCharacterTargetServiceHandler : IBehaviorTreeServiceHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;
        private readonly IBehaviorTreeTargetProvider targetProvider;

        /// <summary>
        /// 构造目标同步服务。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        /// <param name="targetProvider">目标提供器。</param>
        public SyncCharacterTargetServiceHandler(
            IBehaviorTreeCharacterFacade facade,
            IBehaviorTreeTargetProvider targetProvider)
        {
            this.facade = facade;
            this.targetProvider = targetProvider;
        }

        /// <summary>服务进入时的空实现。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
        }

        /// <summary>把当前目标状态写入黑板。</summary>
        public void Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            if (targetProvider == null || !targetProvider.TryGetTarget(out BehaviorTreeTargetData targetData))
            {
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.HasTarget, false);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionX, 0f);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionY, 0f);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionZ, 0f);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetDistance, float.MaxValue);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetHorizontalDistance, float.MaxValue);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetName, string.Empty);
                context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetInstanceId, 0);
                return;
            }

            Vector3 delta = targetData.Position - facade.WorldPosition;
            Vector2 horizontalDelta = new Vector2(delta.x, delta.z);

            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.HasTarget, true);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionX, targetData.Position.x);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionY, targetData.Position.y);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetPositionZ, targetData.Position.z);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetDistance, delta.magnitude);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetHorizontalDistance, horizontalDelta.magnitude);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetName, targetData.Name);
            context.SetBlackboardValue(BehaviorTreeCharacterBlackboardKeys.TargetInstanceId, targetData.InstanceId);
        }

        /// <summary>服务退出时的空实现。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStopReason stopReason)
        {
        }
    }

    /// <summary>
    /// 持续清空角色移动输入的 Idle 动作。
    /// </summary>
    internal sealed class CharacterIdleActionHandler : IBehaviorTreeActionHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;

        /// <summary>
        /// 构造 Idle 动作处理器。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        public CharacterIdleActionHandler(IBehaviorTreeCharacterFacade facade)
        {
            this.facade = facade;
        }

        /// <summary>进入 Idle 时先清空输入。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            facade.ClearMovement();
        }

        /// <summary>持续清空移动输入并保持 Running。</summary>
        public BehaviorTreeNodeStatus Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            facade.ClearMovement();
            return BehaviorTreeNodeStatus.Running;
        }

        /// <summary>退出 Idle 时再次清空输入。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStatus lastStatus, BehaviorTreeNodeStopReason stopReason)
        {
            facade.ClearMovement();
        }
    }

    /// <summary>
    /// 从黑板读取 MoveX/MoveY 并驱动角色移动的动作。
    /// </summary>
    internal sealed class CharacterMoveActionHandler : IBehaviorTreeActionHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;

        /// <summary>
        /// 构造移动动作处理器。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        public CharacterMoveActionHandler(IBehaviorTreeCharacterFacade facade)
        {
            this.facade = facade;
        }

        /// <summary>进入移动动作时的空实现。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
        }

        /// <summary>读取黑板移动输入并驱动角色移动。</summary>
        public BehaviorTreeNodeStatus Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            float moveX = context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.MoveX, 0f);
            float moveY = context.Blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.MoveY, 0f);
            Vector2 direction = Vector2.ClampMagnitude(new Vector2(moveX, moveY), 1f);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                facade.ClearMovement();
                return BehaviorTreeNodeStatus.Failure;
            }

            facade.SetMovement(direction);
            return BehaviorTreeNodeStatus.Running;
        }

        /// <summary>退出移动动作时清空移动输入。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStatus lastStatus, BehaviorTreeNodeStopReason stopReason)
        {
            facade.ClearMovement();
        }
    }

    /// <summary>
    /// 直接朝当前目标追击的动作处理器。
    /// </summary>
    internal sealed class CharacterChaseTargetActionHandler : IBehaviorTreeActionHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;
        private readonly IBehaviorTreeTargetProvider targetProvider;

        /// <summary>
        /// 构造追击动作处理器。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        /// <param name="targetProvider">目标提供器。</param>
        public CharacterChaseTargetActionHandler(
            IBehaviorTreeCharacterFacade facade,
            IBehaviorTreeTargetProvider targetProvider)
        {
            this.facade = facade;
            this.targetProvider = targetProvider;
        }

        /// <summary>进入追击动作时的空实现。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
        }

        /// <summary>每帧取目标并向目标方向写入移动输入。</summary>
        public BehaviorTreeNodeStatus Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            if (targetProvider == null || !targetProvider.TryGetTarget(out BehaviorTreeTargetData targetData))
            {
                facade.ClearMovement();
                return BehaviorTreeNodeStatus.Failure;
            }

            float stopDistance = context.Blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetStopDistance,
                0.1f);
            float dashDistance = context.Blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetDashDistance,
                0f);

            Vector3 delta = targetData.Position - facade.WorldPosition;
            Vector2 horizontalDelta = new Vector2(delta.x, delta.z);
            float currentDistSq = horizontalDelta.sqrMagnitude;

            if (currentDistSq <= stopDistance * stopDistance)
            {
                facade.ClearMovement();
                return BehaviorTreeNodeStatus.Success;
            }

            // 1. 先写入移动方向，确保如果后续触发闪避，闪避能拿到正确的输入方向
            if (facade is BehaviorTreeCharacterFacade characterFacade)
            {
                characterFacade.SetWorldMovement(new Vector3(horizontalDelta.x, 0f, horizontalDelta.y));
            }
            else
            {
                facade.SetMovement(horizontalDelta.normalized);
            }

            // 2. 如果距离大于冲刺阈值，且角色当前不在闪避或冲刺状态，触发 EvadeFront 以进入冲刺
            if (dashDistance > 0f && currentDistSq > dashDistance * dashDistance)
            {
                if (!facade.IsEvadeState && !facade.IsDashState)
                {
                    facade.TriggerEvadeFront();
                }
            }

            return BehaviorTreeNodeStatus.Running;
        }

        /// <summary>退出追击动作时清空移动输入。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStatus lastStatus, BehaviorTreeNodeStopReason stopReason)
        {
            facade.ClearMovement();
        }
    }

    /// <summary>
    /// 负责触发一次角色指令，并等待对应状态机状态完成的动作处理器。
    /// </summary>
    internal sealed class TriggerCharacterCommandActionHandler : IBehaviorTreeActionHandler
    {
        private readonly IBehaviorTreeCharacterFacade facade;
        private readonly Func<IBehaviorTreeCharacterFacade, bool> triggerCommand;
        private readonly Func<IBehaviorTreeCharacterFacade, bool> activeStatePredicate;

        /// <summary>
        /// 构造命令触发动作处理器。
        /// </summary>
        /// <param name="facade">角色门面。</param>
        /// <param name="triggerCommand">真正触发指令的方法。</param>
        /// <param name="activeStatePredicate">判断角色是否进入目标状态的方法。</param>
        public TriggerCharacterCommandActionHandler(
            IBehaviorTreeCharacterFacade facade,
            Func<IBehaviorTreeCharacterFacade, bool> triggerCommand,
            Func<IBehaviorTreeCharacterFacade, bool> activeStatePredicate)
        {
            this.facade = facade;
            this.triggerCommand = triggerCommand;
            this.activeStatePredicate = activeStatePredicate;
        }

        /// <summary>进入动作时触发一次指令并初始化节点内存。</summary>
        public void OnEnter(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            TriggerActionMemory memory = context.GetOrCreateNodeMemory<TriggerActionMemory>(node);
            memory.Triggered = triggerCommand != null && triggerCommand(facade);
            memory.ObservedActiveState = activeStatePredicate != null && activeStatePredicate(facade);
            memory.GraceTicksRemaining = 1;
        }

        /// <summary>根据角色状态决定该动作是失败、运行中还是成功。</summary>
        public BehaviorTreeNodeStatus Tick(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node)
        {
            TriggerActionMemory memory = context.GetOrCreateNodeMemory<TriggerActionMemory>(node);
            if (!memory.Triggered)
            {
                return BehaviorTreeNodeStatus.Failure;
            }

            bool isActive = activeStatePredicate != null && activeStatePredicate(facade);
            if (isActive)
            {
                memory.ObservedActiveState = true;
                return BehaviorTreeNodeStatus.Running;
            }

            if (memory.ObservedActiveState)
            {
                return BehaviorTreeNodeStatus.Success;
            }

            if (memory.GraceTicksRemaining-- > 0)
            {
                return BehaviorTreeNodeStatus.Running;
            }

            return BehaviorTreeNodeStatus.Failure;
        }

        /// <summary>退出动作时清理节点内存。</summary>
        public void OnExit(BehaviorTreeExecutionContext context, BehaviorTreeDefinitionNode node, BehaviorTreeNodeStatus lastStatus, BehaviorTreeNodeStopReason stopReason)
        {
            context.ClearNodeMemory(node);
        }

        private sealed class TriggerActionMemory
        {
            public bool Triggered;
            public bool ObservedActiveState;
            public int GraceTicksRemaining;
        }
    }
}
