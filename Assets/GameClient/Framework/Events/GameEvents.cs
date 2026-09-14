using UnityEngine;

namespace Game.Framework
{
    // ============================================================
    // 战斗相关事件
    // ============================================================

    /// <summary>
    /// 技能施放事件
    /// 由技能执行器发布，SkillEditor 运行时、特效、网络同步等均可监听
    /// </summary>
    public struct SkillCastEvent : IGameEvent
    {
        /// <summary>施放者的 Entity ID</summary>
        public int CasterId;
        /// <summary>技能 ID（对应配表）</summary>
        public int SkillId;
        /// <summary>目标位置（用于指向型技能）</summary>
        public Vector3 TargetPosition;
    }

    /// <summary>
    /// 伤害结算事件
    /// 由战斗系统发布，UI 血条、伤害飘字、音效均可监听
    /// </summary>
    public struct DamageDealtEvent : IGameEvent
    {
        public int AttackerId;
        public int TargetId;
        public float DamageAmount;
        public bool IsCritical;
    }

    /// <summary>
    /// 角色死亡事件
    /// </summary>
    public struct EntityDiedEvent : IGameEvent
    {
        public int EntityId;
        public Vector3 DeathPosition;
        /// <summary>是否为玩家角色</summary>
        public bool IsPlayer;
    }

    // ============================================================
    // 玩家状态相关事件
    // ============================================================

    /// <summary>
    /// 玩家属性变化事件（血量、蓝量、经验等）
    /// 由属性系统发布，UI 进度条监听
    /// </summary>
    public struct PlayerStatChangedEvent : IGameEvent
    {
        public int PlayerId;
        public StatType StatType;
        public float OldValue;
        public float NewValue;
        /// <summary>最大值（用于计算百分比）</summary>
        public float MaxValue;
    }

    public enum StatType
    {
        HP,
        MP,
        Stamina,
        Experience,
    }

    /// <summary>
    /// 队伍支援点数变化事件
    /// 当全队共享的支援点数发生消耗、恢复或重置时触发，供 UI (如 SkillKey 切人圆弧) 监听
    /// </summary>
    public struct AssistPointsChangedEvent : IGameEvent
    {
        public int CurrentPoints;
        public int MaxPoints;
    }

    // ============================================================
    // 网络相关事件
    // ============================================================

    /// <summary>
    /// 网络连接状态变化事件
    /// </summary>
    public struct NetworkStateChangedEvent : IGameEvent
    {
        public NetworkState State;
        /// <summary>若断开，原因描述</summary>
        public string Reason;
    }

    public enum NetworkState
    {
        Connecting,
        Connected,
        Disconnected,
        Reconnecting,
    }

    // ============================================================
    // UI 相关事件
    // ============================================================

    /// <summary>
    /// UI 面板打开/关闭请求事件
    /// 由业务逻辑发布，UIManager 监听
    /// </summary>
    public struct UIPanelRequestEvent : IGameEvent
    {
        /// <summary>面板标识符（对应 PanelType 枚举或字符串路径）</summary>
        public string PanelId;
        public UIPanelAction Action;
        public object Data;
    }

    public enum UIPanelAction { Open, Close, Toggle }
}
