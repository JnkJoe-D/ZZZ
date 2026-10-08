using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物全向走位与周旋对峙状态 (Walk)。
    /// 规则契约：
    /// 1. 唯一宿主：仅在攻击冷却中的周旋对峙期 (Strategy == Strafe) 运行；
    ///    若接收到 Approach 策略，立即切出至 MonsterRunState (全速奔跑突进)；
    /// 2. 迟滞区间决策：基于 [targetDis - x, targetDis + x] 动态调度：
    ///    - 距离 > maxDis + Δcatch: 升档切入 MonsterRunState (RunF 追赶)；
    ///    - 距离 > maxDis: 播 WalkF 慢速前压；
    ///    - 距离 < minDis: 播 WalkB 战术后撤 (minDis 严格保证大于物理碰撞半径 + 0.3m 缓冲)；
    ///    - 处于 [minDis, maxDis] 区间内: 播 WalkL / WalkR 环绕横移并定时随机反向。
    /// </summary>
    public class MonsterWalkState : MonsterStateBase
    {
        private StrafeDirection _currentDirection = StrafeDirection.Forward;
        private float _strafeDirectionTimer = 0f;
        private float _gaitCooldownTimer = 0f; // 微小步态切换冷却，防止边界高频震荡

        public override void OnEnter()
        {
            _strafeDirectionTimer = 0f;
            _gaitCooldownTimer = 0f;
            EvaluateAndApplyGait(true);
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterStun()) return;
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            if (Context == null)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            RotateTowardsTarget(deltaTime);

            // ── 策略 1：攻击逼近策略，Walk 状态禁止处理，交由 RunState 全速冲刺 ──
            if (Context.Strategy == MonsterStrategy.Approach)
            {
                Machine.ChangeState<MonsterRunState>();
                return;
            }

            // ── 策略 2：闲置策略 ──
            if (Context.Strategy == MonsterStrategy.Idle)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            // ── 策略 3：周旋对峙期 (Strafe) ──
            float distance = Entity.TargetFinder?.GetDistanceToTarget() ?? -1f;
            if (distance < 0f) return;

            float x = LocoConfig != null ? LocoConfig.strafeDistanceTolerance : 0.8f;
            float catchUpDelta = LocoConfig != null ? LocoConfig.strafeCatchUpThreshold : 2.5f;
            float targetDis = Context.TargetRadius > 0f ? Context.TargetRadius : 3.5f;
            float rSum = GetCombinedPhysicalRadius();

            // 绝对物理几何安全下限：留出至少 30cm 空间，避免穿插推挤卡顿
            float minDis = Mathf.Max(targetDis - x, rSum + 0.3f);
            float maxDis = targetDis + x;
            float catchDis = maxDis + catchUpDelta;

            // 1. 玩家跑远脱离对峙区：升档切换至 RunState 快速跟进
            if (distance > catchDis)
            {
                Machine.ChangeState<MonsterRunState>();
                return;
            }

            if (_gaitCooldownTimer > 0f)
            {
                _gaitCooldownTimer -= deltaTime;
            }

            // 2. 根据迟滞区间选择微观步态
            StrafeDirection nextDirection = _currentDirection;

            if (distance > maxDis)
            {
                // 偏远：前压
                nextDirection = StrafeDirection.Forward;
            }
            else if (distance < minDis)
            {
                // 偏近：后退
                nextDirection = StrafeDirection.Backward;
            }
            else
            {
                // 处于最佳对峙区间：环绕横移 (WalkL / WalkR)
                _strafeDirectionTimer -= deltaTime;
                if (_currentDirection != StrafeDirection.Left && _currentDirection != StrafeDirection.Right)
                {
                    nextDirection = Random.value > 0.5f ? StrafeDirection.Left : StrafeDirection.Right;
                    ResetStrafeInterval();
                }
                else if (_strafeDirectionTimer <= 0f)
                {
                    nextDirection = _currentDirection == StrafeDirection.Left ? StrafeDirection.Right : StrafeDirection.Left;
                    ResetStrafeInterval();
                }
            }

            // 3. 步态状态转移判定（带防抖动保护）
            if (nextDirection != _currentDirection && _gaitCooldownTimer <= 0f)
            {
                _currentDirection = nextDirection;
                _gaitCooldownTimer = 0.3f; // 0.3 秒步态防抖冷却
                UpdateWalkAction(false);
            }
            else
            {
                // 确保当前动作没有被意外中断
                EnsureWalkActionPlaying();
            }
        }

        private void ResetStrafeInterval()
        {
            float minInterval = LocoConfig != null ? LocoConfig.minStrafeInterval : 1.5f;
            float maxInterval = LocoConfig != null ? LocoConfig.maxStrafeInterval : 3.5f;
            _strafeDirectionTimer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
        }

        private void EvaluateAndApplyGait(bool force)
        {
            float distance = Entity?.TargetFinder?.GetDistanceToTarget() ?? -1f;
            if (distance < 0f)
            {
                UpdateWalkAction(force);
                return;
            }

            float x = LocoConfig != null ? LocoConfig.strafeDistanceTolerance : 0.8f;
            float targetDis = Context != null && Context.TargetRadius > 0f ? Context.TargetRadius : 3.5f;
            float rSum = GetCombinedPhysicalRadius();
            float minDis = Mathf.Max(targetDis - x, rSum + 0.3f);
            float maxDis = targetDis + x;

            if (distance > maxDis)
            {
                _currentDirection = StrafeDirection.Forward;
            }
            else if (distance < minDis)
            {
                _currentDirection = StrafeDirection.Backward;
            }
            else
            {
                _currentDirection = Random.value > 0.5f ? StrafeDirection.Left : StrafeDirection.Right;
                ResetStrafeInterval();
            }

            UpdateWalkAction(force);
        }

        private void UpdateWalkAction(bool force)
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

        private void EnsureWalkActionPlaying()
        {
            if (LocoConfig == null || Entity.ActionController == null) return;

            ActionConfigAsset targetAction = _currentDirection switch
            {
                StrafeDirection.Forward => LocoConfig.WalkF,
                StrafeDirection.Backward => LocoConfig.WalkB,
                StrafeDirection.Left => LocoConfig.WalkL,
                StrafeDirection.Right => LocoConfig.WalkR,
                _ => LocoConfig.WalkF
            };

            if (targetAction != null && Entity.ActionController.CurrentPlayingAction != targetAction)
            {
                SendCommand(targetAction);
            }
        }
    }
}
