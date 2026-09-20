namespace Game.GamePlay
{
    /// <summary>
    /// 切人类型定义（覆盖绝区零异构切人场景）
    /// </summary>
    public enum SwitchType
    {
        /// <summary> 普通切人（主动按键换人，双人同屏打完退场） </summary>
        NormalSwitch = 0,

        /// <summary> 招架支援（怪物黄光攻击，前台换人举刀格挡弹刀） </summary>
        ParryAid = 10,

        /// <summary> 闪避支援（怪物红光攻击，前台高速突进极限闪避并触发全局子弹时间） </summary>
        EvasionAid = 20,

        /// <summary> 连携技切人（怪物失衡 Daze 满，QTE 选择队友影视级连携大招） </summary>
        ChainAttack = 30,

        /// <summary> 快速救援支援（主控角色被击飞/倒地硬直时救场切入） </summary>
        QuickAid = 40,

        /// <summary> 避险垫步切人（预警时点数不足，切入角色出场播放闪避_后垫步逃生，不进招架） </summary>
        FallbackEvasion = 45
    }

    /// <summary>
    /// 切出角色退场生命周期策略
    /// </summary>
    public enum OutgoingExitPolicy
    {
        /// <summary> 即时模式：切入瞬间立即关闭碰撞、隐藏渲染、转入 Standby 待机（如招架/闪避支援） </summary>
        Immediate = 0,

        /// <summary> 动作自动托管模式：由路由根据实时条件切换动作 </summary>
        AutoByRoute = 10,
    }

    /// <summary>
    /// 相机机位交接策略
    /// </summary>
    public enum CameraSwitchMode
    {
        /// <summary> 共享虚拟相机平滑对焦跟随 </summary>
        SmoothFollow = 0,

        /// <summary> 即时瞬切对齐 </summary>
        InstantSnap = 10,

        /// <summary> 连携技影视级特写机位 </summary>
        CinematicQTE = 20
    }

    /// <summary>
    /// 切人流水线过滤器标准接口
    /// </summary>
    public interface ISwitchPipe
    {
        /// <summary> 过滤器名称 </summary>
        string PipeName { get; }

        /// <summary> 执行优先级（越小越早执行） </summary>
        int Priority { get; }

        /// <summary> 核心处理方法 </summary>
        void Process(SwitchPipelineContext ctx);
    }
}
