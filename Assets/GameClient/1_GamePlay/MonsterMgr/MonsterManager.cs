using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 全局怪物管理器。
    /// 负责统筹怪物的异步预加载、实例化（对象池装配）、索敌雷达分配以及生命周期回收。
    /// </summary>
    public class MonsterManager : Singleton<MonsterManager>
    {
        // ================= 核心依赖与缓存 =================
        
        // 每个预制体实例 ID 对应一个独立的专属 MonsterPool
        private readonly Dictionary<int, MonsterPool> _pools = new Dictionary<int, MonsterPool>();
        
        // 当前场上处于活跃状态的怪物列表
        private readonly List<MonsterEntity> _activeMonsters = new List<MonsterEntity>();
        public IReadOnlyList<MonsterEntity> ActiveMonsters => _activeMonsters;
        
        // 待延迟入池的怪物倒计时字典（由受控逻辑 Tick 驱动）
        private readonly Dictionary<MonsterEntity, float> _pendingRecycleMonsters = new Dictionary<MonsterEntity, float>();
        private readonly List<MonsterEntity> _recycleBuffer = new List<MonsterEntity>();
        private readonly List<MonsterEntity> _recycleKeysBuffer = new List<MonsterEntity>();

        // 对象池在场景中的根节点，用于收纳失活的怪物
        private Transform _poolRoot;

        // ================= 生命周期 =================

        public void Initialize()
        {
            if (_poolRoot == null)
            {
                var rootObj = new GameObject("[MonsterManager_PoolRoot]");
                Object.DontDestroyOnLoad(rootObj);
                _poolRoot = rootObj.transform;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick += OnGameplayLogicTick;
            }

            EventCenter.Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        public void Shutdown()
        {
            EventCenter.Unsubscribe<EntityDiedEvent>(OnEntityDied);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnGameplayLogicTick -= OnGameplayLogicTick;
            }

            _pendingRecycleMonsters.Clear();
            _recycleBuffer.Clear();

            // 退出清理时直接销毁活跃怪物，避免先重挂到 _poolRoot 导致同帧双重销毁竞争
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _activeMonsters[i];
                if (monster != null && monster.gameObject != null)
                {
                    Object.Destroy(monster.gameObject);
                }
            }
            _activeMonsters.Clear();
            
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }
            _pools.Clear();

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (evt.Victim is MonsterEntity monster && _activeMonsters.Contains(monster))
            {
                float delay = (monster.LifecycleComponent as MonsterLifecycleComponent)?.recycleDelay ?? 0.5f;
                if (delay <= 0f)
                {
                    RecycleMonster(monster);
                }
                else
                {
                    _pendingRecycleMonsters[monster] = delay;
                }
            }
        }

        private void OnGameplayLogicTick(float logicDeltaTime)
        {
            for (int i = 0; i < _activeMonsters.Count; i++)
            {
                var monster = _activeMonsters[i];
                if (monster != null && monster.gameObject != null && monster.gameObject.activeInHierarchy)
                {
                    monster.OnLogicTick(logicDeltaTime);
                }
            }

            // 统筹处理死亡怪物的延时入池倒计时（受受控玩法时钟步长驱动，暂停或子弹时间下完全同步）
            if (_pendingRecycleMonsters.Count > 0)
            {
                _recycleBuffer.Clear();
                _recycleKeysBuffer.Clear();
                foreach (var k in _pendingRecycleMonsters.Keys)
                {
                    _recycleKeysBuffer.Add(k);
                }
                for (int i = 0; i < _recycleKeysBuffer.Count; i++)
                {
                    var m = _recycleKeysBuffer[i];
                    if (m == null || m.gameObject == null)
                    {
                        _recycleBuffer.Add(m);
                        continue;
                    }

                    float remaining = _pendingRecycleMonsters[m] - logicDeltaTime;
                    if (remaining <= 0f)
                    {
                        _recycleBuffer.Add(m);
                    }
                    else
                    {
                        _pendingRecycleMonsters[m] = remaining;
                    }
                }

                for (int i = 0; i < _recycleBuffer.Count; i++)
                {
                    var m = _recycleBuffer[i];
                    _pendingRecycleMonsters.Remove(m);
                    if (m != null)
                    {
                        RecycleMonster(m);
                    }
                }
                _recycleBuffer.Clear();
            }
        }

        // ================= 1. 资源统筹 =================

        /// <summary>
        /// 战前调用，向 ActionManager 和 ResourceManager 下达并发预加载指令。
        /// </summary>
        public async Task PreloadMonstersAsync(List<MonsterConfigAsset> monsterConfigs)
        {
            if (monsterConfigs == null || monsterConfigs.Count == 0) return;

            var loadTasks = new List<Task>();

            foreach (var config in monsterConfigs)
            {
                if (config == null) continue;
                
                // 1. 动作与特效资源预加载
                loadTasks.Add(ActionManager.Instance.PreloadCharacterActionsAsync(config));
                
                // 2. 如果未来使用 Addressables 或 AssetBundle 加载预制体，也在这里异步 Load
                // 目前因为是直接引用，不需要走异步，但结构上预留在此处
            }

            await Task.WhenAll(loadTasks);
        }

        // ================= 2. 业务装配 =================

        /// <summary>
        /// 生成一只怪物。从专属怪物池中获取实例，然后交接给实体自行黑盒 Init。
        /// </summary>
        public async Task<MonsterEntity> SpawnMonsterAsync(MonsterConfigAsset config, Vector3 spawnPos, Quaternion spawnRot)
        {
            if (config == null || config.Prefab == null)
            {
                GLog.Error(LogTags.Monster, "Spawn failed: Config or Prefab is null.");
                return null;
            }

            // 获取或创建专属对象池
            int prefabId = config.Prefab.GetInstanceID();
            if (!_pools.TryGetValue(prefabId, out var pool))
            {
                pool = new MonsterPool(config.Prefab, _poolRoot, new Game.Framework.GameObjectPool.Config { maxSize = 100, prewarmCount = 0 });
                _pools[prefabId] = pool;
            }

            // 1. 从对象池取出物理实例
            MonsterEntity entity = pool.SpawnMonster(spawnPos, spawnRot);
            if (entity == null)
            {
                GLog.Error(LogTags.Monster, "Spawn failed: Prefab lacks MonsterEntity component.");
                return null;
            }

            // 2. 执行黑盒初始化：实体内部自行完成专属雷达创建、黑板注册、树的重启等操作。
            entity.Init(config);

            // 3. 显式时钟生命周期装配：出池挂载至怪物时钟通道
            TimeManager.Instance?.RegisterMonsterClock(entity.Clock);

            _activeMonsters.Add(entity);

            // 兼容之前异步返回的设计需求（方便以后做按帧拆分出生特效等）
            await Task.Yield();

            return entity;
        }

        // ================= 3. 业务回收 =================

        /// <summary>
        /// 处理怪物死亡/离开战场后的收尾与入池。
        /// </summary>
        public void RecycleMonster(MonsterEntity monster)
        {
            if (monster == null) return;
            
            _pendingRecycleMonsters.Remove(monster);
            monster.Clock?.Detach();
            _activeMonsters.Remove(monster);

            if (monster.Config != null && monster.Config.Prefab != null)
            {
                int prefabId = monster.Config.Prefab.GetInstanceID();
                if (_pools.TryGetValue(prefabId, out var pool))
                {
                    pool.ReturnMonster(monster);
                    return;
                }
            }

            // 如果发生意外丢失池映射，降级为暴力销毁
            Object.Destroy(monster.gameObject);
        }

        /// <summary>
        /// 强制清除场上所有怪物，通常用于波次彻底结束或退出副本。
        /// </summary>
        public void ClearAllMonsters()
        {
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _activeMonsters[i];
                RecycleMonster(monster);
            }
            _activeMonsters.Clear();
        }
    }
}
