using NPBehave;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 轻量级行为树运行组件，负责在运行时加载和管理 NPBehave 行为树实例。
    /// </summary>
    public class BTRunner : MonoBehaviour
    {
        public BehaviorTree.BehaviorTreeAsset treeAsset;

        public Root RuntimeRoot => RuntimeTranslationResult?.Root;
        public BehaviorTree.TranslationResult RuntimeTranslationResult { get; private set; }
        public Blackboard RuntimeBlackboard => RuntimeRoot?.Blackboard;

        public void Init(BehaviorTree.BehaviorTreeAsset asset)
        {
            treeAsset = asset;
            var blackboard = new Blackboard(UnityContext.GetClock());
            
            // 可以通过拓展将更多上下文信息注册到黑板，如 Owner (Entity) 等
            blackboard.Set("GameObject", this.gameObject);

            // 创建并传入动作代理类
            var monster = gameObject.GetComponent<MonsterEntity>();
            var agent = new TreeActionAgent(monster);
            
            RuntimeTranslationResult = BehaviorTree.BehaviorTreeTranslator.Translate(asset, blackboard, agent);

#if UNITY_EDITOR
            // 自动挂载 NPBehave 原生 Debugger 组件供调试参考
            if (RuntimeRoot != null)
            {
                var debugger = gameObject.GetComponent<NPBehave.Debugger>();
                if (debugger == null) debugger = gameObject.AddComponent<NPBehave.Debugger>();
                debugger.BehaviorTree = RuntimeRoot;
            }
#endif
        }

        public void StartTree()
        {
            if (RuntimeRoot != null && RuntimeRoot.CurrentState == Node.State.INACTIVE)
            {
                RuntimeRoot.Start();
            }
        }

        public void StopTree()
        {
            if (RuntimeRoot != null && RuntimeRoot.CurrentState == Node.State.ACTIVE)
            {
                RuntimeRoot.Stop();
            }
        }

        private void OnDestroy()
        {
            StopTree();
        }
    }
}
