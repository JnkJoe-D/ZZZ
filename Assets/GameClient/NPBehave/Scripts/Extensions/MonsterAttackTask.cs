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
        private Func<bool> _isInterruptedByHit;

        private long _commandId;
        private TaskState _internalState;

        public MonsterAttackTask(
            MonsterAttackData data,
            TryPlayActionDelegate tryPlayAction,
            Func<long, CommandFate> checkCommandFate,
            Func<ActionConfigAsset, bool> isPlayingAction,
            Action<float> startCooldown,
            Func<bool> isInterruptedByHit = null) : base("MonsterAttackTask")
        {
            _data = data;
            _tryPlayAction = tryPlayAction;
            _checkCommandFate = checkCommandFate;
            _isPlayingAction = isPlayingAction;
            _startCooldown = startCooldown;
            _isInterruptedByHit = isInterruptedByHit;
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
                    if (_isInterruptedByHit != null && _isInterruptedByHit())
                    {
                        Debug.Log($"<color=yellow>[MonsterAttackTask] Action {_data.action.name} was interrupted by hit reaction.</color>");
                    }
                    else
                    {
                        Debug.Log($"Action {_data.action.name} has finished for monster attack");
                    }

                    StopAndReturn(true); // 无论正常结束还是受击打断，均返回 true，防止 Selector 顺序执行后续周旋分支覆盖受击状态
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
