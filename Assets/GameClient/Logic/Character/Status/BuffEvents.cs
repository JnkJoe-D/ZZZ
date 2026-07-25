using Game.Framework;

namespace Game.Logic
{
    /// <summary>Buff 施加事件。</summary>
    public struct BuffAppliedEvent : IGameEvent
    {
        public CharacterEntity Target;
        public BuffInstance Buff;
    }

    /// <summary>Buff 移除事件。</summary>
    public struct BuffRemovedEvent : IGameEvent
    {
        public CharacterEntity Target;
        public BuffDefAsset Definition;
        public BuffRemoveReason Reason;
    }
}
