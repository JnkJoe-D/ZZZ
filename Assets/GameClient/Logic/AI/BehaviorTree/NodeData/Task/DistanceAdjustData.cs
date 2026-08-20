using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public enum DistanceAdjustStrategy
    {
        EnterCircle,                // 策略1：进入圆内即成功
        ReachCircumference,         // 策略2：双向逼近，到达圆周成功
        ReachCircumferenceTimeout   // 策略3：双向逼近圆周，后退带超时
    }

    public class DistanceAdjustData : TaskData
    {
        public ActionConfigAsset walkF; // 前进动作
        public ActionConfigAsset walkB; // 后退动作
        public float referenceDistance; // 参考距离
        public DistanceAdjustStrategy strategy; // 调整策略
        public float timeLimit; // 超时限制（仅对策略3生效）
    }
}
