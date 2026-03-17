namespace Game.Logic.Character
{
    /// <summary>
    /// 命中效果接口。每种 eventTag 对应一个 IHitImpact 实现。
    /// 不同的 Impact 表现不同：扣血、加血、击退、加 Buff、播放特定受击动画等。
    /// </summary>
    public interface IHitImpact
    {
        /// <summary>
        /// 执行该效果。
        /// </summary>
        /// <param name="ctx">命中上下文（包含攻击者、受击者、碰撞信息等）</param>
        /// <param name="entry">当前正在处理的效果条目（包含 eventTag 和 targetTags）</param>
        void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry);
    }
}
