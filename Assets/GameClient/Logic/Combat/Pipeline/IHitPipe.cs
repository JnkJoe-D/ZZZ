using System;

namespace Game.Logic.Combat.Pipeline
{
    /// <summary>
    /// 管道执行标志位（支持位运算与状态快速判断）
    /// </summary>
    [Flags]
    public enum HitResultFlags
    {
        None            = 0,
        Protected       = 1 << 0, // 受击保护/内置CD拦截
        Invincible      = 1 << 1, // 无敌状态拦截
        Parried         = 1 << 2, // 招架成功（弹刀）
        Damaged         = 1 << 3, // 造成有效伤害
        Killed          = 1 << 4, // 致死
        SuperArmor      = 1 << 5, // 处于霸体状态
        Interrupted     = 1 << 6, // 动作/行为被打断
        HitStopApplied  = 1 << 7, // 顿帧已施加
    }

    /// <summary>
    /// 命中流水线过滤器标准接口
    /// </summary>
    public interface IHitPipe
    {
        /// <summary>过滤器名称（用于日志与 Profiler 采样）</summary>
        string PipeName { get; }

        /// <summary>过滤器执行优先级（值越小越先执行）</summary>
        int Priority { get; }

        /// <summary>
        /// 执行过滤器逻辑
        /// </summary>
        /// <param name="ctx">命中上下文数据总线</param>
        void Process(HitPipelineContext ctx);
    }
}
