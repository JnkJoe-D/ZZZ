namespace Game.Framework
{
    /// <summary>
    /// 受控 60Hz 逻辑步长驱动契约。
    /// 适用于：实体核心循环、状态机、技能路由、伤害计算、数值衰减、AI决策、指令缓冲。
    /// 由 FrameworkTimeManager / TimeManager 的 OnGameplayLogicTick 事件统一推进。
    /// </summary>
    public interface ILogicTickable
    {
        void OnLogicTick(float logicDeltaTime);
    }
}
