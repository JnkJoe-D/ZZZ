using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree.Tests
{
    public class MonsterAssetBTTestController : MonoBehaviour
    {
        [Header("Monster Config")]
        public MonsterConfigAsset monsterConfig;
        
        [Header("Behavior Tree")]
        public BehaviorTreeAsset behaviorTreeAsset;

        [Header("Runtime Info (Read Only)")]
        private Blackboard _blackboard;
        private MonsterEntity _monsterEntity;
        private AIContext _context;
        private Clock _clock;
        private BTRunner _runner;

        void Start()
        {
            if (monsterConfig == null || behaviorTreeAsset == null)
            {
                Debug.LogError("Monster Config or Behavior Tree Asset is null!");
                return;
            }

            if (monsterConfig.Prefab == null)
            {
                Debug.LogError("Monster Config Prefab is null!");
                return;
            }

            // 1. 生成怪物
            GameObject monsterObj = Instantiate(monsterConfig.Prefab, Vector3.zero, Quaternion.identity);
            monsterObj.name = "AssetTest_Monster_" + monsterConfig.Name;
            
            // 2. 挂载实体并初始化
            _monsterEntity = monsterObj.GetComponent<MonsterEntity>();
            if (_monsterEntity == null)
            {
                _monsterEntity = monsterObj.AddComponent<MonsterEntity>();
            }
            _monsterEntity.Init(monsterConfig);
            
            // 3. 挂载 Runner
            _runner = monsterObj.AddComponent<BTRunner>();
            _runner.treeAsset = behaviorTreeAsset;

            // 4. 构建上下文与黑板
            _clock = new Clock();
            _context = new AIContext(_monsterEntity, _monsterEntity.TargetFinder);
            _blackboard = new Blackboard(_clock);
            // 将 Context 注入黑板，方便节点（如 PlayActionNode 等）获取
            _blackboard.Set("Context", _context);

            // 5. 初始化并启动行为树
            _runner.Setup(_blackboard);
            _runner.StartTree();
        }

        private float lastAttackTime = -9999f;
        private AIState _previousState = AIState.Idle;

        private void UpdateSensor()
        {
            Transform target = _monsterEntity.TargetFinder.GetTarget();
            float distance = float.MaxValue;
            
            _blackboard.Set(BlackboardKey.HasTarget.ToString(), target != null);
            if (target != null)
            {
                distance = Vector3.Distance(_monsterEntity.transform.position, target.position);
                _blackboard.Set(BlackboardKey.Target.ToString(), target);
            }
            _blackboard.Set(BlackboardKey.Distance.ToString(), distance);

            // 基于距离的抽象状态
            _blackboard.Set(BlackboardKey.IsDistanceWithinAttackRange.ToString(), distance <= monsterConfig.SensorConfig.AttackRange);
            _blackboard.Set(BlackboardKey.IsDistanceGreaterThanAttackRange.ToString(), distance > monsterConfig.SensorConfig.AttackRange);
            _blackboard.Set(BlackboardKey.IsDistanceWithinPursuitRadius.ToString(), distance <= monsterConfig.SensorConfig.PursuitRadius);
            _blackboard.Set(BlackboardKey.IsDistanceGreaterThanPursuitRadius.ToString(), distance > monsterConfig.SensorConfig.PursuitRadius);

            // 基于时间的抽象状态
            // 测试脚本通过检查状态跳出Attack时更新lastAttackTime，避免在Attack期间每帧更新导致条件瞬间失效
            AIState currentState = (AIState)(_blackboard.Get("AIState") ?? AIState.Idle);
            if (_previousState == AIState.Attack && currentState != AIState.Attack)
            {
                lastAttackTime = Time.time;
            }
            _previousState = currentState;
            
            _blackboard.Set(BlackboardKey.IsAttackCDReady.ToString(), Time.time - lastAttackTime >= monsterConfig.SensorConfig.AttackCooldown);
        }

        void Update()
        {
            if (_clock != null)
            {
                _clock.Update(Time.deltaTime);
            }

            if (_monsterEntity != null && _blackboard != null)
            {
                UpdateSensor();
            }

            if (_runner != null)
            {
                if (_runner.RuntimeTree != null && _runner.RuntimeTree.rootNode != null && _runner.RuntimeTree.rootNode.CurrentState == NodeState.Inactive)
                {
                    // 当行为树执行完一个完整序列（比如Attack打完）并停止时，手动把状态切回Idle
                    // 这样下一帧 UpdateSensor 就会捕获到脱离 Attack 状态，从而正确记录 CD！
                    _blackboard.Set("AIState", AIState.Idle);
                }
                _runner.UpdateTree();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300), "Asset BT Debug", GUI.skin.window);
            bool hasTarget = _blackboard != null && _blackboard.Get<bool>("HasTarget");
            float distance = _blackboard != null ? _blackboard.Get<float>("Distance") : 0f;
            GUILayout.Label($"HasTarget: {hasTarget}");
            GUILayout.Label($"Distance: {distance}");
            
            if (hasTarget && _blackboard.Get<Transform>("Target") != null)
            {
                GUILayout.Label($"Target: {_blackboard.Get<Transform>("Target").name}");
            }
            
            GUILayout.Label($"BT Active: {_runner != null && _runner.RuntimeTree != null && _runner.RuntimeTree.rootNode != null && _runner.RuntimeTree.rootNode.CurrentState == NodeState.Active}");
            GUILayout.EndArea();
        }
    }
}
