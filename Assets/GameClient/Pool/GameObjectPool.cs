using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Pool
{
    /// <summary>
    /// 基于预制体的 GameObject 对象池
    /// 支持容量限制、预热、自定义回调、null 安全检查
    /// </summary>
    public class GameObjectPool : IPool<GameObject>, IDisposable
    {
        /// <summary>
        /// 池配置
        /// </summary>
        public struct Config
        {
            /// <summary>最大缓存数量，超出时直接销毁。0 表示不限制</summary>
            public int maxSize;
            /// <summary>预热数量，初始化时预创建的对象数</summary>
            public int prewarmCount;

            /// <summary>默认配置</summary>
            public static Config Default => new Config { maxSize = 50, prewarmCount = 0 };
        }

        private readonly GameObject _prefab;
        private readonly Config _config;
        private readonly Stack<GameObject> _inactive;
        private readonly HashSet<GameObject> _active;
        private Transform _poolRoot;
        private bool _disposed;

        /// <summary>
        /// 取出对象时的回调（在 SetActive(true) 之后调用）
        /// </summary>
        public Action<GameObject> OnSpawn;

        /// <summary>
        /// 归还对象时的回调（在 SetActive(false) 之前调用）
        /// </summary>
        public Action<GameObject> OnReturn;

        // ── IPool<GameObject> 属性 ──

        public int CountInactive => _inactive.Count;
        public int CountActive => _active.Count;
        public int CountAll => CountInactive + CountActive;

        /// <summary>
        /// 此池对应的预制体
        /// </summary>
        public GameObject Prefab => _prefab;

        // ── 构造 ──

        /// <summary>
        /// 创建 GameObjectPool
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="poolRoot">池根节点，空闲对象将挂载到此节点下</param>
        /// <param name="config">池配置</param>
        public GameObjectPool(GameObject prefab, Transform poolRoot, Config config = default)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _poolRoot = poolRoot;
            _config = config.maxSize <= 0 && config.prewarmCount <= 0 ? Config.Default : config;
            _inactive = new Stack<GameObject>(_config.maxSize > 0 ? _config.maxSize : 16);
            _active = new HashSet<GameObject>();
            _disposed = false;

            Prewarm(_config.prewarmCount);
        }

        // ── 核心 API ──

        /// <summary>
        /// 从池中获取一个 GameObject 实例
        /// </summary>
        public GameObject Get()
        {
            ThrowIfDisposed();

            GameObject instance = null;

            // 从池中取出，跳过已被外部销毁的对象
            while (_inactive.Count > 0)
            {
                instance = _inactive.Pop();
                if (instance != null) break;
                instance = null;
            }

            // 池中无可用对象，创建新的
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(_prefab);
            }

            instance.SetActive(true);
            _active.Add(instance);
            OnSpawn?.Invoke(instance);

            return instance;
        }

        /// <summary>
        /// 从池中获取一个 GameObject 并设置变换信息
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点（null 则无父节点）</param>
        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var instance = Get();

            instance.transform.SetPositionAndRotation(position, rotation);
            if (parent != null)
            {
                instance.transform.SetParent(parent);
            }
            else
            {
                if (instance.transform.parent != null)
                {
                    instance.transform.SetParent(null);
                }
            }

            return instance;
        }

        /// <summary>
        /// 归还 GameObject 到池中
        /// </summary>
        public void Return(GameObject instance)
        {
            if (instance == null) return;

            // 只归还本池借出的对象
            if (!_active.Remove(instance))
            {
                Debug.LogWarning($"[GameObjectPool] 尝试归还一个不属于本池的对象: {instance.name}");
                return;
            }

            OnReturn?.Invoke(instance);

            // 超过最大容量，直接销毁
            if (_config.maxSize > 0 && _inactive.Count >= _config.maxSize)
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            
            bool isSafeToParent = true;
#if UNITY_EDITOR
            if (!Application.isPlaying) isSafeToParent = false;
#endif

            if (_poolRoot != null && isSafeToParent)
            {
                instance.transform.SetParent(_poolRoot);
            }
            _inactive.Push(instance);
        }

        /// <summary>
        /// 清空池中所有空闲对象
        /// </summary>
        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                var obj = _inactive.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }

        /// <summary>
        /// 销毁池中所有对象（空闲 + 活跃）并释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 销毁空闲对象
            Clear();

            // 销毁活跃对象
            foreach (var obj in _active)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            _active.Clear();

            OnSpawn = null;
            OnReturn = null;
        }

        // ── 内部方法 ──

        /// <summary>
        /// 预热：预创建指定数量的对象
        /// </summary>
        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = UnityEngine.Object.Instantiate(_prefab);
                obj.SetActive(false);
                if (_poolRoot != null)
                {
                    obj.transform.SetParent(_poolRoot);
                }
                _inactive.Push(obj);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    $"GameObjectPool ({(_prefab != null ? _prefab.name : "null")})");
        }
    }
}
