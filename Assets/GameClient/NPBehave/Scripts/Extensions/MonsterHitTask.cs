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
                // 一旦动作结束或者路由到非受击动作
                if (!_isPlayingAction(_currentAction))
                {
                    StopAndReturn(true); // 受击结束
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            _internalState = TaskState.None;
            RootNode.Clock.RemoveUpdateObserver(Tick);
            _clearHitStun?.Invoke(); // 清除黑板中的受击状态时间
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
