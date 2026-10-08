namespace Game.GamePlay
{
    /// <summary>
    /// 宏观动作领域标识。
    /// 用于对动作系统的宏观业务生命周期（移动阻尼、攻击连招、闪避无敌、受击硬直等）进行高内聚划分。
    /// </summary>
    public enum ActionDomainId
    {
        None = 0,

        /// <summary>
        /// 地面与空间位移领域（站立、步行、跑步、急停）。
        /// </summary>
        Locomotion = 1,

        /// <summary>
        /// 战斗与技能领域（普攻、特殊技、强化特殊技、终结技）。
        /// </summary>
        Combat = 2,

        /// <summary>
        /// 闪避与冲刺领域（点按闪避、长按冲刺、极限闪避）。
        /// </summary>
        Evasion = 3,

        /// <summary>
        /// 防御与招架领域（受击招架、格挡反击、支援招架）。
        /// </summary>
        Defense = 4,

        /// <summary>
        /// 受击与失衡领域（受击硬直、击退、浮空、失衡倒地）。
        /// </summary>
        HitReaction = 5,

        /// <summary>
        /// 角色切换领域（换入、换出、连携换人）。
        /// </summary>
        Switch = 6,

        /// <summary>
        /// 自定义扩展领域。
        /// </summary>
        Custom = 99,
    }
}
