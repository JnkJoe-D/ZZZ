using System;

namespace Game.Logic.AI.BehaviorTree
{
    public class ActionNode : LeafNode
    {
        private System.Action _action;
        private System.Func<bool> _func;
        private System.Func<ActionNode, bool> _asyncAction;

        public ActionNode(System.Action action)
        {
            _action = action;
        }

        public ActionNode(System.Func<bool> func)
        {
            _func = func;
        }

        public ActionNode(System.Func<ActionNode, bool> asyncAction)
        {
            _asyncAction = asyncAction;
        }

        protected override void DoStart()
        {
            if (_action != null)
            {
                _action.Invoke();
                Stopped(true);
            }
            else if (_func != null)
            {
                Stopped(_func.Invoke());
            }
            else if (_asyncAction != null)
            {
                _asyncAction.Invoke(this);
            }
        }

        public void FinishExecute(bool success)
        {
            if (CurrentState == NodeState.Active)
            {
                Stopped(success);
            }
        }
    }
}
