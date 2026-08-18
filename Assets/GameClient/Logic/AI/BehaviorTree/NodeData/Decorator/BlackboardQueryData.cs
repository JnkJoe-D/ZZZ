using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 黑板查询节点数据。
    /// 对应 NPBehave.BlackboardQuery —— 支持多个键的复杂查询。
    /// </summary>
    [NodeColor("#5588AA")]
    public class BlackboardQueryData : DecoratorData
    {
        public string[] keys;
        public NPBehave.Stops stopsOnChange = NPBehave.Stops.NONE;
    }
}
