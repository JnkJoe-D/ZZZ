using NPBehave;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 行为树攻击指令下发任务。
    /// 向 Context 写入待播放的动作资产，等待状态机消费并在动作播放完毕后返回 Success。
    /// </summary>
    public class MonsterAttackTask : Task
    {
        private readonly TreeActionAgent _agent;
        private readonly ActionConfigAsset _attackAction;
        private bool _hasBeenConsumed;

        public MonsterAttackTask(TreeActionAgent agent, ActionConfigAsset attackAction) 
            : base("MonsterAttackTask")
        {
            _agent = agent;
            _attackAction = attackAction;
        }

        protected override void DoStart()
        {
            if (_attackAction == null || _agent.Context == null)
            {
                Stopped(false);
                return;
            }

            _hasBeenConsumed = false;
            _agent.Context.Strategy = MonsterStrategy.Attack;
            _agent.Context.PendingAttack = _attackAction;

            RootNode.Clock.AddUpdateObserver(OnTick);
        }

        private void OnTick()
        {
            var ctx = _agent.Context;
            if (ctx == null)
            {
                Finish(false);
                return;
            }

            // 1. 等待状态机消费 PendingAttack
            if (!_hasBeenConsumed && ctx.PendingAttack == null)
            {
                _hasBeenConsumed = true;
            }

            // 2. 状态机已消费，且动作播放完毕
            if (_hasBeenConsumed)
            {
                if (_agent.CurrentPlayingAction != _attackAction)
                {
                    Finish(true);
                }
            }
        }

        private void Finish(bool success)
        {
            RootNode.Clock.RemoveUpdateObserver(OnTick);
            Stopped(success);
        }

        protected override void DoStop() => Finish(false);
    }
}
