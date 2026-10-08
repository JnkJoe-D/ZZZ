using UnityEngine;

namespace Game.GamePlay 
{
    /// <summary>
    /// 怪物冷却期自适应周旋对峙节点数据。
    /// 基于黑板中透传的 NextActionEffectiveRange 提前调整距离或就位后侧向环绕对峙。
    /// </summary>
    public class MonsterCoolingStrafeData : TaskData
    {
        [Tooltip("单次对峙周旋动作的最长持续时间 (秒)")]
        public float strafeDuration = 2.0f;

        [Tooltip("对峙基准距离 (米)。周旋时在此距离形成的迟滞区间内走动，默认 3.5m")]
        public float targetDistance = 3.5f;
    }
}
