using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 行为树节点的 SO 数据基类。
    /// 对应 NPBehave.Node —— 纯数据容器，不包含任何运行时逻辑。
    /// 仅用于编辑器序列化和可视化编辑。
    /// </summary>
    public abstract class NodeData : ScriptableObject
    {
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;

        /// <summary>
        /// 获取子节点列表（供编辑器图遍历使用）。
        /// </summary>
        public virtual List<NodeData> GetChildren() => new List<NodeData>();

        /// <summary>
        /// 添加子节点（供编辑器连线使用）。
        /// </summary>
        public virtual void AddChild(NodeData child) { }

        /// <summary>
        /// 移除子节点（供编辑器断线使用）。
        /// </summary>
        public virtual void RemoveChild(NodeData child) { }

        /// <summary>
        /// 深拷贝节点（供运行时克隆树使用）。
        /// </summary>
        public virtual NodeData Clone()
        {
            return Instantiate(this);
        }
    }
}
