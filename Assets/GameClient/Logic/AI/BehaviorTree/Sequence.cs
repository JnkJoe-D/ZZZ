using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class Sequence : CompositeNode
    {
        private int _currentIndex = -1;

        public Sequence(params Node[] children) : base(children) { }

        protected override void DoStart()
        {
            _currentIndex = -1;
            ProcessChildren();
        }

        protected override void DoStop()
        {
            if (_currentIndex >= 0 && _currentIndex < Children.Count)
            {
                Children[_currentIndex].Stop();
            }
            else
            {
                Stopped(false);
            }
        }

        public override void ChildStopped(Node child, bool success)
        {
            if (!success)
            {
                Stopped(false);
            }
            else
            {
                ProcessChildren();
            }
        }

        private void ProcessChildren()
        {
            if (CurrentState != NodeState.Active) return;

            _currentIndex++;
            if (_currentIndex < Children.Count)
            {
                Children[_currentIndex].Start();
            }
            else
            {
                Stopped(true);
            }
        }
    }
}
