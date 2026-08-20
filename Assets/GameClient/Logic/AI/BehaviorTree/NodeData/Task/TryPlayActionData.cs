using UnityEngine;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#FF5555")]
    public class TryPlayActionData : ActionData
    {
        [Tooltip("要尝试播放的 Action 配置文件")]
        public ActionConfigAsset actionConfig;
    }
}
