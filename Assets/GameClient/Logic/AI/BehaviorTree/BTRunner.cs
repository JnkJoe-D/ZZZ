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
        private Clock _localClock;
        private TreeActionAgent _agent;

        public void Init(BehaviorTree.BehaviorTreeAsset asset)
        {
            _localClock = new Clock();

            treeAsset = asset;
            var blackboard = new Blackboard(_localClock);
            
            // 可以通过拓展将更多上下文信息注册到黑板，如 Owner (Entity) 等
            SetBB(blackboard);

            // 手动强行刷新时钟，立刻消费掉刚刚挂起的所有初值valuechan事件！
            _localClock.Update(0f);

            // 创建并传入动作代理类
            var monster = gameObject.GetComponent<MonsterEntity>();
            _agent = new TreeActionAgent(monster);
            
            RuntimeTranslationResult = BehaviorTree.BehaviorTreeTranslator.Translate(asset, blackboard, _localClock, _agent);

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
        void Update()
        {
            _localClock.Update(Time.deltaTime);
        }
        private void SetBB(Blackboard bb)
        {
            if (bb != null)
            {
                bb.Set("GameObject", this.gameObject);
                bb.Set("HitTriggerTimestamp", -1);
            }
        }
        private void OnDestroy()
        {
            StopTree();
            _agent?.Dispose();
        }
    }
}
