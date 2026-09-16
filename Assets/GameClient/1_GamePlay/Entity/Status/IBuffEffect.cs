namespace Game.GamePlay
{
    /// <summary>
    /// Buff 效果接口。每种具体的 Buff 行为实现此接口。
    /// 由 Luban 配置的 BuffEffectData 驱动，在运行时由 BuffEffectRegistry 动态构建执行。
    /// </summary>
    public interface IBuffEffect
    {
        /// <summary>Buff 首次施加到目标时调用。</summary>
        void OnApply(BuffInstance buff, CharacterEntity target);

        /// <summary>每帧调用（仅在 Buff 存活期间）。</summary>
        void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime);

        /// <summary>Buff 叠加层数变化时调用。</summary>
        void OnStack(BuffInstance buff, CharacterEntity target, int newStack);

        /// <summary>Buff 被移除时调用（到期、驱散、替换等）。</summary>
        void OnRemove(BuffInstance buff, CharacterEntity target);
    }
}
