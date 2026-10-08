using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// ATEditor Spawn 业务适配器 (Adapter)
    /// 实现 ISpawnHandler 接口，作为 ATEditor 时间轴播放层与集中管理器 SpawnObjectManager 之间的交互桥梁。
    /// 不直接管理对象池与生成物实例，统一转交 SpawnObjectManager 进行大盘调度与框架对象池存取。
    /// </summary>
    public class ATSpawnHandler : ISpawnHandler
    {
        private readonly CharacterEntity _owner;

        public ATSpawnHandler(CharacterEntity owner = null)
        {
            _owner = owner;
        }

        public ISpawnObject Spawn(SpawnData data)
        {
            return SpawnObjectManager.Instance.Spawn(data, _owner);
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, bool detach, Transform parent)
        {
            return SpawnObjectManager.Instance.SpawnObject(prefab, position, rotation, detach, parent);
        }

        public void DestroySpawnedObject(ISpawnObject projectile)
        {
            SpawnObjectManager.Instance.Recycle(projectile);
        }
    }
}