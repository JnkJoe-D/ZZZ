using ATEditor;
using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// Spawn 处理器
    /// 实现 ISkillSpawnHandler 接口，使用 GlobalPoolManager 管理 Spawn 对象
    /// </summary>
    public class ATSpawnHandler : ISpawnHandler
    {
        public IProjectileHandler Spawn(SpawnData data)
        {
            var obj = SpawnObject(data.configPrefab, data.position, data.rotation, data.detach, data.parent);
            if (obj == null) return null;
            IProjectileHandler sp = obj.GetComponent<ATProjectileHandler>() ?? obj.AddComponent<ATProjectileHandler>();
            return sp;
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, bool detach, Transform parent)
        {
            if (prefab == null) return null;

            // 通过 GlobalPoolManager 统一获取并设置父节点/场景归属
            Transform targetParent = (!detach && parent != null) ? parent : null;
            var instance = GlobalPoolManager.Spawn(prefab, position, rotation, targetParent);
            instance.SetActive(true);

            return instance;
        }

        public void DestroySpawnedObject(IProjectileHandler projectile)
        {
            if (projectile is MonoBehaviour mono)
            {
                GameObject obj = mono.gameObject;
                if (obj == null || !obj.scene.isLoaded) return;

                obj.SetActive(false);

                // 通过 GlobalPoolManager 统一归还，由池内部统一安全挂回 _poolRoot
                GlobalPoolManager.Return(obj);
            }
        }
    }
}