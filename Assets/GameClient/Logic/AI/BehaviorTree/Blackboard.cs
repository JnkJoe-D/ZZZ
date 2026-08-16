using System;
using System.Collections.Generic;

namespace Game.Logic.AI.BehaviorTree
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        private readonly Dictionary<string, List<Action<Type, object>>> _observers = new Dictionary<string, List<Action<Type, object>>>();
        
        public enum Type
        {
            ADD,
            REMOVE,
            CHANGE
        }

        public Clock Clock { get; private set; }

        public Blackboard(Clock clock)
        {
            Clock = clock;
        }

        public void Set(string key, object value)
        {
            if (_data.TryGetValue(key, out var existing))
            {
                if ((existing == null && value != null) || (existing != null && !existing.Equals(value)))
                {
                    _data[key] = value;
                    NotifyObservers(key, Type.CHANGE, value);
                }
            }
            else
            {
                _data[key] = value;
                NotifyObservers(key, Type.ADD, value);
            }
        }

        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return default(T);
        }

        public object Get(string key)
        {
            if (_data.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }

        public bool IsSet(string key)
        {
            return _data.ContainsKey(key);
        }

        public void Unset(string key)
        {
            if (_data.ContainsKey(key))
            {
                _data.Remove(key);
                NotifyObservers(key, Type.REMOVE, null);
            }
        }

        public void AddObserver(string key, Action<Type, object> observer)
        {
            if (!_observers.TryGetValue(key, out var list))
            {
                list = new List<Action<Type, object>>();
                _observers[key] = list;
            }
            list.Add(observer);
        }

        public void RemoveObserver(string key, Action<Type, object> observer)
        {
            if (_observers.TryGetValue(key, out var list))
            {
                list.Remove(observer);
            }
        }

        private void NotifyObservers(string key, Type type, object value)
        {
            if (_observers.TryGetValue(key, out var list))
            {
                // Create a copy to allow observers to remove themselves during notification
                var observersCopy = new List<Action<Type, object>>(list);
                foreach (var observer in observersCopy)
                {
                    observer?.Invoke(type, value);
                }
            }
        }
    }
}
