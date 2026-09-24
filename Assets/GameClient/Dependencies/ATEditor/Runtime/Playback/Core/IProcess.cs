using System;

namespace ATEditor
{
    /// <summary>
    /// 播放进程接口，定义片段执行的五阶段生命周期
    /// </summary>
    public interface IProcess
    {
        /// <summary>
        /// 初始化，注入片段数据和上下文
        /// </summary>
        void Initialize(ClipBase clipData, ProcessContext context);

        /// <summary>
        /// 重置状态（对象池复用前调用）
        /// </summary>
        void Reset();

        /// <summary>
        /// Runner.Play() 后立即调用，用于缓存组件引用、注册系统级清理
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 时间指针进入片段区间时调用，用于触发播放、生成实例
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 每帧调用（在片段区间内），用于更新状态
        /// </summary>
        /// <param name="currentTime">当前播放时间</param>
        /// <param name="deltaTime">帧间隔</param>
        void OnUpdate(float currentTime, float deltaTime);
        /// <summary>
        /// Runner.Pause() 时调用，用于暂停状态
        /// </summary>
        void OnPause();
        /// <summary>
        /// Runner.Resume() 时调用，用于恢复状态
        /// </summary>
        void OnResume();

        /// <summary>
        /// Runner 执行时间跳跃（Seek）时对于保持激活的片段调用，用于强制同步时间
        /// </summary>
        void OnSeek(float targetTime);

        /// <summary>
        /// 【逻辑边界】时间指针按正常步进（正向/反向）离开片段区间时调用。
        /// 用于处理自然转换逻辑
        /// 注意：在被打断（Stop/Interrupt）时不会触发此方法。
        /// </summary>
        void OnExit();

        /// <summary>
        /// 【中断边界】在被新技能抢占打断（Interrupt）或外部强制终止（Stop）时调用。
        /// 用于处理非正常中断逻辑、中止未播完的表现、打断清理。
        /// 注意：在自然正常播完时绝对不触发此方法。
        /// </summary>
        void OnStop();

        /// <summary>
        /// 【物理边界】宿主组件销毁 / 引擎生命周期注销时调用。
        /// 用于彻底释放托管硬资源、解除外部引擎级长连接引用。
        /// </summary>
        void OnDisable();
    }
}
