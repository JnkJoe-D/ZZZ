using System.Collections.Generic;
using UnityEngine;
using Game.Logic.AI.BehaviorTree;

namespace Game.Logic.AI.BehaviorTree
{
    public class BTTestController : MonoBehaviour
    {
        private Root _btRoot;
        private Blackboard _blackboard;
        private Clock _clock;

        // 测试状态标志
        public bool IsDead = false;
        public bool HasTarget = false;

        void Start()
        {
            _clock = new Clock();
            _blackboard = new Blackboard(_clock);

            // 初始状态
            _blackboard.Set("IsDead", false);
            _blackboard.Set("HasTarget", false);

            BehaviorTreeAsset dummyTree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            dummyTree.blackboard = _blackboard;

            _btRoot = new Root(
                new Selector(
                    // 1. 最高优先级：死亡状态
                    // 当 IsDead 为 true 时，打断其它分支，执行死亡行为
                    new BlackboardCondition("IsDead", Operator.IsEqual, true, Stops.ImmediateRestart,
                        new ActionNode(() => 
                        {
                            Debug.Log("<color=red>[BT Test] 角色已死亡！(Action)</color>");
                        })
                    ),

                    // 2. 次高优先级：战斗状态
                    // 当 HasTarget 为 true 时，打断更低优先级的巡逻分支
                    new BlackboardCondition("HasTarget", Operator.IsEqual, true, Stops.LowerPriority,
                        new Service(0.5f, () => 
                        {
                            Debug.Log("<color=yellow>[BT Test] Service 触发：正在更新目标位置...</color>");
                        },
                        new Sequence(
                            new ActionNode((request) => 
                            {
                                Debug.Log("<color=orange>[BT Test] 开始向目标移动 (Action) - 模拟需要 1 秒</color>");
                                _clock.AddTimer(1.0f, 0, () => request.FinishExecute(true));
                                return true;
                            }),
                            new ActionNode(() => 
                            {
                                Debug.Log("<color=orange>[BT Test] 发动攻击！(Action)</color>");
                            }),
                            new Wait(1.0f) // 攻击后硬直1秒，使得序列不会瞬间结束
                        ))
                    ),

                    // 3. 最低优先级兜底：巡逻状态
                    // 不断循环巡逻并等待
                    new Sequence(
                        new ActionNode(() => 
                        {
                            Debug.Log("<color=green>[BT Test] 开始巡逻，寻找目标... (Action)</color>");
                        }),
                        new Wait(2.0f),
                        new ActionNode(() => 
                        {
                            Debug.Log("<color=green>[BT Test] 到达巡逻点，发呆... (Action)</color>");
                        }),
                        new Wait(2.0f)
                    )
                )
            );

            Debug.Log("======================================");
            Debug.Log("行为树测试已启动！");
            Debug.Log("通过修改 Inspector 中的 HasTarget 和 IsDead 来测试打断机制。");
            Debug.Log("======================================");

            _btRoot.SetTree(dummyTree);
            _btRoot.Start();
        }

        void Update()
        {
            _clock.Update(Time.deltaTime);

            // 同步 Inspector 面板的变量到黑板中
            if ((bool)_blackboard.Get("IsDead") != IsDead)
            {
                Debug.Log($"<color=cyan>[Inspector] 设置 IsDead = {IsDead}</color>");
                _blackboard.Set("IsDead", IsDead);
            }

            if ((bool)_blackboard.Get("HasTarget") != HasTarget)
            {
                Debug.Log($"<color=cyan>[Inspector] 设置 HasTarget = {HasTarget}</color>");
                _blackboard.Set("HasTarget", HasTarget);
            }

            // 因为示例中的巡逻是一个序列，执行完一遍就会返回成功然后整棵树就 inactive 了。
            // 真实使用中，最外层通常会套一个 Repeater 装饰器。
            // 为了方便在 Update 里重置，这里我们如果树停了，再重新 Start。
            if (_btRoot.MainNode.CurrentState == NodeState.Inactive)
            {
                Debug.Log("<color=grey>[BT Test] 树已执行完毕，即将重新启动(模拟 Repeater)...</color>");
                _btRoot.Start();
            }
        }
    }
}
