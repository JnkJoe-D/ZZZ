using NPBehave;
using UnityEngine;
using Game.GamePlay;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物冷却期自适应周旋对峙运行时任务。
    /// 基于黑板中的 NextActionEffectiveRange：
    /// 1. 距离不足（Δ > 0）：提前调整身位（差值大用 Run，差值小用 Walk_F）；
    /// 2. 距离满足（Δ <= 0）：纯粹侧向环绕走位对峙（Walk_L / Walk_R），绝对不站桩发呆！
    /// </summary>
    public class MonsterCoolingStrafeTask : Task
    {
        private readonly MonsterCoolingStrafeData _data;
        private readonly TreeActionAgent _agent;
        private readonly float _runThresholdRadius;
        private float _elapsedTimer;
        private bool _isAdjustingDistance;

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
            _elapsedTimer = 0f;

            float distance = _agent.GetDistanceToTarget();
            if (distance < 0f)
            {
                Stopped(false);
                return;
            }

            // 从黑板读取下个招式的有效射程（若无则兜底 3.5m）
            float nextRange = 3.5f;
            if (Blackboard != null && Blackboard.Isset("NextActionEffectiveRange"))
            {
                nextRange = Blackboard.Get<float>("NextActionEffectiveRange");
            }

            float delta = distance - nextRange;

            if (delta > 0f)
            {
                // 身位未就位：在冷却期提前向射程移动
                _isAdjustingDistance = true;
                var intent = (delta > _runThresholdRadius)
                    ? MonsterLocomotionIntent.Run
                    : MonsterLocomotionIntent.StrafeForward;
                _agent.SetLocomotionIntent(intent);
            }
            else
            {
                // 身位已就绪：执行侧向环绕对峙（随机左绕或右绕）
                SwitchToStrafe();
            }

            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void SwitchToStrafe()
        {
            _isAdjustingDistance = false;
            var intent = (UnityEngine.Random.value > 0.5f)
                ? MonsterLocomotionIntent.StrafeLeft
                : MonsterLocomotionIntent.StrafeRight;
            _agent.SetLocomotionIntent(intent);
        }

        private void Tick()
        {
            _elapsedTimer += Time.deltaTime;

            if (_isAdjustingDistance)
            {
                float distance = _agent.GetDistanceToTarget();
                float nextRange = 3.5f;
                if (Blackboard != null && Blackboard.Isset("NextActionEffectiveRange"))
                {
                    nextRange = Blackboard.Get<float>("NextActionEffectiveRange");
                }

                float delta = distance - nextRange;
                if (delta <= 0f)
                {
                    // 已就位，丝滑切换为侧向对峙绕步
                    SwitchToStrafe();
                }
                else
                {
                    // 仅当跨过奔跑/走位阈值时更新决策意图
                    var desiredIntent = (delta > _runThresholdRadius)
                        ? MonsterLocomotionIntent.Run
                        : MonsterLocomotionIntent.StrafeForward;
                    _agent.SetLocomotionIntent(desiredIntent);
                }
            }

            // 持续时间耗尽，交出控制权让行为树重新判定
            if (_elapsedTimer >= _data.strafeDuration)
            {
                StopAndReturn(true);
            }
        }

        private void StopAndReturn(bool result)
        {
            RootNode.Clock.RemoveUpdateObserver(Tick);
            _agent.ClearLocomotionIntent();
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
