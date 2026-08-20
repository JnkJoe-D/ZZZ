using System;
using System.Collections.Generic;

namespace Game.Logic
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
            if (_dataMap.TryGetValue(typeof(T), out var data))
                return data as T;
            return null;
        }

        public IEntityRuntimeData this[Type type]
        {
            get => _dataMap.TryGetValue(type, out var data) ? data : null;
            set => _dataMap[type] = value;
        }
    }
}
