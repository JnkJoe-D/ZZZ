using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree.Extensions
{
    public enum TaskState { Pending, Playing, None }
    public delegate bool TryPlayActionDelegate(ActionConfigAsset actionConfig, out long commandId);

    public class PlayActionAndWaitTask : Task
    {
        private ActionConfigAsset _actionConfig;
        private bool _stopAtEnd;
        private TryPlayActionDelegate _tryPlayAction;
        private System.Func<long, CommandFate> _checkCommandFate;
        private System.Func<bool> _isCurrentPlayingAction;
        

        private TaskState _internalState = TaskState.None;
        private long _commandId;

        public PlayActionAndWaitTask(
            ActionConfigAsset actionConfig, 
            bool stopAtEnd,
            TryPlayActionDelegate tryPlayAction, 
            System.Func<long, CommandFate> checkCommandFate,
            System.Func<bool> isCurrentPlayingAction) : base("PlayActionAndWait")
        {
            _actionConfig = actionConfig;
            _stopAtEnd = stopAtEnd;
            _tryPlayAction = tryPlayAction;
            _checkCommandFate = checkCommandFate;
            _isCurrentPlayingAction = isCurrentPlayingAction;
        }

        protected override void DoStart()
        {
            if (_actionConfig == null || _tryPlayAction == null || _checkCommandFate == null || _isCurrentPlayingAction == null)
            {
                Stopped(false);
                Debug.Log("PlayActionAndWaitTask: Invalid parameters provided.");
                return;
            }

            _internalState = TaskState.Pending;
            bool success = _tryPlayAction(_actionConfig, out _commandId);
            if (!success)
            {
                Stopped(false);
                Debug.Log($"PlayActionAndWaitTask: Failed to play action {_actionConfig.name}.");
                return;
            }

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
                    RootNode.Clock.RemoveUpdateObserver(Tick);
                    Stopped(false);
                    Debug.Log($"PlayActionAndWaitTask: Command {_commandId} was dropped for action {_actionConfig.name}.");
                    return;
                }
            }

            if (_internalState == TaskState.Playing && _stopAtEnd)
            {
                if (_isCurrentPlayingAction() == false)
                {
                    RootNode.Clock.RemoveUpdateObserver(Tick);
                    Stopped(true); 
                    Debug.Log($"PlayActionAndWaitTask: Action {_actionConfig.name} has finished playing.");
                }
            }
        }

        protected override void DoStop()
        {
            _internalState = TaskState.None;
            RootNode.Clock.RemoveUpdateObserver(Tick);
            Stopped(false);
        }
    }
}
