using Game.Logic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class RunActionNode : MonsterTaskNode
    {
        [SerializeField] private MonsterActionConfigAsset _startAction;
        [SerializeField] private MonsterActionConfigAsset _loopAction;
        [SerializeField] private MonsterActionConfigAsset _endAction;

        private enum Phase { None, Start, Loop, End }
        private Phase _currentPhase = Phase.None;

        public RunActionNode() {}
        public RunActionNode(AIContext context, MonsterActionConfigAsset start, MonsterActionConfigAsset loop, MonsterActionConfigAsset end) : base(context)
        {
            _startAction = start;
            _loopAction = loop;
            _endAction = end;
        }

        protected override void DoStart()
        {
            if (Context.Owner == null || Context.Owner.ActionPlayer == null)
            {
                Stopped(false);
                return;
            }

            _currentPhase = Phase.Start;
            bool success = PlayPhaseAction(_startAction);
            if (!success)
            {
                Stopped(false);
            }
        }

        private bool PlayPhaseAction(MonsterActionConfigAsset actionAsset)
        {
            if (actionAsset == null) return false;
            
            bool success = Context.Owner.ActionPlayer.PlayAction(actionAsset);
            if (success)
            {
                Context.Owner.ActionPlayer.OnActionComplete += OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt += OnActionInterrupt;
            }
            return success;
        }

        private void Unsubscribe()
        {
            if (Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.OnActionComplete -= OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt -= OnActionInterrupt;
            }
        }

        private void OnActionComplete()
        {
            Unsubscribe();

            if (_currentPhase == Phase.Start)
            {
                // Start 播完了，自动进入 Loop
                _currentPhase = Phase.Loop;
                bool success = PlayPhaseAction(_loopAction);
                if (!success) Stopped(false);
            }
            else if (_currentPhase == Phase.End)
            {
                // End 播完了，正式退出节点
                _currentPhase = Phase.None;
                
                // 如果当前是因为打断(高优先级条件满足)而 StopRequested，我们就上报 false，让行为树去执行其他分支
                // 如果是正常播完(理论上Loop不会自己播完，但如果有意外)，就上报 true
                Stopped(CurrentState == NodeState.StopRequested ? false : true);
            }
            else if (_currentPhase == Phase.Loop)
            {
                // Loop理应是无限循环的，但如果万一播完了，我们就当做全部完成退出
                _currentPhase = Phase.None;
                Stopped(true);
            }
        }

        private void OnActionInterrupt()
        {
            Unsubscribe();
            
            // 无论处于什么阶段，一旦发生非预期的硬直/死亡打断，直接失败退出
            _currentPhase = Phase.None;
            Stopped(false);
        }

        protected override void DoStop()
        {
            // 当行为树高层（如 DynamicInterrupt）发现不再满足追击条件（例如目标进入攻击范围），会调用此方法
            if (_currentPhase == Phase.Start || _currentPhase == Phase.Loop)
            {
                // 停止当前的播放，开始播放刹车(End)动画
                Unsubscribe();
                Context.Owner.ActionPlayer.StopAction();

                _currentPhase = Phase.End;
                bool success = PlayPhaseAction(_endAction);
                if (!success)
                {
                    _currentPhase = Phase.None;
                    Stopped(false);
                }
                else
                {
                    UnityEngine.Debug.Log($"[RunActionNode] Interrupted! Started playing End Action '{_endAction.name}'.");
                }
                // 注意：这里我们不立刻调用 Stopped(false)，而是挂起在 StopRequested 状态。
                // 这样能让行为树耐心地等刹车动画播完，然后再去进入 Attack 分支。
            }
            else if (_currentPhase == Phase.End)
            {
                UnityEngine.Debug.Log("[RunActionNode] Received Stop while in End phase. Forcing exit.");
                // 如果已经在播 End 了，又被高层疯狂 Stop，则直接结束
                Unsubscribe();
                Context.Owner.ActionPlayer.StopAction();
                _currentPhase = Phase.None;
                Stopped(false);
            }
            else
            {
                Stopped(false);
            }
        }
    }


        [System.Serializable]
        public struct RunActionConfig
        {
            public MonsterActionConfigAsset start;
            public MonsterActionConfigAsset loop;
            public MonsterActionConfigAsset end;
        }
}
