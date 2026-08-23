using NPBehave;
using UnityEngine;
using System;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree.Extensions
{
    public class DistanceAdjustTask : Task
    {
        private DistanceAdjustData _data;
        private Func<float> _getDistance;
        private TryPlayActionDelegate _tryPlayAction;
        private Func<long, CommandFate> _checkCommandFate;
        private Func<ActionConfigAsset, bool> _isPlayingAction;

        private bool _isMovingForward;
        private float _timer;
        private ActionConfigAsset _currentAction;
        private long _commandId;
        private TaskState _internalState;

        public DistanceAdjustTask(
            DistanceAdjustData data, 
            Func<float> getDistance,
            TryPlayActionDelegate tryPlayAction,
            Func<long, CommandFate> checkCommandFate,
            Func<ActionConfigAsset, bool> isPlayingAction) : base("DistanceAdjustTask")
        {
            _data = data;
            _getDistance = getDistance;
            _tryPlayAction = tryPlayAction;
            _checkCommandFate = checkCommandFate;
            _isPlayingAction = isPlayingAction;
        }

        protected override void DoStart()
        {
            float dist = _getDistance();
            if (dist < 0)
            {
                Stopped(false); // 丢失目标，直接失败
                return;
            }

            // 如果刚好在圆周（加入小容差防止浮点数误差直接跳过）
            if (Mathf.Abs(dist - _data.referenceDistance) < 0.01f)
            {
                Stopped(true);
                return;
            }

            // 策略1：若要求进入圆内，且初始已在圆内，直接成功
            if (_data.strategy == DistanceAdjustStrategy.EnterCircle && dist <= _data.referenceDistance)
            {
                Stopped(true); 
                return;
            }

            // 判断距离方向并下发动作
            if (dist > _data.referenceDistance)
            {
                _isMovingForward = true;
                _currentAction = _data.walkF;
            }
            else
            {
                _isMovingForward = false;
                _currentAction = _data.walkB;
            }

            bool success = _tryPlayAction(_currentAction, out _commandId);
            if (!success)
            {
                Stopped(false); // 动作系统拒绝播放，直接失败
                return;
            }

            _internalState = TaskState.Pending;
            _timer = 0f;
            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            float dist = _getDistance();
            if (dist < 0)
            {
                StopAndReturn(false); // 突然丢锁
                return;
            }

            // --- [防卡死机制] ---
            if (_internalState == TaskState.Pending)
            {
                CommandFate fate = _checkCommandFate(_commandId);
                if (fate == CommandFate.Executed) _internalState = TaskState.Playing;
                else if (fate == CommandFate.Dropped) { StopAndReturn(false); return; }
            }
            else if (_internalState == TaskState.Playing)
            {
                // 如果动作被外部高优逻辑（如挨打硬直）切掉了，退出任务
                if (!_isPlayingAction(_currentAction)) { StopAndReturn(false); return; }
            }

            // --- [触及圆周判定 (利用穿越原则)] ---
            if (_isMovingForward && dist <= _data.referenceDistance)
            {
                StopAndReturn(true); // 前进时，距离等于或穿透变小，即为触及
                return;
            }
            if (!_isMovingForward && dist >= _data.referenceDistance)
            {
                StopAndReturn(true); // 后退时，距离等于或穿透变大，即为触及
                return;
            }

            // --- [策略3：后退超时判定] ---
            if (_data.strategy == DistanceAdjustStrategy.ReachCircumferenceTimeout && !_isMovingForward)
            {
                _timer += Time.deltaTime;
                if (_timer >= _data.timeLimit)
                {
                    StopAndReturn(true); // 倒计时结束，强行当做调整成功
                    return;
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            _internalState = TaskState.None;
            RootNode.Clock.RemoveUpdateObserver(Tick);
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
        }
    }
}
