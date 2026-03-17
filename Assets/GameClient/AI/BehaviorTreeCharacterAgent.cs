using Game.Logic.Character;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 挂在角色上的行为树运行时代理，负责初始化、每帧驱动和目标元数据注册。
    /// </summary>
    [RequireComponent(typeof(AIEntity))]
    public sealed class BehaviorTreeCharacterAgent : MonoBehaviour
    {
        private enum DefaultTargetMode
        {
            LocalPlayerOnly,
            LocalPlayerPreferred,
            ClosestCharacter
        }

        [SerializeField] private BehaviorTreeGraphAsset behaviorTree;
        [SerializeField] private bool forceRecompileOnInitialize = true;
        [SerializeField] private bool autoCreateAIInputProvider = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private DefaultTargetMode defaultTargetMode = DefaultTargetMode.LocalPlayerPreferred;
        [SerializeField] private BehaviorTreeTargetFactionFilter targetFactionFilter = BehaviorTreeTargetFactionFilter.DifferentFaction;
        [SerializeField] private BehaviorTreeTargetControlFilter targetControlFilter = BehaviorTreeTargetControlFilter.Any;
        [SerializeField] private int factionId = 2;
        [SerializeField] private float targetMinDistance = 0f;
        [SerializeField] private float targetSearchRadius = 20f;
        [SerializeField, Range(0f, 360f)] private float targetFieldOfView = 360f;
        [SerializeField] private bool retainCurrentTarget = true;
        [SerializeField] private float targetRetainDistanceMultiplier = 1.25f;

        private AIEntity character;
        private AIInputProvider aiInputProvider;
        private BehaviorTreeInstance instance;

        /// <summary>当前绑定的角色实体。</summary>
        public AIEntity Character => character;
        /// <summary>当前使用的 AI 输入代理。</summary>
        public AIInputProvider InputProvider => aiInputProvider;
        /// <summary>当前运行中的行为树实例。</summary>
        public BehaviorTreeInstance Instance => instance;
        /// <summary>当前行为树黑板。</summary>
        public BehaviorTreeBlackboard Blackboard => instance?.Context.Blackboard;
        /// <summary>当前使用的行为树图资产。</summary>
        public BehaviorTreeGraphAsset BehaviorTree => behaviorTree;
        /// <summary>角色状态机当前状态名，主要供调试显示。</summary>
        public string CurrentCharacterStateName => character?.StateMachine?.CurrentState?.GetType().Name ?? string.Empty;

        /// <summary>
        /// 缓存角色和输入代理，并把角色注册到目标系统。
        /// </summary>
        private void Awake()
        {
            character = GetComponent<AIEntity>();
            aiInputProvider = GetComponent<AIInputProvider>();

            RegisterTargetMetadata();
        }

        /// <summary>
        /// 在启用自动初始化时，尝试启动行为树。
        /// </summary>
        private void Start()
        {
            if (autoInitializeOnStart && instance == null)
            {
                TryInitialize();
            }
        }

        /// <summary>
        /// 每帧尝试维持行为树实例并推进行为树执行。
        /// </summary>
        private void Update()
        {
            if (instance == null && autoInitializeOnStart)
            {
                TryInitialize();
            }

            instance?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 初始化行为树实例。
        /// </summary>
        /// <param name="graphOverride">可选的覆盖行为树图。</param>
        /// <param name="targetProvider">可选的覆盖目标提供器。</param>
        /// <returns>是否初始化成功。</returns>
        public bool TryInitialize(
            BehaviorTreeGraphAsset graphOverride = null,
            IBehaviorTreeTargetProvider targetProvider = null)
        {
            if (character == null || aiInputProvider == null)
            {
                return false;
            }

            BehaviorTreeGraphAsset resolvedGraph = graphOverride ?? behaviorTree ?? character.Config?.BehaviorTreeGraph;
            if (resolvedGraph == null)
            {
                return false;
            }

            DisposeInstance();

            behaviorTree = resolvedGraph;
            aiInputProvider.ResetInputState();

            BehaviorTreeCharacterFacade facade = new BehaviorTreeCharacterFacade(character, aiInputProvider);
            IBehaviorTreeTargetProvider resolvedTargetProvider = targetProvider;
            BehaviorTreeInstance createdInstance = null;
            if (resolvedTargetProvider == null)
            {
                BehaviorTreeTargetSelectionOptions selectionOptions = new BehaviorTreeTargetSelectionOptions
                {
                    SelectionMode = defaultTargetMode switch
                    {
                        DefaultTargetMode.LocalPlayerOnly => BehaviorTreeTargetSelectionMode.LocalPlayerOnly,
                        DefaultTargetMode.ClosestCharacter => BehaviorTreeTargetSelectionMode.ClosestCharacter,
                        _ => BehaviorTreeTargetSelectionMode.LocalPlayerPreferred
                    },
                    FactionFilter = targetFactionFilter,
                    ControlFilter = targetControlFilter,
                    MinDistance = targetMinDistance,
                    MaxDistance = targetSearchRadius,
                    FieldOfViewDegrees = targetFieldOfView,
                    RetainCurrentTarget = retainCurrentTarget,
                    RetainDistanceMultiplier = targetRetainDistanceMultiplier
                };
                resolvedTargetProvider = new BehaviorTreeSceneCharacterTargetProvider(
                    character,
                    selectionOptions,
                    () => createdInstance?.Context.Blackboard);
            }

            BehaviorTreeRuntimeBindings bindings = BehaviorTreeCharacterBindingsFactory.CreateDefault(
                facade,
                resolvedTargetProvider);
            createdInstance = resolvedGraph.CreateInstance(character, bindings, null, forceRecompileOnInitialize);
            instance = createdInstance;

            if (instance == null || !instance.IsValid)
            {
                DisposeInstance();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 停止当前行为树，并可选清空 AI 输入。
        /// </summary>
        /// <param name="clearInput">停止后是否清空输入。</param>
        public void StopTree(bool clearInput = true)
        {
            instance?.Stop();

            if (clearInput)
            {
                aiInputProvider?.ResetInputState();
            }
        }

        /// <summary>
        /// 组件销毁时注销目标元数据并释放行为树实例。
        /// </summary>
        private void OnDestroy()
        {
            ClearTargetMetadata();
            DisposeInstance();
        }

        /// <summary>
        /// 释放当前实例并清空输入。
        /// </summary>
        private void DisposeInstance()
        {
            if (instance == null)
            {
                return;
            }

            instance.Dispose();
            instance = null;
            aiInputProvider?.ResetInputState();
        }

        /// <summary>
        /// 把当前角色注册到目标系统，并写入阵营与是否为玩家控制的信息。
        /// </summary>
        private void RegisterTargetMetadata()
        {
            if (character == null)
            {
                return;
            }

            CharacterEntity localPlayer =
                Game.Logic.Player.PlayerManager.Instance?.LocalCharacter ?? Game.Logic.Character.CharcterManager.Instance?.LocalCharacter;
            bool isPlayerControlled = localPlayer == character;
            BehaviorTreeCharacterRegistry.SetMetadata(
                character,
                new BehaviorTreeTargetMetadata(factionId, isPlayerControlled));
        }

        /// <summary>
        /// 从目标系统里移除当前角色的元数据。
        /// </summary>
        private void ClearTargetMetadata()
        {
            if (character == null)
            {
                return;
            }

            BehaviorTreeCharacterRegistry.ClearMetadata(character);
        }
    }
}
