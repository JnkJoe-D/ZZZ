using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物战术出刀任务节点数据。
    /// 纯意图节点：向 Context 写入待播放的动作资产，由状态机微观切入 MonsterAttackState 消费执行。
    /// </summary>
    [NodeColor("#E53935")]
    public class MonsterAttackData : TaskData
    {
        [Tooltip("待施放的攻击动作资产（若为空则从黑板 NextAttackAction 读取）")]
        public ActionConfigAsset attackAction;
    }
}
