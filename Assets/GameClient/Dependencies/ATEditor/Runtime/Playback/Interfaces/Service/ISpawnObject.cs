using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 生成物实体抽象组件接口 (身份识别与核心职责契约)
    /// 挂载在投射物/召唤物/机关 Prefab 上的核心组件，接管生成后的生命周期、移动轨迹与碰撞攻击检测。
    /// </summary>
    public interface ISpawnObject : IService
    {
        /// <summary>生成物宿主对象</summary>
        GameObject GameObject { get; }

        /// <summary>是否正处于活跃生命周期</summary>
        bool IsActive { get; }

        /// <summary>
        /// 初始化生成物，灌入完整运行期参数包与所有者上下文
        /// </summary>
        /// <param name="data">包含生成配置、移动参数、生命周期与可选攻击策略的运行时数据</param>
        /// <param name="handler">生成器管理器引用 (用于回调回收)</param>
        void Initialize(SpawnData data, ISpawnHandler handler);

        /// <summary>
        /// 逻辑终止表现（如停止粒子发射、播放淡出音效、关闭碰撞体）
        /// </summary>
        void Terminate();

        /// <summary>
        /// 彻底回收（入池或销毁）
        /// </summary>
        void Recycle();
    }
}
