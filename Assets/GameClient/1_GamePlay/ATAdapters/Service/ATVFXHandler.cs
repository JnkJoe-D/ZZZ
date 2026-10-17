using UnityEngine;
using ATEditor;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 运行时 VFX 适配器。
    /// 实现 ISkillVFXHandler 接口，将请求转发给 VFXManager 单例。
    /// </summary>
    public class ATVFXHandler : Singleton<ATVFXHandler>,IVFXHandler
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

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Vector3 scale)
        {
            var instance = Spawn(prefab, position, rotation, parent);
            if (instance != null) instance.transform.localScale = scale;
            return instance;
        }

        public void SetTime(GameObject instance, float time)
        {
            // 外部自驱动，若需强制 Seek 快进可扩展
        }

        public void ReleaseWhenFinished(GameObject instance, bool stopEmission)
        {
            if (instance == null) return;
            if (stopEmission)
            {
                var systems = instance.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] != null)
                    {
                        var em = systems[i].emission;
                        em.enabled = false;
                        systems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
        }
    }
}