using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// VFX 对象池服务接口
    /// 由外部层（GameClient）实现，通过 IServiceFactory 注入到 ProcessContext
    /// 用于在 RuntimeVFXProcess 中解耦对具体对象池实现的依赖
    /// </summary>
    public interface ISkillVFXHandler
    {
        /// <summary>
        /// 从池中生成 VFX 实例
        /// </summary>
        /// <param name="prefab">VFX 预制体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点（null 则无父节点）</param>
        /// <returns>VFX 实例</returns>
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// 归还 VFX 实例到池中
        /// </summary>
        /// <param name="instance">要归还的实例</param>
        void Return(GameObject instance);
        /// <summary>
        /// 延迟归还 VFX 实例到池中
        /// </summary>
        /// <param name="inst">VFX 实例</param>
        /// <param name="delay">延迟时间</param>
        void ReturnVFXDelay(GameObject inst, float delay);
    }
}
