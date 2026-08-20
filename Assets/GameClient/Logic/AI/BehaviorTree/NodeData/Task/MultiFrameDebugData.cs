using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#5555FF")]
    public class MultiFrameDebugData : ActionData
    {
        [Tooltip("持续多少秒")]
        public float duration = 3f;

        [Tooltip("打印的信息")]
        public string message = "Debugging...";
    }
}
