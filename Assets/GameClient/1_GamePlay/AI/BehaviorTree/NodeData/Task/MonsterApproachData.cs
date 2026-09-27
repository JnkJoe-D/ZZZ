using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物攻击前战术逼近节点数据。
    /// 纯策略节点：仅向 Context 声明逼近意图与目标射程，由状态机微观自决策走跑与刹车。
    /// </summary>
    [NodeColor("#546E7A")]
    public class MonsterApproachData : TaskData
    {
        [Tooltip("当黑板未设置 NextActionEffectiveRange 时的默认射程保底 (米)")]
        public float defaultRange = 3.5f;

        [Tooltip("逼近超时时限 (秒)，防止目标不可达导致死锁")]
        public float timeout = 8.0f;
    }
}
