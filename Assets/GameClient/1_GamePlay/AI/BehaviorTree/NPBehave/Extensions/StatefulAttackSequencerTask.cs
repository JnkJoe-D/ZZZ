using System;
using NPBehave;
using UnityEngine;
using Game.GamePlay;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物智能记忆连招序列器运行时任务。
    /// 具备：
    /// 1. 连招断点记忆能力（被打断后恢复出招继续接续）；
    /// 2. 行为树内自主维护并向黑板透传 NextActionEffectiveRange；
    /// 3. 基于距离差值 Δ 的自适应逼近（狂奔 / 前压），进入射程瞬间秒切出刀。
    /// </summary>
    public class StatefulAttackSequencerTask : Task
    {
        private enum SequencerPhase
        {
            None,
            Approaching, // 逼近调整阶段 (Run / Walk_F)
            Pending,     // 攻击指令下发排队
            Attacking    // 攻击动作播放中
        }

        private readonly MonsterAttackSequenceData _data;
        private readonly TreeActionAgent _agent;
        private readonly Action<float> _startCooldown;
        private readonly Func<bool> _isInterruptedByHit;
        private readonly float _runThresholdRadius;

        private int _currentIndex = 0;
        private SequencerPhase _phase = SequencerPhase.None;
        private long _currentCommandId;

        public StatefulAttackSequencerTask(
            MonsterAttackSequenceData data,
            TreeActionAgent agent,
            Action<float> startCooldown,
            Func<bool> isInterruptedByHit,
            float runThresholdRadius = 4.0f) : base("StatefulAttackSequencerTask")
        {
            _data = data;
            _agent = agent;
            _startCooldown = startCooldown;
            _isInterruptedByHit = isInterruptedByHit;
            _runThresholdRadius = runThresholdRadius;
        }

        protected override void DoStart()
        {
            if (_data == null || _data.sequence == null || _data.sequence.Count == 0)
            {
                Stopped(false);
                return;
            }

            // 越界安全保护
            if (_currentIndex < 0 || _currentIndex >= _data.sequence.Count)
            {
                _currentIndex = 0;
            }

            // 启动时自主将当前动作的有效射程同步到黑板
            SyncNextRangeToBlackboard();

            _phase = SequencerPhase.None;
            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            if (_data.sequence.Count == 0)
            {
                StopAndReturn(false);
                return;
            }

            var currentEntry = _data.sequence[_currentIndex];
            if (currentEntry == null || currentEntry.action == null)
            {
                // 跳过无效配置，推进下一个
                AdvanceIndex();
                return;
            }

            float distance = _agent.GetDistanceToTarget();
            if (distance < 0f)
            {
                // 无有效目标
                StopAndReturn(false);
                return;
            }

            float delta = distance - currentEntry.effectiveRange;

            switch (_phase)
            {
                case SequencerPhase.None:
                case SequencerPhase.Approaching:
                    if (delta <= 0f)
                    {
                        // 已在有效射程内，立即秒切出刀！
                        _agent.ClearLocomotionIntent();
                        if (_agent.SendCommand(currentEntry.action, out _currentCommandId))
                        {
                            _phase = SequencerPhase.Pending;
                        }
                        else
                        {
                            StopAndReturn(false);
                        }
                    }
                    else
                    {
                        // 距离不足，执行自适应逼近
                        _phase = SequencerPhase.Approaching;

                        // 决策判断：差值大则意图为 Run，差值小则意图为 StrafeForward
                        // 内部仅在决策变更时才会下发动作指令，保证奔跑/走位循环动画不被打断
                        var desiredIntent = (delta > _runThresholdRadius)
                            ? MonsterLocomotionIntent.Run
                            : MonsterLocomotionIntent.StrafeForward;

                        _agent.SetLocomotionIntent(desiredIntent);
                    }
                    break;

                case SequencerPhase.Pending:
                    CommandFate fate = _agent.CheckCommandFate(_currentCommandId);
                    if (fate == CommandFate.Executed)
                    {
                        _phase = SequencerPhase.Attacking;
                    }
                    else if (fate == CommandFate.Dropped)
                    {
                        StopAndReturn(false);
                    }
                    break;

                case SequencerPhase.Attacking:
                    // 检查是否被玩家招架反击或受击硬直打断
                    if (_isInterruptedByHit != null && _isInterruptedByHit())
                    {
                        // 发生断点打断！保留当前 _currentIndex 不重置，下次恢复后继续推进
                        StopAndReturn(true);
                        return;
                    }

                    // 动作播放完成判定
                    if (!_agent.IsPlayingAction(currentEntry.action))
                    {
                        // 当前动作顺利完成，推进索引
                        AdvanceIndex();
                        StopAndReturn(true);
                    }
                    break;
            }
        }

        private void AdvanceIndex()
        {
            _currentIndex++;
            if (_currentIndex >= _data.sequence.Count)
            {
                // 整套连招套路打完，重置回第0段，并开启统一攻击间隔冷却
                _currentIndex = 0;
                _startCooldown?.Invoke(_data.attackInterval);
            }

            // 自主向黑板刷新下一段招式的有效射程
            SyncNextRangeToBlackboard();
        }

        private void SyncNextRangeToBlackboard()
        {
            if (_data.sequence != null && _currentIndex >= 0 && _currentIndex < _data.sequence.Count)
            {
                var nextEntry = _data.sequence[_currentIndex];
                if (nextEntry != null && Blackboard != null)
                {
                    Blackboard.Set("NextActionEffectiveRange", nextEntry.effectiveRange);
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            _phase = SequencerPhase.None;
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
