using System.Collections;
using UnityEngine;
using Game.Pool;

namespace Game.VFX
{
    /// <summary>
    /// VFX 管理器（MonoSingleton）。
    /// 封装 GlobalPoolManager 的 VFX 生成/归还，并提供协程驱动的延迟归还。
    /// </summary>
    public class VFXManager : Framework.MonoSingleton<VFXManager>
    {
        /// <summary>
        /// 从对象池生成 VFX 实例
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            return GlobalPoolManager.Spawn(prefab, position, rotation, parent);
        }

        /// <summary>
        /// 立即归还 VFX 实例到对象池
        /// </summary>
        public void Return(GameObject instance)
        {
            if (instance == null) return;
            GlobalPoolManager.Return(instance);
        }

        /// <summary>
        /// 延迟归还 VFX 实例到对象池
        /// </summary>
        public void ReturnDelay(GameObject instance, float delay)
        {
            if (instance == null) return;
            if (delay <= 0f)
            {
                Return(instance);
                return;
            }
            StartCoroutine(DelayReturnCoroutine(instance, delay));
        }

        private IEnumerator DelayReturnCoroutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instance != null)
            {
                Return(instance);
            }
        }

        /// <summary>
        /// 持续检测实例上所有粒子系统是否播放完毕，全部完成后自动归还。
        /// 适用于不确定粒子时长或有多个子粒子系统的特效。
        /// </summary>
        public void ReturnWhenDone(GameObject instance)
        {
            if (instance == null) return;
            StartCoroutine(WaitParticlesDoneCoroutine(instance));
        }

        private IEnumerator WaitParticlesDoneCoroutine(GameObject instance)
        {
            if (instance == null) yield break;

            var particles = instance.GetComponentsInChildren<ParticleSystem>(true);

            // 没有粒子系统则直接归还
            if (particles == null || particles.Length == 0)
            {
                Return(instance);
                yield break;
            }

            // 等待所有粒子系统播放结束
            while (instance != null && instance.activeInHierarchy)
            {
                bool allStopped = true;
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] != null && particles[i].IsAlive(true))
                    {
                        allStopped = false;
                        break;
                    }
                }

                if (allStopped) break;
                yield return null;
            }

            if (instance != null)
            {
                Return(instance);
            }
        }
    }
}
