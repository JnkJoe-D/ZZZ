namespace Game.Framework
{
    /// <summary>
    /// 日志标签常量定义。
    /// 将常用标签集中管理，避免硬编码字符串散落各处。
    /// 也支持直接传入任意字符串作为标签（不强制必须使用此处定义的常量）。
    /// </summary>
    public static class LogTags
    {
        // ── 框架层 ──

        /// <summary>框架核心</summary>
        public const string Framework = "Framework";
        /// <summary>游戏根入口 / 生命周期管理</summary>
        public const string GameRoot = "GameRoot";
        /// <summary>资源加载与管理</summary>
        public const string Resource = "Resource";
        /// <summary>场景加载与切换</summary>
        public const string Scene = "Scene";
        /// <summary>对象池管理</summary>
        public const string Pool = "Pool";
        /// <summary>有限状态机</summary>
        public const string FSM = "FSM";
        /// <summary>事件系统</summary>
        public const string Event = "Event";
        /// <summary>配置表加载</summary>
        public const string Config = "Config";
        /// <summary>音频管理</summary>
        public const string Audio = "Audio";

        // ── 网络层 ──

        /// <summary>网络管理总线</summary>
        public const string Network = "Network";
        /// <summary>TCP 通道</summary>
        public const string Tcp = "Tcp";
        /// <summary>UDP 通道</summary>
        public const string Udp = "Udp";
        /// <summary>心跳与重连</summary>
        public const string Heartbeat = "Heartbeat";

        // ── 玩法层 ──

        /// <summary>战斗系统</summary>
        public const string Combat = "Combat";
        /// <summary>动作时间轴</summary>
        public const string Action = "Action";
        /// <summary>输入系统</summary>
        public const string Input = "Input";
        /// <summary>技能系统</summary>
        public const string Skill = "Skill";
        /// <summary>Buff 系统</summary>
        public const string Buff = "Buff";
        /// <summary>AI / 行为树</summary>
        public const string AI = "AI";
        /// <summary>队伍管理</summary>
        public const string Team = "Team";
        /// <summary>怪物管理</summary>
        public const string Monster = "Monster";
        /// <summary>玩家管理</summary>
        public const string Player = "Player";

        // ── 表现层 ──

        /// <summary>UI 系统</summary>
        public const string UI = "UI";
        /// <summary>摄像机管理</summary>
        public const string Camera = "Camera";
        /// <summary>特效管理</summary>
        public const string VFX = "VFX";
        /// <summary>动画系统</summary>
        public const string Anim = "Anim";

        // ── 编辑器工具 ──

        /// <summary>动作时间轴编辑器</summary>
        public const string ATEditor = "ATEditor";
        /// <summary>行为树编辑器</summary>
        public const string BehaviorTree = "BehaviorTree";
    }
}
