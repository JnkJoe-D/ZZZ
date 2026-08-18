using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 调试节点，继承自 ActionData。
    /// 运行时将在控制台输出一段配置的日志字符串，输出完成后即视为节点执行成功 (Success)。
    /// </summary>
    [NodeColor("#555555")]
    public class DebugData : ActionData
    {
        [Tooltip("要输出的调试字符串信息。")]
        public string message = "Debug Log!";
    }
}
