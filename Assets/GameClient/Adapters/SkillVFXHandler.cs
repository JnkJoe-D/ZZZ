using UnityEngine;
using SkillEditor;
using Game.VFX;
using Game.Framework;

namespace Game.Adapters
{
    /// <summary>
    /// 运行时 VFX 适配器。
    /// 实现 ISkillVFXHandler 接口，将请求转发给 VFXManager 单例。
    /// </summary>
    public class SkillVFXHandler : Singleton<SkillVFXHandler>,ISkillVFXHandler
    {
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (VFXManager.Instance == null) return null;
            return VFXManager.Instance.Spawn(prefab, position, rotation, parent);
        }

        public void Return(GameObject instance)
        {
            VFXManager.Instance?.Return(instance);
        }

        public void ReturnVFXDelay(GameObject inst, float delay)
        {
            VFXManager.Instance?.ReturnDelay(inst, delay);
        }
    }
}