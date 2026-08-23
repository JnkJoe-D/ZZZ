using NPBehave;
using UnityEngine;
using System;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree.Extensions
{
    public class MonsterAttackTask : Task
    {
        private MonsterAttackData _data;
        private TryPlayActionDelegate _tryPlayAction;
        private Func<long, CommandFate> _checkCommandFate;
        private Func<ActionConfigAsset, bool> _isPlayingAction;
        private Action<float> _startCooldown;

        private long _commandId;
        private TaskState _internalState;

        public MonsterAttackTask(
            MonsterAttackData data,
            TryPlayActionDelegate tryPlayAction,
            Func<long, CommandFate> checkCommandFate,
            Func<ActionConfigAsset, bool> isPlayingAction,
            Action<float> startCooldown) : base("MonsterAttackTask")
        {
            _data = data;
            _tryPlayAction = tryPlayAction;
            _checkCommandFate = checkCommandFate;
            _isPlayingAction = isPlayingAction;
            _startCooldown = startCooldown;
        }

        protected override void DoStart()
        {
            if (_data.action == null)
            {
                Stopped(false);
                Debug.LogError($"Invalid action for monster attack: {_data.action?.name ?? "null"}");
                return;
            }

            bool success = _tryPlayAction(_data.action, out _commandId);
            if (!success)
            {
                Stopped(false);
                Debug.LogError($"Failed to play action: {_data.action.name}");
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
                    StopAndReturn(false); // 指令被丢弃，任务失败
                    Debug.LogError($"Command {_commandId} was dropped for action: {_data.action.name}");
                }
            }
            else if (_internalState == TaskState.Playing)
            {
                // 一旦在播放状态且 currentaction != action，代表当前攻击动作已结束或路由到了其他动作
                if (!_isPlayingAction(_data.action))
                {
                    Debug.Log($"Action {_data.action.name} has finished for monster attack");
                    StopAndReturn(true); // 动作结束，开启冷却并返回成功
                }
            }
        }

        private void StopAndReturn(bool result)
        {
            if(_internalState == TaskState.Playing)
            {
                // 开启 behaviorRuntimeData 的冷却计时
                _startCooldown?.Invoke(_data.cooldown);
            }
            _internalState = TaskState.None;
            RootNode.Clock.RemoveUpdateObserver(Tick);
            Stopped(result);
        }

        protected override void DoStop()
        {
            StopAndReturn(false);
            Debug.Log($"MonsterAttackTask DoStop. Current state: {_internalState}");
        }
    }
}
