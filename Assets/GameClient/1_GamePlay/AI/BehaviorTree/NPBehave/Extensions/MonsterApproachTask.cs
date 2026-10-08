using Game.Framework;
using NPBehave;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 行为树逼近突进任务。
    /// 规则契约：
    /// 1. 向 Context 声明 Strategy = Approach 及当前攻击招式的目标射程 TargetRadius；
    /// 2. 启动时若已在射程内或物理接触到位，0 帧立即成功返回，零延迟直切攻击出刀；
    /// 3. 每帧进行双重事实校验 (Context.IsInRange 与实时距离/胶囊体物理接触)，到位即完成；
    /// 4. 自带超时安全防护兜底 (防止配置极端极小值导致秒退)。
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
            if (_timeout <= 0f || _defaultRange <= 0f)
            {
                GLog.Warning(LogTags.AI, $"[MonsterApproachTask] 非法配置: timeout ({_timeout}) <= 0 或 defaultRange ({_defaultRange}) <= 0，终止执行");
                Stopped(false);
                return;
            }

            var ctx = _agent?.Context;
            if (ctx == null)
            {
                Stopped(false);
                return;
            }

            _startTime = RootNode.Clock.ElapsedTime;

            float range = _defaultRange;
            if (Blackboard != null && Blackboard.Isset(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange)))
            {
                range = Blackboard.Get<float>(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange));
            }

            // ── 0 帧极速判定：若启动时已处于有效距离区间内，直接返回 true，绝不播 RunF，直切攻击 ──
            float currentDist = _agent.GetDistanceToTarget();
            float contactRadius = _agent.GetCombinedPhysicalRadius();
            if (currentDist > 0f && (currentDist <= range || currentDist <= contactRadius + 0.15f))
            {
                ctx.IsInRange = true;
                Stopped(true);
                return;
            }

            // 攻击距离不够，才声明 Approach 策略并以 RunF 冲锋追击
            ctx.Strategy = MonsterStrategy.Approach;
            ctx.TargetRadius = range;
            ctx.IsInRange = false;

            RootNode.Clock.AddUpdateObserver(OnTick);
        }

        private void OnTick()
        {
            if (_agent == null || _agent.Context == null || _agent.IsInHitStun())
            {
                Finish(false);
                return;
            }

            // 双重到位检测：状态机回写事实 或 物理接触达标
            float currentDist = _agent.GetDistanceToTarget();
            float contactRadius = _agent.GetCombinedPhysicalRadius();

            if (_agent.Context.IsInRange || (currentDist > 0f && (currentDist <= _agent.Context.TargetRadius || currentDist <= contactRadius + 0.15f)))
            {
                _agent.Context.IsInRange = true;
                Finish(true);
                return;
            }

            // 超时保护
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
