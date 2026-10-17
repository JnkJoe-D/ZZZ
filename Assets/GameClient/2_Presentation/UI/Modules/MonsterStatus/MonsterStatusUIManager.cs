using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
using Game.GamePlay;
using Game.UI;

namespace Game.UI
{
    /// <summary>
    /// 怪物状态 UI 管理器（表现层专职管理器）。
    /// 
    /// 核心职责：
    ///   1. 统一维护怪物头顶血条/失衡 UI 的对象池，避免频繁 Instantiate/Destroy 开销；
    ///   2. 监听 MonsterManager 的生成 (MonsterSpawnedEvent) 与回收 (MonsterRecycledEvent) 领域事件；
    ///   3. 与 GamePlay 业务层严格解耦，单向实现血条面板的镜像创建、约束装配与归还生命周期闭环。
    /// </summary>
    public class MonsterStatusUIManager : Singleton<MonsterStatusUIManager>
    {
        private const string PrefabAssetPath = "Assets/Resources/Prefab/UI/PanelView/MonsterStatus/MonsterStatusPanel.prefab";
        private const string PrefabResourcePath = "Prefab/UI/PanelView/MonsterStatus/MonsterStatusPanel";

        // 当前正在场上展示的怪物血条映射
        private readonly Dictionary<MonsterEntity, MonsterStatusPanelModule> _activeModules = new();
        // 缓存各实例 GameObject 绑定的 Module（支持对象池复用时避免重复构建 Module）
        private readonly Dictionary<GameObject, MonsterStatusPanelModule> _instanceModuleMap = new();

        private GameObjectPool _panelPool;
        private Transform _poolRoot;
        private Transform _activeUIRoot;
        private bool _isInitialized;

        public IReadOnlyDictionary<MonsterEntity, MonsterStatusPanelModule> ActiveModules => _activeModules;

        public void Initialize()
        {
            if (_isInitialized) return;

            // 1. 初始化场景层级根节点
            if (_poolRoot == null)
            {
                var poolRootObj = new GameObject("[MonsterStatusUI_PoolRoot]");
                Object.DontDestroyOnLoad(poolRootObj);
                _poolRoot = poolRootObj.transform;
            }

            if (_activeUIRoot == null)
            {
                var uiRootObj = new GameObject("[MonsterStatusUI_ActiveRoot]");
                Object.DontDestroyOnLoad(uiRootObj);
                _activeUIRoot = uiRootObj.transform;
            }

            // 2. 加载预制体并初始化对象池
            GameObject prefab = LoadPrefab();
            if (prefab != null)
            {
                _panelPool = new GameObjectPool(prefab, _poolRoot, new GameObjectPool.Config { maxSize = 50, prewarmCount = 0 });
            }
            else
            {
                GLog.Warning(LogTags.UI, $"[MonsterStatusUIManager] 未找到预制体: {PrefabAssetPath}，将在首次生成时尝试延迟加载。");
            }

            // 3. 订阅业务层解耦事件
            EventCenter.Subscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            EventCenter.Subscribe<MonsterRecycledEvent>(OnMonsterRecycled);
            EventCenter.Subscribe<EntityDiedEvent>(OnEntityDied);

            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;

            EventCenter.Unsubscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            EventCenter.Unsubscribe<MonsterRecycledEvent>(OnMonsterRecycled);
            EventCenter.Unsubscribe<EntityDiedEvent>(OnEntityDied);

            // 清理所有活跃面板
            foreach (var kv in _activeModules)
            {
                var module = kv.Value;
                if (module != null)
                {
                    module.UnbindMonster();
                    module.Internal_Destroy();
                }
            }
            _activeModules.Clear();
            _instanceModuleMap.Clear();

            _panelPool?.Dispose();
            _panelPool = null;

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }

            if (_activeUIRoot != null)
            {
                Object.Destroy(_activeUIRoot.gameObject);
                _activeUIRoot = null;
            }

            _isInitialized = false;
        }

        private GameObject LoadPrefab()
        {
            GameObject prefab = null;

            if (ResourceManager.Instance != null)
            {
                prefab = ResourceManager.Instance.LoadAsset<GameObject>(PrefabAssetPath);
            }

            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(PrefabResourcePath);
            }

#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            }
#endif
            return prefab;
        }

        private void EnsurePoolReady()
        {
            if (_panelPool != null) return;

            GameObject prefab = LoadPrefab();
            if (prefab != null)
            {
                _panelPool = new GameObjectPool(prefab, _poolRoot, new GameObjectPool.Config { maxSize = 50, prewarmCount = 0 });
            }
            else
            {
                GLog.Error(LogTags.UI, $"[MonsterStatusUIManager] 无法初始化对象池，Prefab 缺失: {PrefabAssetPath}");
            }
        }

        // ================= 事件响应与同步 =================

        private void OnMonsterSpawned(MonsterSpawnedEvent evt)
        {
            if (evt.Monster == null) return;

            // 容错防重
            if (_activeModules.ContainsKey(evt.Monster))
            {
                RecyclePanelForMonster(evt.Monster);
            }

            EnsurePoolReady();
            if (_panelPool == null) return;

            // 1. 从对象池取出 UI 对象
            GameObject panelGo = _panelPool.Spawn(Vector3.zero, Quaternion.identity, _activeUIRoot);
            if (panelGo == null) return;

            // 2. 获取或创建绑定的 Module
            if (!_instanceModuleMap.TryGetValue(panelGo, out var module) || module == null)
            {
                var view = panelGo.GetComponent<MonsterStatusPanelView>();
                if (view == null)
                {
                    view = panelGo.AddComponent<MonsterStatusPanelView>();
                }

                module = new MonsterStatusPanelModule();
                module.Internal_Create(view);
                _instanceModuleMap[panelGo] = module;
            }

            // 3. 关联怪物并激活显示
            module.Internal_Show(null);
            module.BindMonster(evt.Monster);
            _activeModules[evt.Monster] = module;
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            // 怪物死亡瞬间立即隐藏血条，避免怪物播死亡动画时头顶依旧悬停血条
            if (evt.Victim is MonsterEntity monster && _activeModules.TryGetValue(monster, out var module))
            {
                module.Internal_Hide();
            }
        }

        private void OnMonsterRecycled(MonsterRecycledEvent evt)
        {
            if (evt.Monster == null) return;
            RecyclePanelForMonster(evt.Monster);
        }

        private void RecyclePanelForMonster(MonsterEntity monster)
        {
            if (!_activeModules.TryGetValue(monster, out var module)) return;

            _activeModules.Remove(monster);

            if (module != null)
            {
                module.UnbindMonster();
                module.Internal_Hide();

                if (module.View != null && _panelPool != null)
                {
                    _panelPool.Return(module.View.gameObject);
                }
            }
        }
    }
}
