namespace Game.GamePlay
{
    /// <summary>
    /// 受击防御拦截策略契约。
    /// 无论极限闪避、招架弹刀、纯无敌、护盾吸收，只要具备“在受击裁决阶段拦截打击声明”能力的组件均实现此接口。
    /// </summary>
    public interface IHitDefenseModifier
    {
        /// <summary>
        /// 防御仲裁优先级（值越大越先结算）。
        /// 严格梯度保证判定确定性：
        /// 极限闪避 (300) > 招架弹刀 (200) > 纯无敌 (50)
        /// </summary>
        int DefensePriority { get; }

        /// <summary>
        /// 尝试拦截本次打击声明。
        /// </summary>
        /// <param name="ctx">命中总线上下文（包含攻击者、受击者、打击参数）</param>
        /// <param name="ownerBuff">挂载该效果的所属 Buff 实例（若非 Buff 驱动可为 null）</param>
        /// <returns>若返回 true，表示本次攻击已被该防御策略全权拦截/消费，受击管线短路</returns>
        bool TryInterceptHit(HitPipelineContext ctx, BuffInstance ownerBuff);
    }
}
