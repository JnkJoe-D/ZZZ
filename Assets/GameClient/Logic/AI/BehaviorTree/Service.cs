using System;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class Service : DecoratorNode
    {
        private float _interval;
        private float _randomVariance;
        private System.Action _serviceMethod;

        public Service() {}

        public Service(float interval, float randomVariance, System.Action serviceMethod, Node decoratee) : base(decoratee)
        {
            _interval = interval;
            _randomVariance = randomVariance;
            _serviceMethod = serviceMethod;
        }

        public Service(float interval, System.Action serviceMethod, Node decoratee) 
            : this(interval, 0f, serviceMethod, decoratee) { }

        protected override void DoStart()
        {
            Tree.blackboard.Clock.AddTimer(_interval, _randomVariance, -1, ExecuteService);
            base.DoStart();
            // Invoke immediately on start
            ExecuteService();
        }

        protected override void DoStop()
        {
            Tree.blackboard.Clock.RemoveTimer(ExecuteService);
            base.DoStop();
        }

        public override void ChildStopped(Node c, bool success)
        {
            Tree.blackboard.Clock.RemoveTimer(ExecuteService);
            base.ChildStopped(c, success);
        }

        private void ExecuteService()
        {
            _serviceMethod?.Invoke();
        }
    }
}
