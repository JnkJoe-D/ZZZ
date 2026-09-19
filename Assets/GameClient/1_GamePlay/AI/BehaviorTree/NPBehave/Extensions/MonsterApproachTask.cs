using NPBehave;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 行为树逼近任务。
    /// 仅向 Context 声明逼近意图与目标射程，每帧检测 Context.IsInRange 是否达标。
    /// 彻底消除对底层 MovementComponent 的直接操纵。
    /// </summary>
    public class MonsterApproachTask : Task
    {
        private readonly TreeActionAgent _agent;
        private readonly float _defaultRange;
        private readonly float _timeout;
        private double _startTime;

        public MonsterApproachTask(TreeActionAgent agent, float defaultRange = 3.5f, float timeout = 8.0f) 
            : base("MonsterApproachTask")
        {
            _agent = agent;
            _defaultRange = defaultRange;
            _timeout = timeout;
        }

        protected override void DoStart()
        {
            _startTime = RootNode.Clock.ElapsedTime;
            var ctx = _agent.Context;
            if (ctx == null)
            {
                Stopped(false);
                return;
            }

            float range = _defaultRange;
            if (Blackboard != null && Blackboard.Isset(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange)))
            {
                range = Blackboard.Get<float>(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange));
            }

            ctx.Strategy = MonsterStrategy.Approach;
            ctx.TargetRadius = range;
            ctx.IsInRange = false;

            RootNode.Clock.AddUpdateObserver(OnTick);
        }

        private void OnTick()
        {
            // 状态机微观执行到位并写回事实
            if (_agent.Context != null && _agent.Context.IsInRange)
            {
                Finish(true);
                return;
            }

            if (_timeout > 0f && (RootNode.Clock.ElapsedTime - _startTime >= _timeout))
            {
                Finish(false);
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
