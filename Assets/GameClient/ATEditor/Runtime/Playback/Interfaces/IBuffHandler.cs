using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// Buff 生命周期驱动接口。
    /// 状态系统/业务适配层实现此接口，用于接收 ATEditor 时间轴的 Buff 施加与清理指令。
    /// 遵循依赖倒置 (DIP) 原则，ATEditor 仅依赖此接口，绝不直接依赖具体的业务状态系统。
    /// </summary>
    public interface IBuffHandler
    {
        /// <summary>
        /// 当时间轴播放指针进入 Buff 片段时触发
        /// </summary>
        /// <param name="buffId">Buff 配置 ID（对应 Luban TbBuff）</param>
        /// <param name="clipId">时间轴片段唯一标识</param>
        /// <param name="mode">生命周期管理模式</param>
        void OnBuffEnter(int buffId, string clipId, BuffClipLifetimeMode mode);

        /// <summary>
        /// 当时间轴播放指针离开 Buff 片段、或动作被切招/受击打断时触发
        /// </summary>
        /// <param name="buffId">Buff 配置 ID（对应 Luban TbBuff）</param>
        /// <param name="clipId">时间轴片段唯一标识</param>
        void OnBuffExit(int buffId, string clipId);
    }
}
