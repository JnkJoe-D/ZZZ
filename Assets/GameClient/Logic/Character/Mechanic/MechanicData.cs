using Game.Framework;

namespace Game.Logic
{
    /// <summary>
    /// 角色专属机制的通用数据载体
    /// 无论是层数、充能进度、还是特殊状态，都可以用这个结构体统一传递
    /// </summary>
    public struct MechanicData
    {
        public int StackCount;      // 层数/次数（如：急冻层数 1-3）
        public float Progress;      // 进度/能量（如：狼哥蓄力值 0-1）
        public float MaxProgress;   // 最大进度（可选）
        public int StateFlag;       // 特殊状态枚举（如：0=正常，1=过热，2=强化）
    }

    /// <summary>
    /// 核心机制状态变化事件
    /// 任何角色的特有机制数值发生变化时，抛出此事件供UI层（StatusPanelModule）捕获
    /// </summary>
    public struct MechanicStatChangedEvent : IGameEvent
    {
        public int PlayerId; // 触发该机制的角色实体ID (Entity.GetInstanceID())
        public MechanicData Data;

        public MechanicStatChangedEvent(int playerId, MechanicData data)
        {
            PlayerId = playerId;
            Data = data;
        }
    }
}
