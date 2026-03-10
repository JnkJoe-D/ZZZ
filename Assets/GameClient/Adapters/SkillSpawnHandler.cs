using System.Collections.Generic;
using SkillEditor;
using UnityEngine;
using Game.Pool;

namespace Game.Adapters
{
    /// <summary>
    /// Spawn 处理器
    /// 实现 ISkillSpawnHandler 接口，使用 GlobalPoolManager 管理 Spawn 对象
    /// </summary>
    public class SkillSpawnHandler : ISkillSpawnHandler
    {
        public ISkillProjectileHandler Spawn(SpawnData data)
        {
            var obj = SpawnObject(data.configPrefab, data.position, data.rotation, data.detach, data.parent);
            if (obj == null) return null;
            ISkillProjectileHandler sp = obj.GetComponent<SkillProjectileHandler>() ?? obj.AddComponent<SkillProjectileHandler>();
            return sp;
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, bool detach, Transform parent)
        {
            if (prefab == null) return null;

            // 通过 GlobalPoolManager 统一获取
            var instance = GlobalPoolManager.Spawn(prefab, position, rotation);

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            if (!detach && parent != null)
            {
                instance.transform.SetParent(parent);
            }
            else
            {
                instance.transform.SetParent(null);
            }

            return instance;
        }

        public void DestroySpawnedObject(ISkillProjectileHandler projectile)
        {
            if (projectile is MonoBehaviour mono)
            {
                GameObject obj = mono.gameObject;
                obj.SetActive(false);
                obj.transform.SetParent(null); // 回池时脱离父节点，防止被带着跑

                // 通过 GlobalPoolManager 统一归还
                GlobalPoolManager.Return(obj);
            }
        }
    }
}