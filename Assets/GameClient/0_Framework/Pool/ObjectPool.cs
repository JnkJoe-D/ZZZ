using System;
using System.Collections.Generic;

namespace Game.Framework
{
    /// <summary>
    /// 纯 C# 对象池
    /// 适用于不依赖 Unity 引擎的普通 C# 类的池化管理
    /// 支持自定义创建/取出/归还逻辑、容量限制和预热
    /// </summary>
    /// <typeparam name="T">池化对象类型，必须为引用类型</typeparam>
    public class ObjectPool<T> : IClearable, IDisposable where T : class
    {
        /// <summary>
        /// 池配置
        /// </summary>
        public struct Config
        {
            /// <summary>初始预分配数量</summary>
            public int initialSize;
            /// <summary>最大缓存数量，超出时丢弃。0 表示不限制</summary>
            public int maxSize;

            /// <summary>默认配置</summary>
            public static Config Default => new Config { initialSize = 0, maxSize = 64 };
        }

        private readonly Config _config;
        private readonly Func<T> _createFunc;
        private readonly Stack<T> _inactive;
        private int _activeCount;
        private bool _disposed;

        /// <summary>
        /// 取出对象时的回调
        /// </summary>
        public Action<T> OnGet;

        /// <summary>
        /// 归还对象时的回调（用于重置对象状态）
        /// </summary>
        public Action<T> OnReturn;

        /// <summary>
        /// 对象销毁时的回调（仅当对象实现了 IDisposable 时自动调用，或可自定义清理）
        /// </summary>
        public Action<T> OnDestroy;

        // ── 属性 ──

        /// <summary>当前池中空闲对象数量</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>当前已借出的活跃对象数量</summary>
        public int CountActive => _activeCount;

        /// <summary>池中对象总数（空闲 + 活跃）</summary>
        public int CountAll => CountInactive + CountActive;

        // ── 构造 ──

        /// <summary>
        /// 创建 ObjectPool
        /// </summary>
        /// <param name="createFunc">对象创建工厂方法</param>
        /// <param name="config">池配置</param>
        public ObjectPool(Func<T> createFunc, Config config = default)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _config = config.initialSize <= 0 && config.maxSize <= 0 ? Config.Default : config;
            _inactive = new Stack<T>(_config.maxSize > 0 ? _config.maxSize : 16);
            _activeCount = 0;
            _disposed = false;

            Prewarm(_config.initialSize);
        }

        /// <summary>
        /// 简化构造：使用无参构造函数作为工厂方法
        /// 要求 T 具有公共无参构造函数
        /// </summary>
        /// <param name="config">池配置</param>
        public ObjectPool(Config config = default)
            : this(() => Activator.CreateInstance<T>(), config)
        {
        }

        // ── 核心 API ──

        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        public T Get()
        {
            ThrowIfDisposed();

            T item;
            if (_inactive.Count > 0)
            {
                item = _inactive.Pop();
            }
            else
            {
                item = _createFunc();
            }

            _activeCount++;
            OnGet?.Invoke(item);

            return item;
        }

        /// <summary>
        /// 归还对象到池中
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            _activeCount = Math.Max(0, _activeCount - 1);
            OnReturn?.Invoke(item);

            // 超过最大容量，执行销毁回调后丢弃
            if (_config.maxSize > 0 && _inactive.Count >= _config.maxSize)
            {
                DestroyItem(item);
                return;
            }

            _inactive.Push(item);
        }

        /// <summary>
        /// 清空池中所有空闲对象
        /// </summary>
        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                var item = _inactive.Pop();
                DestroyItem(item);
            }
        }

        /// <summary>
        /// 销毁池中所有空闲对象并释放资源
        /// 注意：活跃对象的生命周期由调用方管理
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Clear();
            _activeCount = 0;

            OnGet = null;
            OnReturn = null;
            OnDestroy = null;
        }

        // ── 内部方法 ──

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _inactive.Push(_createFunc());
            }
        }

        /// <summary>
        /// 销毁单个对象：优先调用 OnDestroy 回调，其次尝试 IDisposable
        /// </summary>
        private void DestroyItem(T item)
        {
            if (item == null) return;

            if (OnDestroy != null)
            {
                OnDestroy(item);
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"ObjectPool<{typeof(T).Name}>");
        }
    }
}
