using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    public class MonsterBTTestController : MonoBehaviour
    {
        [Header("Monster Config")]
        public MonsterConfigAsset monsterConfig;
        
        [Header("Test Actions (Drag actions from Project window)")]
        public MonsterActionConfigAsset idleAction;
        
        [Header("Movement Actions (For distance adjust)")]
        public MonsterActionConfigAsset walkForwardAction;
        public MonsterActionConfigAsset walkBackwardAction;

        [Header("Wander Actions")]
        public List<MonsterActionConfigAsset> wanderActions = new List<MonsterActionConfigAsset>();
        [Header("Run Actions (Pursuit)")]
        public RunActionConfig runConfig;
        [Header("Attack Actions")]
        public List<AttackActionConfig> attackActions = new List<AttackActionConfig>();

        public enum MonsterTestState
        {
            Idle,
            Wander,
            Pursuit,
            Attack
        }

        [Header("Runtime Info (Read Only)")]
        public MonsterTestState currentState = MonsterTestState.Idle;
        private float lastAttackTime = -9999f;

        private Root _treeRoot;
        private Blackboard _blackboard;
        private MonsterEntity _monsterEntity;
        private AIContext _context;
        private Service _serviceNode;
        private Clock _clock;

        void Start()
        {
            if (monsterConfig == null)
            {
                Debug.LogError("Monster Config is null!");
                return;
            }

            if (monsterConfig.Prefab == null)
            {
                Debug.LogError("Monster Config Prefab is null!");
                return;
            }

            // 1. 生成怪物
            GameObject monsterObj = Instantiate(monsterConfig.Prefab, Vector3.zero, Quaternion.identity);
            monsterObj.name = "Test_Monster_" + monsterConfig.Name;
            
            // 2. 挂载实体并初始化
            _monsterEntity = monsterObj.GetComponent<MonsterEntity>();
            if (_monsterEntity == null)
            {
                _monsterEntity = monsterObj.AddComponent<MonsterEntity>();
            }
            // 使用基类的 Init 进行所有核心组件的初始化 (包括 ActionPlayer 等)
            _monsterEntity.Init(monsterConfig);
            
            // 构建上下文与黑板
            _clock = new Clock();
            _context = new AIContext(_monsterEntity, _monsterEntity.TargetFinder);
            _blackboard = new Blackboard(_clock);

            // 3. 构建测试行为树
            BuildBehaviorTree();

            // 4. 启动行为树
            _treeRoot.Start();
        }

        private void BuildBehaviorTree()
        {
            float tolerance = 0.5f;

            // Attack Branch - Execute
            var executeAttackBranch = new Sequence(
                new DebugLogNode("--> Playing Attack Action"),
                new PlayBlackboardActionNode(_context, "SelectedAttackAction"),
                new CustomAction(() => { currentState = MonsterTestState.Idle; lastAttackTime = Time.time; return true; })
            );

            // Attack Sequence Root
            var attackSequenceCore = new Sequence(
                new TraceCondition("Attack: HasTarget", () => _blackboard.Get<bool>("HasTarget")),
                new TraceCondition("Attack: Distance <= Range", () => _blackboard.Get<float>("Distance") <= monsterConfig.SensorConfig.AttackRange),
                new TraceCondition("Attack: State != Attack", () => currentState != MonsterTestState.Attack),
                new TraceCondition("Attack: CD Ready", () => Time.time - lastAttackTime >= monsterConfig.SensorConfig.AttackCooldown),
                new CustomAction(() => { currentState = MonsterTestState.Attack; return true; }),
                new DebugLogNode("--> Execute Attack Branch"),
                new SelectAttackNode(attackActions),
                new Sequence(
                    new AdjustDistanceNode(_context, walkForwardAction, walkBackwardAction, tolerance, monsterConfig.SensorConfig.AttackRange * 0.8f),
                    executeAttackBranch
                )
            );
            
            var attackSequence = new DynamicInterrupt(
                () => !_blackboard.Get<bool>("HasTarget") || _blackboard.Get<float>("Distance") > monsterConfig.SensorConfig.AttackRange,
                attackSequenceCore
            );

            // Wander Sequence
            Node wanderActionNode = wanderActions.Count > 0 ? 
                (Node)new RandomSelector(wanderActions.Select(a => new TryPlayActionNode(_context, a)).ToArray()) : 
                new DebugLogNode("No Wander Actions Configured");

            var wanderSequence = new DynamicInterrupt(
                () => !_blackboard.Get<bool>("HasTarget") || _blackboard.Get<float>("Distance") > monsterConfig.SensorConfig.AttackRange || Time.time - lastAttackTime >= monsterConfig.SensorConfig.AttackCooldown,
                new Sequence(
                    new TraceCondition("Wander: HasTarget", () => _blackboard.Get<bool>("HasTarget")),
                    new TraceCondition("Wander: Distance <= Range", () => _blackboard.Get<float>("Distance") <= monsterConfig.SensorConfig.AttackRange),
                    new TraceCondition("Wander: CD Not Ready", () => Time.time - lastAttackTime < monsterConfig.SensorConfig.AttackCooldown),
                    new CustomAction(() => { currentState = MonsterTestState.Wander; return true; }),
                    new DebugLogNode("--> Execute Wander Sequence"),
                    wanderActionNode
                )
            );

            // Pursuit Sequence
            var pursuitSequence = new DynamicInterrupt(
                () => !_blackboard.Get<bool>("HasTarget") || _blackboard.Get<float>("Distance") <= monsterConfig.SensorConfig.AttackRange || _blackboard.Get<float>("Distance") > monsterConfig.SensorConfig.PursuitRadius,
                new Sequence(
                    new TraceCondition("Pursuit: HasTarget", () => _blackboard.Get<bool>("HasTarget")),
                    new TraceCondition("Pursuit: Distance > AttackRange", () => _blackboard.Get<float>("Distance") > monsterConfig.SensorConfig.AttackRange),
                    new TraceCondition("Pursuit: Distance <= PursuitRadius", () => _blackboard.Get<float>("Distance") <= monsterConfig.SensorConfig.PursuitRadius),
                    new CustomAction(() => { currentState = MonsterTestState.Pursuit; return true; }),
                    new DebugLogNode("--> Execute Pursuit Sequence"),
                    new RunActionNode(_context, runConfig.start, runConfig.loop, runConfig.end)
                )
            );

            // Idle Sequence
            var idleSequence = new DynamicInterrupt(
                () => _blackboard.Get<bool>("HasTarget") && _blackboard.Get<float>("Distance") <= monsterConfig.SensorConfig.PursuitRadius,
                new Sequence(
                    new TraceCondition("Idle: ShouldIdle", () => !_blackboard.Get<bool>("HasTarget") || _blackboard.Get<float>("Distance") > monsterConfig.SensorConfig.PursuitRadius),
                    new CustomAction(() => { currentState = MonsterTestState.Idle; return true; }),
                    new DebugLogNode("--> Execute Idle Sequence"),
                    new TryPlayActionNode(_context, idleAction)
                )
            );

            // 根选择节点：优先级 攻击 > 徘徊 > 追击 > 待机
            var rootSelector = new Selector(attackSequence, wanderSequence, pursuitSequence, idleSequence);

            // 服务节点：每 0.1 秒刷新一次传感器数据
            _serviceNode = new Service(0.1f, UpdateSensor, rootSelector);

            _treeRoot = new Root( _serviceNode);
            BehaviorTreeAsset dummyTree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            dummyTree.blackboard = _blackboard;
            _treeRoot.SetTree(dummyTree);
        }

        private void UpdateSensor()
        {
            Transform target = _monsterEntity.TargetFinder.GetTarget();
            _blackboard.Set("HasTarget", target != null);
            if (target != null)
            {
                _blackboard.Set("Target", target);
                _blackboard.Set("Distance", Vector3.Distance(_monsterEntity.transform.position, target.position));
            }
            else
            {
                _blackboard.Set("Distance", float.MaxValue);
            }
        }

        void Update()
        {
            // 驱动底层时钟
            if (_clock != null)
            {
                _clock.Update(Time.deltaTime);
            }

            // 循环检测
            if (_treeRoot != null && _serviceNode != null && _serviceNode.CurrentState == NodeState.Inactive)
            {
                _treeRoot.Start();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300), "Monster BT Debug", GUI.skin.window);
            GUILayout.Label($"State: {currentState}");
            bool hasTarget = _blackboard != null && _blackboard.Get<bool>("HasTarget");
            float distance = _blackboard != null ? _blackboard.Get<float>("Distance") : 0f;
            GUILayout.Label($"HasTarget: {hasTarget}");
            GUILayout.Label($"Distance: {distance}");
            
            if (hasTarget && _blackboard.Get<Transform>("Target") != null)
            {
                GUILayout.Label($"Target: {_blackboard.Get<Transform>("Target").name}");
            }
            
            float cdRemain = monsterConfig != null ? lastAttackTime + monsterConfig.SensorConfig.AttackCooldown - Time.time : 0;
            GUILayout.Label($"Attack CD: {(cdRemain > 0 ? cdRemain.ToString("F1") : "Ready")}");
            
            GUILayout.Label($"BT Active: {_serviceNode?.CurrentState == NodeState.Active}");
            GUILayout.EndArea();
        }

        // 调试打印节点

        // 带日志的条件节点，每 60 帧打印一次条件计算结果，方便排查

        // 自定义动作节点，执行 Func 并返回成功/失败

        // 动态打断装饰器：每0.1秒轮询一次条件，如果满足打断条件（说明高优先级分支已就绪），则主动打断子节点

        // 随机选择器，随机执行一个子节点，如果子节点完成则向上抛出结果

        // 动态选择攻击并写入黑板
        // 独立处理调整距离的节点，避免装饰器和时钟导致的异常


        // 从黑板读取动作资产并播放
    }
}
