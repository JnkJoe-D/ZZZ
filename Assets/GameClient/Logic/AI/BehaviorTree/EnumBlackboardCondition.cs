using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#CC9933")]
    public class EnumBlackboardCondition : BlackboardCondition
    {
        public BlackboardKey blackboardKey;
        [Tooltip("期望布尔值为")]
        public bool expectedValue = true;
        [Tooltip("打断类型")]
        public Stops abortType = Stops.None;

        public override void SetTree(BehaviorTreeAsset tree)
        {
            // 将编辑器配置的值同步到底层 BlackboardCondition 的隐式字段中
            this._key = blackboardKey.ToString();
            this._op = Operator.IsEqual;
            this._value = expectedValue;
            this._stops = abortType;
            
            base.SetTree(tree);
        }
    }
}
