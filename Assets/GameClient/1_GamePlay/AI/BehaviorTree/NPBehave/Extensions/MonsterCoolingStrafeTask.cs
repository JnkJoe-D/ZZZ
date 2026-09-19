using NPBehave;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物冷却期自适应周旋对峙运行时任务。
    /// 纯策略节点：
    /// 1. 向 Context 声明 Strategy = Strafe 及预期的目标射程；
    /// 2. 具体的横移、换向、对峙走位均由底层状态机 (MonsterWalkState) 微观自决策；
    /// 3. 持续时间结束后返回 Success 交由行为树继续决策。
    /// </summary>
    public class MonsterCoolingStrafeTask : Task
    {
        private readonly MonsterCoolingStrafeData _data;
        private readonly TreeActionAgent _agent;
        private readonly float _runThresholdRadius;
        private double _startTime;

        public MonsterCoolingStrafeTask(
            MonsterCoolingStrafeData data,
            TreeActionAgent agent,
            float runThresholdRadius = 4.0f) : base("MonsterCoolingStrafeTask")
        {
            _data = data;
            _agent = agent;
            _runThresholdRadius = runThresholdRadius;
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

            // 从黑板读取下个招式的有效射程（若无则兜底 3.5m）
            float nextRange = 3.5f;
            if (Blackboard != null && Blackboard.Isset(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange)))
            {
                nextRange = Blackboard.Get<float>(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange));
            }

            ctx.Strategy = MonsterStrategy.Strafe;
            ctx.TargetRadius = nextRange;
            ctx.IsInRange = false;

            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            // 持续时间耗尽，交出控制权让行为树重新判定
            float duration = _data != null ? _data.strafeDuration : 2.0f;
            if (RootNode.Clock.ElapsedTime - _startTime >= duration)
            {
                StopAndReturn(true);
            }
        }

        private void StopAndReturn(bool result)
        {
            RootNode.Clock.RemoveUpdateObserver(Tick);
            if (_agent.Context != null && _agent.Context.Strategy == MonsterStrategy.Strafe)
            {
                _agent.Context.Strategy = MonsterStrategy.Idle;
            }
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
