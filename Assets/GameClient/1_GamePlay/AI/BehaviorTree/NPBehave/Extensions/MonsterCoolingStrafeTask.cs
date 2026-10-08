using Game.Framework;
using NPBehave;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物冷却期自适应周旋对峙运行时任务。
    /// 纯策略节点：
    /// 1. 向 Context 声明 Strategy = Strafe 及预期的目标射程；
    /// 2. 具体的横移、换向、对峙走位均由底层状态机 (MonsterWalkState) 微观自决策；
    /// 3. 双保险打断：除外层 Decorator 打断外，Tick 中实时监测 AttackCooldownTimer <= 0 即刻自主结束交权；
    /// 4. 持续时间结束后返回 Success 交由行为树继续决策。
    /// </summary>
    public class MonsterCoolingStrafeTask : Task
    {
        private readonly MonsterCoolingStrafeData _data;
        private readonly TreeActionAgent _agent;
        private double _startTime;

        public MonsterCoolingStrafeTask(
            MonsterCoolingStrafeData data,
            TreeActionAgent agent) : base("MonsterCoolingStrafeTask")
        {
            _data = data;
            _agent = agent;
        }

        protected override void DoStart()
        {
            if (_data == null || _data.targetDistance <= 0f || _data.strafeDuration <= 0f)
            {
                GLog.Warning(LogTags.AI, $"[MonsterCoolingStrafeTask] 非法配置: targetDistance ({_data?.targetDistance}) <= 0 或 strafeDuration ({_data?.strafeDuration}) <= 0，终止执行");
                Stopped(false);
                return;
            }

            _startTime = RootNode.Clock.ElapsedTime;

            var ctx = _agent?.Context;
            if (ctx == null)
            {
                Stopped(false);
                return;
            }

            ctx.Strategy = MonsterStrategy.Strafe;
            ctx.TargetRadius = _data.targetDistance;
            ctx.IsInRange = false;

            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            if (_agent == null || _agent.IsInHitStun())
            {
                StopAndReturn(false);
                return;
            }

            // ── 瞬时响应：若攻击冷却已归零，立即交出控制权切入攻击准备 ──
            if (Blackboard != null && Blackboard.Isset(BBKeyMapper.GetString(BBKey.AttackCooldownTimer)))
            {
                float cd = Blackboard.Get<float>(BBKeyMapper.GetString(BBKey.AttackCooldownTimer));
                if (cd <= 0.001f)
                {
                    StopAndReturn(true);
                    return;
                }
            }

            // 持续时间耗尽，交出控制权让行为树重新判定
            if (RootNode.Clock.ElapsedTime - _startTime >= _data.strafeDuration)
            {
                StopAndReturn(true);
            }
        }

        private void StopAndReturn(bool result)
        {
            RootNode.Clock.RemoveUpdateObserver(Tick);
            if (_agent?.Context != null && _agent.Context.Strategy == MonsterStrategy.Strafe)
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
