using UnityEngine;
using UnityEngine.Assertions;

namespace NPBehave
{
    /// <summary>
    /// A decorator that listens to a blackboard key. When the key's value changes, 
    /// it unconditionally restarts its active child or aborts lower priority branches.
    /// Used for event-driven "Trigger" behavior that forces a restart even if already active.
    /// </summary>
    public class BlackboardTrigger : Decorator
    {
        private string key;
        private Stops stopsOnChange;
        private bool isObserving;
        private bool restartPending;
        private bool isTriggered;

        public BlackboardTrigger(string key, Stops stopsOnChange, Node decoratee) : base("BlackboardTrigger", decoratee)
        {
            this.key = key;
            this.stopsOnChange = stopsOnChange;
            this.isObserving = false;
            this.isTriggered = false;
        }

        protected override void DoStart()
        {
            if (stopsOnChange != Stops.NONE)
            {
                if (!isObserving)
                {
                    isObserving = true;
                    this.RootNode.Blackboard.AddObserver(key, onValueChanged);
                }
            }
            
            if (isTriggered)
            {
                isTriggered = false;
                Decoratee.Start();
            }
            else
            {
                Stopped(false);
            }
        }

        override protected void DoStop()
        {
            Decoratee.Stop();
        }

        protected override void DoChildStopped(Node child, bool result)
        {
            if (restartPending)
            {
                restartPending = false;
                Decoratee.Start();
                return;
            }

            Assert.AreNotEqual(this.CurrentState, State.INACTIVE);
            if (stopsOnChange == Stops.NONE || stopsOnChange == Stops.SELF)
            {
                if (isObserving)
                {
                    isObserving = false;
                    this.RootNode.Blackboard.RemoveObserver(key, onValueChanged);
                }
            }
            Stopped(result);
        }

        override protected void DoParentCompositeStopped(Composite parentComposite)
        {
            if (isObserving)
            {
                isObserving = false;
                this.RootNode.Blackboard.RemoveObserver(key, onValueChanged);
            }
        }

        private void onValueChanged(Blackboard.Type type, object newValue)
        {
            if (IsActive)
            {
                if (stopsOnChange == Stops.SELF || stopsOnChange == Stops.BOTH || stopsOnChange == Stops.IMMEDIATE_RESTART)
                {
                    // 标记需要重启，并杀掉当前的子节点
                    restartPending = true;
                    this.Decoratee.Stop();
                }
            }
            else
            {
                if (stopsOnChange == Stops.LOWER_PRIORITY || stopsOnChange == Stops.BOTH || stopsOnChange == Stops.IMMEDIATE_RESTART || stopsOnChange == Stops.LOWER_PRIORITY_IMMEDIATE_RESTART)
                {
                    isTriggered = true; // <--- This allows it to start next time DoStart is called

                    Container parentNode = this.ParentNode;
                    Node childNode = this;
                    while (parentNode != null && !(parentNode is Composite))
                    {
                        childNode = parentNode;
                        parentNode = parentNode.ParentNode;
                    }
                    Assert.IsNotNull(parentNode, "BlackboardTrigger is only valid when attached to a parent composite");
                    Assert.IsNotNull(childNode);
                    if (parentNode is Parallel)
                    {
                        Assert.IsTrue(stopsOnChange == Stops.IMMEDIATE_RESTART, "On Parallel Nodes all children have the same priority, thus Stops.LOWER_PRIORITY or Stops.BOTH are unsupported in this context!");
                    }

                    if (stopsOnChange == Stops.IMMEDIATE_RESTART || stopsOnChange == Stops.LOWER_PRIORITY_IMMEDIATE_RESTART)
                    {
                        if (isObserving)
                        {
                            isObserving = false;
                            this.RootNode.Blackboard.RemoveObserver(key, onValueChanged);
                        }
                    }

                    ((Composite)parentNode).StopLowerPriorityChildrenForChild(childNode, stopsOnChange == Stops.IMMEDIATE_RESTART || stopsOnChange == Stops.LOWER_PRIORITY_IMMEDIATE_RESTART);
                }
            }
        }
    }
}
