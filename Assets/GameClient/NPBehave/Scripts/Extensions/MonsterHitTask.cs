using NPBehave;
using UnityEngine;
using System;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree.Extensions
{
    public class MonsterHitTask : Task
    {
        public delegate bool TryGetHitActionDelegate(out ActionConfigAsset hitAction);

        private TryGetHitActionDelegate _tryGetHitAction;
        private TryPlayActionDelegate _tryPlayAction;
        private Func<long, CommandFate> _checkCommandFate;
        private Func<ActionConfigAsset, bool> _isPlayingAction;
        private System.Action _clearHitStun;

        private long _commandId;
        private TaskState _internalState;
        private ActionConfigAsset _currentAction;

        public MonsterHitTask(
            TryGetHitActionDelegate tryGetHitAction,
            TryPlayActionDelegate tryPlayAction,
            Func<long, CommandFate> checkCommandFate,
            Func<ActionConfigAsset, bool> isPlayingAction,
            System.Action clearHitStun) : base("MonsterHitTask")
        {
            _tryGetHitAction = tryGetHitAction;
            _tryPlayAction = tryPlayAction;
            _checkCommandFate = checkCommandFate;
            _isPlayingAction = isPlayingAction;
            _clearHitStun = clearHitStun;
        }

        protected override void DoStart()
        {
            if (!_tryGetHitAction(out _currentAction) || _currentAction == null)
            {
                _clearHitStun?.Invoke();
                Stopped(false);
                return;
            }

            Debug.Log($"<color=orange>[MonsterHitTask] DoStart: Watching hit action '{_currentAction.name}', isAlreadyPlaying: {_isPlayingAction?.Invoke(_currentAction)}</color>");

            // 若底层物理命中管线已经即时切入了受击动作，直接进入 Playing 状态，无需重复下发指令
            if (_isPlayingAction != null && _isPlayingAction(_currentAction))
            {
                _internalState = TaskState.Playing;
                RootNode.Clock.AddUpdateObserver(Tick);
                return;
            }

            bool success = _tryPlayAction(_currentAction, out _commandId);
            if (!success)
            {
                _clearHitStun?.Invoke();
                Stopped(false);
                return;
            }

            _internalState = TaskState.Pending;
            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            if (_internalState == TaskState.Pending)
            {
                CommandFate fate = _checkCommandFate(_commandId);
                if (fate == CommandFate.Executed)
                {
                    _internalState = TaskState.Playing;
                }
                else if (fate == CommandFate.Dropped)
                {
                    StopAndReturn(false); // 动作被拒绝
                }
            }
            else if (_internalState == TaskState.Playing)
            {
                // 支持多段连续受击刷新（Combo 锁存）：
                // 若当前动作发生变化，检查是否切入了新的受击动作；若是，则更新当前观察目标并继续锁存等待
                if (!_isPlayingAction(_currentAction))
                {
                    if (_tryGetHitAction(out var newAction) && newAction != null && _isPlayingAction(newAction))
                    {
                        Debug.Log($"<color=orange>[MonsterHitTask] Combo refreshed: {_currentAction.name} -> {newAction.name}</color>");
                        _currentAction = newAction;
                        return; // 连招持续中，继续保持等待
                    }

                    // 连招结束且最后一个受击动作已播放完毕
                    Debug.Log($"<color=orange>[MonsterHitTask] Hit action '{_currentAction.name}' finished.</color>");
                    StopAndReturn(true);
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            _internalState = TaskState.None;
            RootNode.Clock.RemoveUpdateObserver(Tick);
            _clearHitStun?.Invoke(); // 清除黑板与受击状态，平滑恢复正常战斗 AI
            Debug.Log($"<color=orange>[MonsterHitTask] Completed with result={result}. Cleared hit stun.</color>");
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
