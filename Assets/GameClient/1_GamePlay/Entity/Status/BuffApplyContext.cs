namespace Game.GamePlay
{
    /// <summary>
    /// Buff 动态施加上下文。
    /// 承载外部来源（动作时间轴、肉鸽鸣徽系统、角色天赋等）对 Buff 初始参数的动态修正与上下文溯源。
    /// </summary>
    public class BuffApplyContext
    {
        /// <summary>施加来源实体（攻击者/施法者/队友）</summary>
        public CharacterEntity Instigator { get; set; }

        /// <summary>若由时间轴片段施加，记录时间轴片段的唯一标识 ClipId</summary>
        public string SourceClipId { get; set; }

        /// <summary>绝对持续时间覆盖（若指定，则直接取代配置的基础时长；负数或 null 表示使用基础时长）</summary>
        public float? OverrideDuration { get; set; }

        /// <summary>持续时间附加值（秒，鸣徽进阶强化加成，如 +3s）</summary>
        public float DurationAddition { get; set; } = 0f;

        /// <summary>持续时间倍率（鸣徽/词条缩放加成，如 1.5 表示延长 50%）</summary>
        public float DurationMultiplier { get; set; } = 1f;

        /// <summary>动态覆盖最大叠加层数（若有）</summary>
        public int? OverrideMaxStack { get; set; }

        /// <summary>效果数值倍率（鸣徽/局内养成提升，如伤害提升 40% 对应 1.4f）</summary>
        public float ValueMultiplier { get; set; } = 1f;

        /// <summary>动态覆盖初始充能/可生效次数</summary>
        public int? OverrideChargeCount { get; set; }

        /// <summary>默认空上下文静态单例，避免重复无意义堆分配</summary>
        public static readonly BuffApplyContext Default = new();
    }
}
