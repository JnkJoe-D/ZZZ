using System;
using Game.Framework;
using NPBehave;
using UnityEngine;
using Game.GamePlay;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物宏观连招序列选择器任务。
    /// 纯宏观战术节点（不包含任何物理位移与微观步态计算）：
    /// 1. 维护连招套路断点索引（支持受击打断记忆）；
    /// 2. 向黑板同步当前/下一段招式的有效攻击距离 NextActionEffectiveRange；
    /// 3. 向战术上下文下发 PendingAttack，由底层状态机 (MonsterAttackState) 权威消费执行；
    /// 4. 监测动作播放完成，推进序列索引，打完全套开启公共冷却。
    /// </summary>
    public class MonsterAttackSequenceTask : Task
    {
        private readonly MonsterAttackSequenceData _data;
        private readonly TreeActionAgent _agent;
        private readonly Action<float> _startCooldown;
        private readonly Func<bool> _isInterruptedByHit;
        private readonly float _timeout;

        private int _currentIndex = 0;
        private bool _hasBeenConsumed = false;
        private double _startTime;
        private ActionConfigAsset _currentExecutingAction;

        public MonsterAttackSequenceTask(
            MonsterAttackSequenceData data,
            TreeActionAgent agent,
            Action<float> startCooldown,
            Func<bool> isInterruptedByHit,
            float timeout = 8.0f) : base("MonsterAttackSequenceTask")
        {
            _data = data;
            _agent = agent;
            _startCooldown = startCooldown;
            _isInterruptedByHit = isInterruptedByHit;
            _timeout = timeout;
        }

        public override void SetRoot(Root rootNode)
        {
            base.SetRoot(rootNode);
            SyncNextRangeToBlackboard();
        }

        protected override void DoStart()
        {
            if (_data == null || _data.sequence == null || _data.sequence.Count == 0)
            {
                Stopped(false);
                return;
            }

            if (_agent.Context == null)
            {
                Stopped(false);
                return;
            }

            // 越界安全保护
            if (_currentIndex < 0 || _currentIndex >= _data.sequence.Count)
            {
                _currentIndex = 0;
            }

            var currentEntry = _data.sequence[_currentIndex];
            if (currentEntry == null || currentEntry.action == null)
            {
                AdvanceIndex();
                Stopped(true);
                return;
            }

            // 启动时同步当前动作的有效射程到黑板
            SyncNextRangeToBlackboard();

            _currentExecutingAction = currentEntry.action;
            _hasBeenConsumed = false;
            _startTime = RootNode.Clock.ElapsedTime;

            // 宏观意图下发：声明出刀策略与待执行攻击资产
            _agent.Context.Strategy = MonsterStrategy.Attack;
            _agent.Context.PendingAttack = _currentExecutingAction;

            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            // 1. 监测受击打断（招架反击 / 受击失衡）
            if (_isInterruptedByHit != null && _isInterruptedByHit())
            {
                // 发生断点打断！本次攻击完成，更新index,下次恢复后继续推进
                AdvanceIndex();
                StopAndReturn(true);
                return;
            }

            var ctx = _agent.Context;
            if (ctx == null)
            {
                StopAndReturn(false);
                return;
            }

            // 2. 监测状态机是否已消费 PendingAttack
            if (!_hasBeenConsumed && ctx.PendingAttack == null)
            {
                _hasBeenConsumed = true;
            }

            // 3. 状态机已消费，且动作播放完毕
            if (_hasBeenConsumed)
            {
                if (!_agent.IsPlayingAction(_currentExecutingAction))
                {
                    // 当前动作顺利完成，推进索引
                    AdvanceIndex();
                    StopAndReturn(true);
                    return;
                }
            }

            // 4. 超时安全防护：防止动作未播放或卡死导致行为树永久挂起
            if (_timeout > 0f && (RootNode.Clock.ElapsedTime - _startTime >= _timeout))
            {
                GLog.Warning(LogTags.AI, $"MonsterAttackSequenceTask timed out on index {_currentIndex}, resetting sequence.");
                _currentIndex = 0;
                _startCooldown?.Invoke(_data.attackInterval);
                SyncNextRangeToBlackboard();
                StopAndReturn(false);
            }
        }

        private void AdvanceIndex()
        {
            _currentIndex++;
            if (_currentIndex >= _data.sequence.Count)
            {
                // 整套连招套路打完，重置回第0段
                _currentIndex = 0;
            }
            //每打完一段，开启统一攻击间隔冷却
            _startCooldown?.Invoke(_data.attackInterval);

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
                    Blackboard.Set(BBKeyMapper.GetString(BBKey.NextActionEffectiveRange), nextEntry.effectiveRange);
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            RootNode.Clock.RemoveUpdateObserver(Tick);
            if (_agent.Context != null && _agent.Context.PendingAttack == _currentExecutingAction)
            {
                _agent.Context.PendingAttack = null;
            }
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
