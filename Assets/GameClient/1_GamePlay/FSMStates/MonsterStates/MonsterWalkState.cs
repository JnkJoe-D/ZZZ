using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物全向走位与周旋对峙状态。
    /// 微观自决策：
    /// 1. Approach 模式：缓步前压，若距离拉大自决策切回 RunState，到位写回 IsInRange；
    /// 2. Strafe 模式：环绕对峙横移（WalkL / WalkR 定时换向）。
    /// </summary>
    public class MonsterWalkState : MonsterStateBase
    {
        private StrafeDirection _currentDirection = StrafeDirection.Forward;
        private float _strafeDirectionTimer = 0f;

        public override void OnEnter()
        {
            UpdateWalkGait(true);
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            RotateTowardsTarget(deltaTime);

            if (Context.Strategy == MonsterStrategy.Idle)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;
            if (distance < 0f) return;

            float delta = distance - Context.TargetRadius;

            if (Context.Strategy == MonsterStrategy.Approach)
            {
                // 距离过大，重新起跑
                float runThreshold = LocoConfig != null ? LocoConfig.runThresholdRadius : 4.0f;
                if (delta > runThreshold)
                {
                    Machine.ChangeState<MonsterRunState>();
                    return;
                }

                // 逼近到位
                if (delta <= 0f)
                {
                    Context.IsInRange = true;
                }

                if (_currentDirection != StrafeDirection.Forward)
                {
                    _currentDirection = StrafeDirection.Forward;
                    UpdateWalkGait(false);
                }
            }
            else if (Context.Strategy == MonsterStrategy.Strafe)
            {
                // 冷却对峙期：环绕横移换向计时
                _strafeDirectionTimer -= deltaTime;
                if (_strafeDirectionTimer <= 0f)
                {
                    float minInterval = LocoConfig != null ? LocoConfig.minStrafeInterval : 2.0f;
                    float maxInterval = LocoConfig != null ? LocoConfig.maxStrafeInterval : 4.0f;
                    _strafeDirectionTimer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
                    _currentDirection = Random.value > 0.5f ? StrafeDirection.Left : StrafeDirection.Right;
                    UpdateWalkGait(false);
                }
            }
        }

        private void UpdateWalkGait(bool force)
        {
            if (LocoConfig == null) return;

            ActionConfigAsset targetAction = _currentDirection switch
            {
                StrafeDirection.Forward => LocoConfig.WalkF,
                StrafeDirection.Backward => LocoConfig.WalkB,
                StrafeDirection.Left => LocoConfig.WalkL,
                StrafeDirection.Right => LocoConfig.WalkR,
                _ => LocoConfig.WalkF
            };

            if (targetAction != null)
            {
                if (force || Entity.ActionController?.CurrentPlayingAction != targetAction)
                {
                    SendCommand(targetAction);
                }
            }
        }
    }
}
