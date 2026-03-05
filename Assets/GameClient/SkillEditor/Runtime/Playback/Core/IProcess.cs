using System;

namespace SkillEditor
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
        /// 时间指针离开片段区间时调用，用于回收实例、重置临时状态
        /// </summary>
        void OnExit();

        /// <summary>
        /// Runner.Stop() 或打断时调用，用于释放进程内部状态
        /// </summary>
        void OnDisable();
    }
}
