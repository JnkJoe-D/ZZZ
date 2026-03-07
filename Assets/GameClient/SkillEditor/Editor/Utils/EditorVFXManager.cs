using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器预览 VFX 管理器
    /// 管理 VFX 预制体实例的对象池、播放和 ParticleSystem 采样
    /// </summary>
    public class EditorVFXManager
    {
        private static EditorVFXManager instance;
        public static EditorVFXManager Instance => instance ??= new EditorVFXManager();

        private GameObject vfxRoot;

        // 按预制体 InstanceID 分池
        private Dictionary<int, Queue<GameObject>> pools
            = new Dictionary<int, Queue<GameObject>>();

        // 活跃实例 → 原始预制体 InstanceID（归还时定位池）
        private Dictionary<GameObject, int> activeInstances
            = new Dictionary<GameObject, int>();

        /// <summary>
        /// 从池取或新建 VFX 实例
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">旋转</param>
        /// <returns>激活的实例</returns>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            EnsureRoot();
            int prefabId = prefab.GetInstanceID();
            GameObject inst;

            if (pools.TryGetValue(prefabId, out var pool) && pool.Count > 0)
            {
                inst = pool.Dequeue();
                // 池中实例可能已被销毁
                if (inst == null)
                {
                    inst = CreateInstance(prefab, position, rotation);
                }
                else
                {
                    inst.transform.SetPositionAndRotation(position, rotation);
                    inst.SetActive(true);
                }
            }
            else
            {
                inst = CreateInstance(prefab, position, rotation);
            }

            activeInstances[inst] = prefabId;
            RestartParticles(inst);
            return inst;
        }

        /// <summary>
        /// 将 ParticleSystem 采样到指定时间点（Seek 时使用）
        /// 先清除再从头模拟到目标时间
        /// </summary>
        /// <param name="instance">VFX 实例</param>
        /// <param name="time">目标时间（秒）</param>
        public void Sample(GameObject instance, float time)
        {
            if (instance == null) return;

            var particles = instance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 从 0 模拟到 time，withChildren=true, restart=true, fixedTimeStep=false
                ps.Simulate(time, true, true, false);
            }
        }

        /// <summary>
        /// 回收实例到池
        /// </summary>
        public void Return(GameObject instance)
        {
            if (instance == null) return;
            if (!activeInstances.TryGetValue(instance, out int prefabId)) return;

            StopParticles(instance);
            instance.SetActive(false);
            activeInstances.Remove(instance);

            if (!pools.ContainsKey(prefabId))
            {
                pools[prefabId] = new Queue<GameObject>();
            }
            pools[prefabId].Enqueue(instance);
        }

        /// <summary>
        /// 回收所有活跃实例
        /// </summary>
        public void ReturnAll()
        {
            var instances = new List<GameObject>(activeInstances.Keys);
            foreach (var inst in instances)
            {
                Return(inst);
            }
        }

        /// <summary>
        /// 销毁所有实例和池
        /// </summary>
        public void Dispose()
        {
            if (vfxRoot != null)
            {
                Object.DestroyImmediate(vfxRoot);
                vfxRoot = null;
            }
            pools.Clear();
            activeInstances.Clear();
            instance = null;
        }

        // ─── 私有方法 ───

        public GameObject VfxRoot => vfxRoot;

        private void EnsureRoot()
        {
            if (vfxRoot == null)
            {
                vfxRoot = new GameObject("[EditorVFXPreview]");
                // 改为 DontSave，这样可以在 Hierarchy 中看到，但不会保存到场景
                vfxRoot.hideFlags = HideFlags.DontSave;
            }
        }

        private GameObject CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var inst = Object.Instantiate(prefab, position, rotation, vfxRoot.transform);
            inst.hideFlags = HideFlags.DontSave;
            return inst;
        }

        private void RestartParticles(GameObject inst)
        {
            var particles = inst.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Clear(true);
                // 停止播放，完全由 Timeline 的 Sample 方法驱动 Simulate
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void StopParticles(GameObject inst)
        {
            var particles = inst.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
