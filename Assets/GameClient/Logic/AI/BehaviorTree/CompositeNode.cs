using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public abstract class CompositeNode : Container
    {
        public List<Node> children = new List<Node>();
        public List<Node> Children { get => children; private set => children = value; }
        
        public CompositeNode() {}

        public CompositeNode(params Node[] childrenArray)
        {
            children = new List<Node>(childrenArray);
            foreach (var child in children)
            {
                if(child != null) child.ParentNode = this;
            }
        }

        public override void SetTree(BehaviorTreeAsset tree)
        {
            base.SetTree(tree);
            foreach (var child in children)
            {
                if(child != null) child.SetTree(tree);
            }
        }
        
        public override void AddChild(Node child) 
        { 
            if (children == null) children = new List<Node>();
            if(child != null) { children.Add(child); child.ParentNode = this; } 
        }
        
        public override void RemoveChild(Node child) 
        { 
            if (children != null && children.Contains(child)) { children.Remove(child); child.ParentNode = null; } 
        }
        
        public override List<Node> GetChildren() 
        { 
            if (children == null) children = new List<Node>();
            return children; 
        }
    
        public override Node Clone()
        {
            CompositeNode node = Instantiate(this);
            node.children = new List<Node>();
            foreach (var c in children)
            {
                if(c == null) continue;
                var childClone = c.Clone();
                childClone.ParentNode = node;
                node.children.Add(childClone);
            }
            return node;
        }

}
}
