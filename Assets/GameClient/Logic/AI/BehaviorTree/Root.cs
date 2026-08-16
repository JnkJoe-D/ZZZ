using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class Root : Container
    {
        public Node child;
        public Node MainNode { get => child; private set => child = value; }
        
        public Root() {}

        public Root(Node mainNode)
        {
            MainNode = mainNode;
            if(MainNode != null) MainNode.ParentNode = this;
        }

        public override void SetTree(BehaviorTreeAsset tree)
        {
            base.SetTree(tree);
            if(MainNode != null) MainNode.SetTree(tree);
        }

        protected override void DoStart()
        {
            if(MainNode != null) MainNode.Start();
        }

        protected override void DoStop()
        {
            if(MainNode != null) MainNode.Stop();
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
        public override List<Node> GetChildren() 
        { 
            return child != null ? new List<Node> { child } : new List<Node>(); 
        }
        
    
        public override Node Clone()
        {
            Root node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
                node.child.ParentNode = node;
            }
            return node;
        }

}
}
