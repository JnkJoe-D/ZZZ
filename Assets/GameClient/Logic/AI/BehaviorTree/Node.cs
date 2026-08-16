using System;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public abstract class Node: ScriptableObject
    {
        public NodeState CurrentState { get; private set; } = NodeState.Inactive;
        public BehaviorTreeAsset Tree { get; private set; }
        [HideInInspector]public string guid;
        [HideInInspector]public Vector2 position;
        public virtual void SetTree(BehaviorTreeAsset tree)
        {
            Tree = tree;
        }

        public void Start()
        {
            if (CurrentState != NodeState.Inactive)
            {
                UnityEngine.Debug.LogWarning("Trying to start a node that is already active!");
                return;
            }

            CurrentState = NodeState.Active;
            DoStart();
        }

        public void Stop()
        {
            if (CurrentState != NodeState.Active)
            {
                return;
            }

            CurrentState = NodeState.StopRequested;
            DoStop();
        }

        protected abstract void DoStart();
        protected abstract void DoStop();

        protected void Stopped(bool success)
        {
            if (CurrentState == NodeState.Inactive)
            {
                return;
            }

            CurrentState = NodeState.Inactive;
            if (Tree != null)
            {
                // In a full implementation, we might want to schedule the parent's notification 
                // in the next tick to prevent deep recursion/stack overflow on large trees 
                // when things complete synchronously.
                // For simplicity, we directly invoke parent callback here.
            }
            DoStopped(success);
        }

        protected virtual void DoStopped(bool success)
        {
            ParentNode?.ChildStopped(this, success);
        }

        public Container ParentNode { get; set; }

        // Editor and Tree building methods
        public virtual void AddChild(Node child) { }
        public virtual void RemoveChild(Node child) { }
        public virtual System.Collections.Generic.List<Node> GetChildren() { return new System.Collections.Generic.List<Node>(); }
        public virtual Node Clone() { return Instantiate(this); }

    }

    public abstract class Container : Node
    {
        public abstract void ChildStopped(Node child, bool success);
    }
}
