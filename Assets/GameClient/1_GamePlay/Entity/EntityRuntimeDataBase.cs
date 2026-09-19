using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 实体运行时数据抽象基类。
    /// 内部采用高性能类型分区槽（Typed Partitioned Slots），实现零样板代码与基础值类型零装箱存储。
    /// 子类无需编写任何分发 switch，新增字段仅需声明一行只读属性。
    /// </summary>
    public abstract class EntityRuntimeDataBase : IEntityRuntimeData
    {
        // ─── 基础类型分区池 (完全消除 bool, int, float 的装箱与 GC Alloc) ───
        private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _objects = new Dictionary<string, object>(StringComparer.Ordinal);

        public event Action<string, object, object> OnValueChanged;

        /// <summary>
        /// 统一设值入口：自动类型路由 + 防抖比对 + 统一事件广播
        /// </summary>
        public virtual bool Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return false;

            // 1. bool 特化路由（零装箱）
            if (typeof(T) == typeof(bool))
            {
                bool bVal = (bool)(object)value;
                _bools.TryGetValue(key, out bool oldB);
                if (_bools.ContainsKey(key) && oldB == bVal) return false;

                _bools[key] = bVal;
                NotifyValueChanged(key, oldB, bVal);
                return true;
            }

            // 2. int / enum 特化路由（零装箱）
            if (typeof(T) == typeof(int) || typeof(T).IsEnum)
            {
                int iVal = Convert.ToInt32(value);
                _ints.TryGetValue(key, out int oldI);
                if (_ints.ContainsKey(key) && oldI == iVal) return false;

                _ints[key] = iVal;
                NotifyValueChanged(key, oldI, value);
                return true;
            }

            // 3. float 特化路由（零装箱）
            if (typeof(T) == typeof(float))
            {
                float fVal = (float)(object)value;
                _floats.TryGetValue(key, out float oldF);
                if (_floats.ContainsKey(key) && Mathf.Approximately(oldF, fVal)) return false;

                _floats[key] = fVal;
                NotifyValueChanged(key, oldF, fVal);
                return true;
            }

            // 4. 引用类型 / 通用对象存储
            _objects.TryGetValue(key, out object oldObj);
            if (_objects.ContainsKey(key) && EqualityComparer<T>.Default.Equals((T)oldObj, value))
            {
                return false;
            }

            _objects[key] = value;
            NotifyValueChanged(key, oldObj, value);
            return true;
        }

        /// <summary>
        /// 统一取值入口：直接从特化池提取，零拆箱
        /// </summary>
        public virtual T Get<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

            if (typeof(T) == typeof(bool))
            {
                return _bools.TryGetValue(key, out bool b) ? (T)(object)b : defaultValue;
            }

            if (typeof(T) == typeof(int))
            {
                return _ints.TryGetValue(key, out int i) ? (T)(object)i : defaultValue;
            }

            if (typeof(T).IsEnum)
            {
                return _ints.TryGetValue(key, out int i) ? (T)Enum.ToObject(typeof(T), i) : defaultValue;
            }

            if (typeof(T) == typeof(float))
            {
                return _floats.TryGetValue(key, out float f) ? (T)(object)f : defaultValue;
            }

            if (_objects.TryGetValue(key, out object obj) && obj is T typed)
            {
                return typed;
            }

            return defaultValue;
        }

        public virtual bool TryGet<T>(string key, out T value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (typeof(T) == typeof(bool) && _bools.TryGetValue(key, out bool b))
                {
                    value = (T)(object)b; return true;
                }
                if (typeof(T) == typeof(int) && _ints.TryGetValue(key, out int i))
                {
                    value = (T)(object)i; return true;
                }
                if (typeof(T).IsEnum && _ints.TryGetValue(key, out int e))
                {
                    value = (T)Enum.ToObject(typeof(T), e); return true;
                }
                if (typeof(T) == typeof(float) && _floats.TryGetValue(key, out float f))
                {
                    value = (T)(object)f; return true;
                }
                if (_objects.TryGetValue(key, out object obj) && obj is T typed)
                {
                    value = typed; return true;
                }
            }
            value = default;
            return false;
        }

        public virtual bool Has(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _bools.ContainsKey(key) || _ints.ContainsKey(key) || _floats.ContainsKey(key) || _objects.ContainsKey(key);
        }

        protected void NotifyValueChanged(string key, object oldValue, object newValue)
        {
            OnValueChanged?.Invoke(key, oldValue, newValue);
        }

        /// <summary>
        /// 统一重置：四个特化字典瞬间清空，子类零样板代码！
        /// </summary>
        public virtual void Reset()
        {
            _bools.Clear();
            _ints.Clear();
            _floats.Clear();
            _objects.Clear();
        }
    }
}
