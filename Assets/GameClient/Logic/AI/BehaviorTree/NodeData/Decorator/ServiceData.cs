using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 服务节点数据。
    /// 对应 NPBehave.Service —— 周期性执行回调的装饰器，不影响子节点的执行结果。
    /// 注意：具体的 service 回调由 Translator 阶段根据上下文注入，这里只存配置参数。
    /// </summary>
    public enum ServiceMethodType
    {
        UpdateTargetState,
        UpdateSelfState
    }

    [NodeColor("#33CC80")]
    public class ServiceData : DecoratorData
    {
        [Tooltip("指定该服务节点要执行的黑板更新方法。")]
        public ServiceMethodType methodType = ServiceMethodType.UpdateTargetState;

        [Tooltip("服务回调的执行间隔（秒）。设为 0 或负值表示每帧执行。")]
        public float interval = 0.5f;

        [Tooltip("间隔的随机浮动范围（秒）。")]
        public float randomVariation = 0.05f;
    }
}
