using System;
using System.Collections.Generic;

namespace Game.GamePlay
{
    public class EntityDataModule
    {
        private readonly Dictionary<Type, IEntityRuntimeData> _dataMap = new Dictionary<Type, IEntityRuntimeData>();

        public void Add<T>(T data) where T : class, IEntityRuntimeData
        {
            _dataMap[typeof(T)] = data;
        }

        public void Remove<T>() where T : class, IEntityRuntimeData
        {
            _dataMap.Remove(typeof(T));
        }

        public T Get<T>() where T : class, IEntityRuntimeData
        {
            if (_dataMap.TryGetValue(typeof(T), out var data) && data is T typed)
                return typed;

            // 防御性自愈：若容器中尚未注册该类型，自动按需创建具备无参构造的实例并缓存，杜绝生命周期时序或单测环境下的空引用
            if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                var created = (T)Activator.CreateInstance(typeof(T));
                _dataMap[typeof(T)] = created;
                return created;
            }

            return null;
        }

        public IEntityRuntimeData this[Type type]
        {
            get => _dataMap.TryGetValue(type, out var data) ? data : null;
            set => _dataMap[type] = value;
        }
    }
}
