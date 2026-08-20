using System.Collections.Generic;
using NPBehave;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 根节点数据。
    /// 对应 NPBehave.Root —— 行为树的入口节点，拥有单个子节点。
    /// Root 继承自 Decorator，但在编辑器中作为特殊的顶层容器。
    /// </summary>
    [NodeColor("#FF6644")]
    public class RootData : DecoratorData
    {
 
    }
}
