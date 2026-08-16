using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class BTRunner : MonoBehaviour
    {
        public BehaviorTreeAsset treeAsset;
        private BehaviorTreeAsset _runtimeTree;

        public BehaviorTreeAsset RuntimeTree => _runtimeTree;

        public void Setup(Blackboard blackboard)
        {
            if (treeAsset != null)
            {
                _runtimeTree = treeAsset.Clone();
                _runtimeTree.blackboard = blackboard;
                
                if (_runtimeTree.rootNode != null)
                {
                    _runtimeTree.rootNode.SetTree(_runtimeTree);
                }
            }
            else
            {
                Debug.LogWarning("BTRunner Setup failed: treeAsset is null.");
            }
        }

        public void StartTree()
        {
            if (_runtimeTree != null && _runtimeTree.rootNode != null)
            {
                _runtimeTree.rootNode.Start();
            }
        }

        public void UpdateTree()
        {
            if (_runtimeTree != null && _runtimeTree.rootNode != null)
            {
                // 如果树停了，自动重启（模拟Repeater）
                if (_runtimeTree.rootNode.CurrentState == NodeState.Inactive)
                {
                    _runtimeTree.rootNode.Start();
                }
            }
        }
    }
}
