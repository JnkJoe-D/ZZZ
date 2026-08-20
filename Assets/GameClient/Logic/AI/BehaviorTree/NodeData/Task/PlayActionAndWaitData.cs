using UnityEngine;
using Game.Logic;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#FF9900")]
    public class PlayActionAndWaitData : ActionData
    {
        [Tooltip("要播放并等待其结束的 Action 配置文件")]
        public ActionConfigAsset actionConfig;
        [Tooltip("是否在Action播放结束后自动停止该行为树节点")]
        public bool stopAtEnd = true;
    }
}
