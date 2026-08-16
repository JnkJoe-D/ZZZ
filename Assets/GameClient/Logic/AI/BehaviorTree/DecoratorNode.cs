using System;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public abstract class DecoratorNode : Container
    {
        public Node child;
        public Node Decoratee { get => child; private set => child = value; }

        public DecoratorNode() {}
        
        public DecoratorNode(Node decoratee)
        {
            Decoratee = decoratee;
            if(Decoratee != null) Decoratee.ParentNode = this;
        }

        public override void SetTree(BehaviorTreeAsset tree)
        {
            base.SetTree(tree);
            if(Decoratee != null) Decoratee.SetTree(tree);
        }

        protected override void DoStart()
        {
            if(Decoratee != null) Decoratee.Start();
        }

        protected override void DoStop()
        {
            if(Decoratee != null) Decoratee.Stop();
        }

        public override void ChildStopped(Node c, bool success)
        {
            Stopped(success);
        }

        public override void AddChild(Node c) 
        { 
            child = c; 
            if(c != null) c.ParentNode = this; 
        }
        
        public override void RemoveChild(Node c) 
        { 
            if (child == c) { child = null; c.ParentNode = null; } 
        }
        
        public override System.Collections.Generic.List<Node> GetChildren() 
        { 
            var list = new System.Collections.Generic.List<Node>(); 
            if (child != null) list.Add(child); 
            return list; 
        }
    
        public override Node Clone()
        {
            DecoratorNode node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
                node.child.ParentNode = node;
            }
            return node;
        }

}
}
