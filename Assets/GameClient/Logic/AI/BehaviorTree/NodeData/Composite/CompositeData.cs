using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 组合节点数据基类。
    /// 对应 NPBehave.Composite —— 拥有多个子节点的节点（Selector、Sequence、Parallel 等）。
    /// </summary>
    public abstract class CompositeData : ContainerData
    {
        [HideInInspector]
        public List<NodeData> children = new List<NodeData>();

        public override List<NodeData> GetChildren() => children;

        public override void AddChild(NodeData child)
        {
            if (child != null && !children.Contains(child))
                children.Add(child);
        }

        public override void RemoveChild(NodeData child)
        {
            children.Remove(child);
        }

        public override NodeData Clone()
        {
            var clone = Instantiate(this);
            clone.children = children.Select(c => c.Clone()).ToList();
            return clone;
        }
    }
}
