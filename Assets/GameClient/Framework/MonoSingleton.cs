using UnityEngine;

namespace Game.Framework
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static GameObject _globalRoot;

        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton<{typeof(T).Name}>] Instance \'{typeof(T)}\' already destroyed on application quit. Won\'t create again - returning null.");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindFirstObjectByType<T>();
                            if (_instance == null)
                            {
                                if (_globalRoot == null)
                                {
                                    _globalRoot = GameObject.Find("[Global]");
                                    if (_globalRoot == null)
                                    {
                                        _globalRoot = new GameObject("[Global]");
                                        DontDestroyOnLoad(_globalRoot);
                                    }
                                }

                                GameObject singletonObject = new GameObject($"[{typeof(T).Name}]");
                                singletonObject.transform.SetParent(_globalRoot.transform);
                                _instance = singletonObject.AddComponent<T>();
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        
        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
