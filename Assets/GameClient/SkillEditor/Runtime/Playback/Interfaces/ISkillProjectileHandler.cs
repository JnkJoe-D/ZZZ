using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能投射物接口
    /// 挂载在投射物/召唤物 Prefab 上的核心组件，接管生成后的生命周期、轨道飞行、碰撞检测。
    /// 可以接受来自外部服务（如 ProjectileManager）的Tick驱动，也可以自己在内部 Update。
    /// </summary>
    public interface ISkillProjectileHandler
    {
        /// <summary>
        /// 投射物被生成时的初始化数据灌入
        /// </summary>
        /// <param name="data">包含位置、旋转、标签等在内的生成参数包</param>
        /// <param name="handler">生成器接口引用（用于后续可能的销毁/回调反馈）</param>
        void Initialize(SpawnData data, ISkillSpawnHandler handler);

        /// <summary>
        /// 逻辑销毁回调（如：停止粒子、音效淡出、逻辑停更等）
        /// 由外部进程或 Recycle 调用，不负责真实的入池/销毁。
        /// </summary>
        void Terminate();

        /// <summary>
        /// 执行真实的对象回收/入池。
        /// 内部应先触发 Terminate()。
        /// </summary>
        void Recycle();
    }
}
