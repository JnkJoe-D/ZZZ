using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 泛型组件对象池
    /// 适用于 AudioSource、Light 等 Unity 组件的池化管理
    /// 支持自定义创建/取出/归还逻辑和动态扩容
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    public class ComponentPool<T> : IPool<T>, IDisposable where T : Component
    {
        /// <summary>
        /// 池配置
        /// </summary>
        public struct Config
        {
            /// <summary>初始预分配数量</summary>
            public int initialSize;
            /// <summary>最大容量，0 表示不限制</summary>
            public int maxSize;

            /// <summary>默认配置</summary>
            public static Config Default => new Config { initialSize = 5, maxSize = 20 };
        }

        private readonly Config _config;
        private readonly Func<T> _createFunc;
        private readonly Stack<T> _inactive;
        private readonly HashSet<T> _active;
        private bool _disposed;

        /// <summary>
        /// 取出组件时的回调
        /// </summary>
        public Action<T> OnGet;

        /// <summary>
        /// 归还组件时的回调
        /// </summary>
        public Action<T> OnReturn;

        // ── IPool<T> 属性 ──

        public int CountInactive => _inactive.Count;
        public int CountActive => _active.Count;
        public int CountAll => CountInactive + CountActive;

        // ── 构造 ──

        /// <summary>
        /// 创建 ComponentPool
        /// </summary>
        /// <param name="createFunc">组件创建工厂方法</param>
        /// <param name="config">池配置</param>
        public ComponentPool(Func<T> createFunc, Config config = default)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _config = config.initialSize <= 0 && config.maxSize <= 0 ? Config.Default : config;
            _inactive = new Stack<T>(_config.maxSize > 0 ? _config.maxSize : 16);
            _active = new HashSet<T>();
            _disposed = false;

            Prewarm(_config.initialSize);
        }

        // ── 核心 API ──

        /// <summary>
        /// 从池中获取一个组件
        /// </summary>
        public T Get()
        {
            ThrowIfDisposed();

            T component = null;

            // 从池中取出，跳过已被外部销毁的组件
            while (_inactive.Count > 0)
            {
                component = _inactive.Pop();
                if (component != null) break;
                component = null;
            }

            // 池中无可用组件，创建新的
            if (component == null)
            {
                component = _createFunc();
            }

            component.gameObject.SetActive(true);
            _active.Add(component);
            OnGet?.Invoke(component);

            return component;
        }

        /// <summary>
        /// 归还组件到池中
        /// </summary>
        public void Return(T component)
        {
            if (component == null) return;

            // 只归还本池借出的组件
            if (!_active.Remove(component))
            {
                GLog.Warning(LogTags.Pool, $"[ComponentPool<{typeof(T).Name}>] 尝试归还不属于本池的组件");
                return;
            }

            OnReturn?.Invoke(component);

            // 超过最大容量，直接销毁
            if (_config.maxSize > 0 && _inactive.Count >= _config.maxSize)
            {
                UnityEngine.Object.Destroy(component.gameObject);
                return;
            }

            component.gameObject.SetActive(false);
            _inactive.Push(component);
        }

        /// <summary>
        /// 清空池中所有空闲组件
        /// </summary>
        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                var comp = _inactive.Pop();
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp.gameObject);
                }
            }
        }

        /// <summary>
        /// 销毁池中所有组件（空闲 + 活跃）并释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Clear();

            foreach (var comp in _active)
            {
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp.gameObject);
                }
            }
            _active.Clear();

            OnGet = null;
            OnReturn = null;
        }

        // ── 内部方法 ──

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var comp = _createFunc();
                comp.gameObject.SetActive(false);
                _inactive.Push(comp);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"ComponentPool<{typeof(T).Name}>");
        }
    }
}
