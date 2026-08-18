using UnityEngine;
using Game.Pool;

namespace Game.Logic
{
    /// <summary>
    /// 怪物专属对象池。继承自底层的 GameObjectPool 以复用基于预制体的池化逻辑。
    /// 负责怪物实例的创建、复用、回收以及强制清理状态。
    /// </summary>
    public class MonsterPool : GameObjectPool
    {
        public MonsterPool(GameObject prefab, Transform poolRoot = null, Config config = default) 
            : base(prefab, poolRoot, config)
        {
            OnSpawn += HandleSpawn;
            OnReturn += HandleReturn;
        }

        /// <summary>
        /// 从池中取出怪物实体
        /// </summary>
        public MonsterEntity SpawnMonster(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var go = Spawn(position, rotation, parent);
            if (go == null) return null;
            
            return go.GetComponent<MonsterEntity>();
        }

        /// <summary>
        /// 将怪物实体安全归还给对象池
        /// </summary>
        public void ReturnMonster(MonsterEntity entity)
        {
            if (entity != null && entity.gameObject != null)
            {
                Return(entity.gameObject);
            }
        }

        private void HandleSpawn(GameObject go)
        {
            // 取出时，GameObjectPool 已经自动调用了 SetActive(true)。
            // 实体的黑盒重置和初始化工作交由 Manager 调用的 entity.Init() 完成。
        }

        private void HandleReturn(GameObject go)
        {
            // 回收时，GameObjectPool 稍后会自动调用 SetActive(false)。
            // 在此之前，强制中断可能正在播放的动作，清理行为树状态，防止脏数据残留到下一次复用。
            var entity = go.GetComponent<MonsterEntity>();
            if (entity != null)
            {
                if (entity.ActionPlayer != null)
                {
                    entity.ActionPlayer.StopAction();
                }
                
                if (entity.BTRunner != null)
                {
                    entity.BTRunner.StopTree();
                }
            }
        }
    }
}
