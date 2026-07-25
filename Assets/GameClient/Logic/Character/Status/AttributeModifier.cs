namespace Game.Logic
{
    /// <summary>
    /// 属性修改器的运算类型。
    /// </summary>
    public enum ModifierOp
    {
        /// <summary>固定值加减。</summary>
        Flat = 0,
        /// <summary>百分比加减（基于基础值）。</summary>
        Percent = 10,
    }

    /// <summary>
    /// 属性修改器。由 Buff 效果创建，挂载到 AttributeInstance 上。
    /// 修改器被移除后属性值自动回算。
    /// </summary>
    public class AttributeModifier
    {
        /// <summary>修改器的唯一标识，用于精准移除。</summary>
        public int Id { get; }

        /// <summary>运算类型。</summary>
        public ModifierOp Op { get; }

        /// <summary>修改值。Flat 模式为绝对值，Percent 模式为百分比 (0.2 = +20%)。</summary>
        public float Value { get; }

        /// <summary>来源标记（通常是 BuffInstance 的 RuntimeId），用于按来源批量移除。</summary>
        public int SourceId { get; }

        private static int _nextId;

        public AttributeModifier(ModifierOp op, float value, int sourceId = 0)
        {
            Id = ++_nextId;
            Op = op;
            Value = value;
            SourceId = sourceId;
        }
    }
}
