using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 全局对象池管理器
    /// 统一注册、获取和管理所有 GameObjectPool、ComponentPool 与 ObjectPool 实例
    /// 提供按预制体获取 GameObject 池、按 Key 获取组件池和纯 C# 对象池的 API
    /// </summary>
    public static class GlobalPoolManager
    {
        // ── GameObject 池注册表 ──
        // Key: Prefab InstanceID → GameObjectPool
        private static readonly Dictionary<int, GameObjectPool> _gameObjectPools
            = new Dictionary<int, GameObjectPool>();

        // ── 活跃实例反查表 ──
        // Key: 活跃 GameObject InstanceID → Prefab InstanceID
        private static readonly Dictionary<int, int> _activeInstances
            = new Dictionary<int, int>();

        // ── Component 池注册表 ──
        // Key: 自定义字符串 Key → IDisposable (实际类型为 ComponentPool<T>)
        private static readonly Dictionary<string, IDisposable> _componentPools
            = new Dictionary<string, IDisposable>();

        // ── 纯 C# 对象池注册表 ──
        // Key: 自定义字符串 Key → IDisposable (实际类型为 ObjectPool<T>)
        private static readonly Dictionary<string, IDisposable> _objectPools
            = new Dictionary<string, IDisposable>();

        // ── 全局池根节点 ──
        private static Transform _globalRoot;

        // ── 是否已初始化 ──
        private static bool _initialized;

        // ────────────────────────────
        // 初始化
        // ────────────────────────────

        /// <summary>
        /// 初始化全局池管理器
        /// 在游戏启动或场景加载时调用
        /// 重复调用是安全的
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_globalRoot == null)
            {
                var rootObj = new GameObject("[GlobalPoolManager]");
                UnityEngine.Object.DontDestroyOnLoad(rootObj);
                _globalRoot = rootObj.transform;
            }
        }

        /// <summary>
        /// 确保已初始化（内部惰性调用）
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized) Initialize();
        }

        // ────────────────────────────
        // GameObject 池 API
        // ────────────────────────────

        /// <summary>
        /// 获取或创建指定预制体的 GameObjectPool
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置（仅在首次创建时生效）</param>
        /// <returns>对应的 GameObjectPool</returns>
        public static GameObjectPool GetPool(GameObject prefab, GameObjectPool.Config config = default)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            EnsureInitialized();

            int prefabId = prefab.GetInstanceID();
            if (_gameObjectPools.TryGetValue(prefabId, out var pool))
            {
                return pool;
            }

            // 为该预制体创建专属的父节点
            var poolParent = new GameObject($"Pool_{prefab.name}");
            poolParent.transform.SetParent(_globalRoot);

            pool = new GameObjectPool(prefab, poolParent.transform, config);
            _gameObjectPools[prefabId] = pool;

            return pool;
        }

        /// <summary>
        /// 便捷方法：从预制体池中生成 GameObject
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            var pool = GetPool(prefab);
            var instance = pool.Spawn(position, rotation, parent);

            // 记录反查信息
            if (instance != null)
            {
                _activeInstances[instance.GetInstanceID()] = prefab.GetInstanceID();
            }

            return instance;
        }

        /// <summary>
        /// 便捷方法：归还 GameObject 到对应的预制体池
        /// 通过反查表 O(1) 查找所属池
        /// </summary>
        public static void Return(GameObject instance)
        {
            if (instance == null) return;

            int instanceId = instance.GetInstanceID();

            if (_activeInstances.TryGetValue(instanceId, out int prefabId))
            {
                _activeInstances.Remove(instanceId);

                if (_gameObjectPools.TryGetValue(prefabId, out var pool))
                {
                    pool.Return(instance);
                    return;
                }
            }

            // 不属于任何池，直接销毁
            GLog.Warning(LogTags.Pool, $"归还的对象不属于任何已注册的池: {instance.name}");
            UnityEngine.Object.Destroy(instance);
        }

        // ────────────────────────────
        // Component 池 API
        // ────────────────────────────

        /// <summary>
        /// 注册组件池
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="key">池唯一标识</param>
        /// <param name="pool">组件池实例</param>
        public static void RegisterComponentPool<T>(string key, ComponentPool<T> pool) where T : Component
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            EnsureInitialized();
            _componentPools[key] = pool;
        }

        /// <summary>
        /// 获取已注册的组件池
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="key">池唯一标识</param>
        /// <returns>组件池实例，未注册则返回 null</returns>
        public static ComponentPool<T> GetComponentPool<T>(string key) where T : Component
        {
            EnsureInitialized();

            if (_componentPools.TryGetValue(key, out var pool))
            {
                return pool as ComponentPool<T>;
            }

            return null;
        }

        // ────────────────────────────
        // 纯 C# 对象池 API
        // ────────────────────────────

        /// <summary>
        /// 注册纯 C# 对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">池唯一标识</param>
        /// <param name="pool">对象池实例</param>
        public static void RegisterObjectPool<T>(string key, ObjectPool<T> pool) where T : class
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            EnsureInitialized();
            _objectPools[key] = pool;
        }

        /// <summary>
        /// 获取已注册的纯 C# 对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">池唯一标识</param>
        /// <returns>对象池实例，未注册则返回 null</returns>
        public static ObjectPool<T> GetObjectPool<T>(string key) where T : class
        {
            EnsureInitialized();

            if (_objectPools.TryGetValue(key, out var pool))
            {
                return pool as ObjectPool<T>;
            }

            return null;
        }

        /// <summary>
        /// 获取或创建纯 C# 对象池
        /// 如果指定 key 的池不存在，则使用提供的工厂方法和配置自动创建并注册
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">池唯一标识</param>
        /// <param name="createFunc">对象创建工厂方法</param>
        /// <param name="config">池配置（仅在首次创建时生效）</param>
        /// <returns>对象池实例</returns>
        public static ObjectPool<T> GetOrCreateObjectPool<T>(string key, Func<T> createFunc,
            ObjectPool<T>.Config config = default) where T : class
        {
            var existing = GetObjectPool<T>(key);
            if (existing != null) return existing;

            var pool = new ObjectPool<T>(createFunc, config);
            RegisterObjectPool(key, pool);
            return pool;
        }

        // ────────────────────────────
        // 生命周期管理
        // ────────────────────────────

        /// <summary>
        /// 清空所有池中的空闲对象（不影响活跃对象）
        /// 适合在场景切换时调用
        /// </summary>
        public static void ClearAll()
        {
            foreach (var kvp in _gameObjectPools)
            {
                kvp.Value.Clear();
            }

            foreach (var kvp in _componentPools)
            {
                if (kvp.Value is IClearable clearable)
                {
                    clearable.Clear();
                }
            }

            foreach (var kvp in _objectPools)
            {
                if (kvp.Value is IClearable clearable)
                {
                    clearable.Clear();
                }
            }
        }

        /// <summary>
        /// 销毁所有池及其管理的对象
        /// 适合在应用退出或完全重置时调用
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var kvp in _gameObjectPools)
            {
                kvp.Value.Dispose();
            }
            _gameObjectPools.Clear();
            _activeInstances.Clear();

            foreach (var kvp in _componentPools)
            {
                kvp.Value.Dispose();
            }
            _componentPools.Clear();

            foreach (var kvp in _objectPools)
            {
                kvp.Value.Dispose();
            }
            _objectPools.Clear();

            if (_globalRoot != null)
            {
                UnityEngine.Object.Destroy(_globalRoot.gameObject);
                _globalRoot = null;
            }

            _initialized = false;
        }

        // ────────────────────────────
        // 调试与统计
        // ────────────────────────────

        /// <summary>
        /// 获取所有 GameObject 池的统计信息
        /// </summary>
        public static string GetStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== GlobalPoolManager Statistics ===");
            sb.AppendLine($"GameObject Pools: {_gameObjectPools.Count}");

            foreach (var kvp in _gameObjectPools)
            {
                var pool = kvp.Value;
                var prefabName = pool.Prefab != null ? pool.Prefab.name : "null";
                sb.AppendLine($"  [{prefabName}] Active:{pool.CountActive} Inactive:{pool.CountInactive} Total:{pool.CountAll}");
            }

            sb.AppendLine($"Component Pools: {_componentPools.Count}");
            foreach (var kvp in _componentPools)
            {
                sb.AppendLine($"  [{kvp.Key}] Registered");
            }

            sb.AppendLine($"Object Pools: {_objectPools.Count}");
            foreach (var kvp in _objectPools)
            {
                sb.AppendLine($"  [{kvp.Key}] Registered");
            }

            return sb.ToString();
        }
    }
}
