using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class Selector : CompositeNode
    {
        private int _currentIndex = -1;

        public Selector(params Node[] children) : base(children) { }

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
            if (success)
            {
                Stopped(true);
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
                Stopped(false);
            }
        }
        
        public void StopLowerPriorityChildrenForChild(Node child)
        {
            int index = Children.IndexOf(child);
            if (index < 0 || index >= _currentIndex) return;

            // child is higher priority (lower index) than what is currently running
            var activeChild = Children[_currentIndex];
            _currentIndex = index - 1; // Move index back so ProcessChildren() will increment it to 'index'
            
            // Wait for the active child to stop completely, which will call ChildStopped.
            // But we actually need to handle the restart of `child` properly, typically 
            // the decorator Handles restarting itself.
            activeChild.Stop();
        }
    }
}
