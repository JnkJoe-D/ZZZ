using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 装饰器节点数据基类。
    /// 对应 NPBehave.Decorator —— 拥有单个子节点的节点。
    /// </summary>
    public abstract class DecoratorData : ContainerData
    {
        [HideInInspector]
        public NodeData child;

        public override List<NodeData> GetChildren()
        {
            return child != null ? new List<NodeData> { child } : new List<NodeData>();
        }

        public override void AddChild(NodeData c)
        {
            child = c;
        }

        public override void RemoveChild(NodeData c)
        {
            if (child == c) child = null;
        }

        public override NodeData Clone()
        {
            var clone = Instantiate(this);
            if (child != null) clone.child = child.Clone();
            return clone;
        }
    }
}
