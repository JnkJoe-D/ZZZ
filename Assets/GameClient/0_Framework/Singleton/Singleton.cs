using System;

namespace Game.Framework
{
    /// <summary>
    /// 全局泛型单例基类 (非 MonoBehaviour)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> where T : class
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 使用反射调用无参构造函数创建实例，允许构造函数是非 public 的
                            _instance = (T)Activator.CreateInstance(typeof(T), true);
                        }
                    }
                }
                return _instance;
            }
        }
    }
}
