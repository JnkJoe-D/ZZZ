namespace Game.Logic
{
    /// <summary>
    /// Buff 运行时实例。每次施加创建一个，由 BuffContainer 管理生命周期。
    /// </summary>
    public class BuffInstance
    {
        private static int _nextRuntimeId;

        /// <summary>运行时唯一 ID，用于修改器的 SourceId 追踪。</summary>
        public int RuntimeId { get; }

        /// <summary>Buff 定义资产。</summary>
        public BuffDefAsset Definition { get; }

        /// <summary>剩余持续时间。-1 表示永久。</summary>
        public float RemainingTime { get; set; }

        /// <summary>当前叠加层数。</summary>
        public int CurrentStack { get; set; }

        /// <summary>施加者（可为 null）。</summary>
        public CharacterEntity Source { get; }

        /// <summary>是否为永久 Buff。</summary>
        public bool IsPermanent => Definition.Duration < 0f;

        /// <summary>是否已过期。</summary>
        public bool IsExpired => !IsPermanent && RemainingTime <= 0f;

        public BuffInstance(BuffDefAsset definition, CharacterEntity source)
        {
            RuntimeId = ++_nextRuntimeId;
            Definition = definition;
            Source = source;
            RemainingTime = definition.Duration;
            CurrentStack = 1;
        }

        /// <summary>刷新持续时间为定义值。</summary>
        public void RefreshDuration()
        {
            if (!IsPermanent)
            {
                RemainingTime = Definition.Duration;
            }
        }

        /// <summary>尝试叠加。返回是否成功增加了层数。</summary>
        public bool TryStack()
        {
            if (CurrentStack >= Definition.MaxStack)
            {
                return false;
            }
            CurrentStack++;
            return true;
        }
    }
}
