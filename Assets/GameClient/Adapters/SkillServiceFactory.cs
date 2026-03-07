using System;
using System.Collections.Generic;
using UnityEngine;
using SkillEditor;
using Game.MAnimSystem;
using Game.Pool;

namespace Game.Adapters
{
    public class SkillServiceFactory : IServiceFactory
    {
        private GameObject _owner;

        // 全局静态缓存：以角色 (Owner) 为 Key
        // 解决每次播放重新 new Factory 导致的服务被重复创建引发内存泄漏
        private static Dictionary<GameObject, Dictionary<Type, object>> _staticCache = 
            new Dictionary<GameObject, Dictionary<Type, object>>();

        public SkillServiceFactory(GameObject owner)
        {
            _owner = owner;

            // 被动清理已销毁的 Owner 缓存
            var keys = new System.Collections.Generic.List<GameObject>(_staticCache.Keys);
            foreach (var key in keys)
            {
                if (key == null) _staticCache.Remove(key);
            }

            if (_owner != null && !_staticCache.ContainsKey(_owner))
            {
                _staticCache[_owner] = new System.Collections.Generic.Dictionary<Type, object>();
            }
        }

        public object ProvideService(Type serviceType)
        {
            // 1. 动画服务
            if (serviceType == typeof(ISkillAnimationHandler))
            {
                var animComp = _owner.GetComponent<AnimComponent>();
                if (animComp == null) return null;
                return new AnimComponentAdapter(animComp);
            }

            // 2. 协程服务 (返回 MonoBehaviour)
            // 优先查找现有的 MonoBehaviour 组件作为 Runner
            if (serviceType == typeof(MonoBehaviour))
            {
                var mb = _owner.GetComponent<MonoBehaviour>(); // 获取任意一个
                return mb;
            }

            // 3. 技能角色服务
            if (serviceType == typeof(ISkillActor))
            {
                 return new CharSkillActor(_owner);
            }

            if (serviceType == typeof(ISkillEventHandler))
            {
                 return _owner.GetComponent<ISkillEventHandler>();
            }

            //4. 音频管理服务
            if(serviceType == typeof(ISkillAudioHandler))
            {
                var audioComp = _owner.GetComponent<GameSkillAudioHandler>();
                if (audioComp == null)
                {
                    audioComp = _owner.AddComponent<GameSkillAudioHandler>();
                }
                return audioComp;
            }

            //5. 伤害处理服务
            if(serviceType == typeof(ISkillHitHandler))
            {
                return new SkillHitHandler();
            }

            // 6. VFX 对象池服务
            if (serviceType == typeof(IVFXPoolService))
            {
                return new VFXPoolServiceAdapter();
            }

            // 7. Spawn 服务
            if (serviceType == typeof(ISkillSpawnHandler))
            {
                return new SkillSpawnHandler();
            }

            // 8. 全局事件服务
            if (serviceType == typeof(ISkillEventHandler))
            {
                return new SkillEventHandler();
            }

            if(serviceType == typeof(ISkillCameraHandler))
            {
                if (_owner != null)
                {
                    if (_staticCache[_owner].TryGetValue(serviceType, out var cached))
                    {
                        if(cached != null) return cached;
                    }
                    var handler = new SkillCameraHandler();
                    _staticCache[_owner][serviceType] = handler;
                    return handler;
                }
                return new SkillCameraHandler();
            }
            return null;
        }
        public static void ClearAllStaticCaches()
        {
            foreach (var outerKvp in _staticCache)
            {
                if (outerKvp.Value != null)
                {
                    foreach (var innerKvp in outerKvp.Value)
                    {
                        if (innerKvp.Value is System.IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }
            _staticCache.Clear();
        }
    }

    /// <summary>
    /// IVFXPoolService 适配器，委托给 GlobalPoolManager
    /// </summary>
    internal class VFXPoolServiceAdapter : IVFXPoolService
    {
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return GlobalPoolManager.Spawn(prefab, position, rotation, parent);
        }

        public void Return(GameObject instance)
        {
            GlobalPoolManager.Return(instance);
        }
    }
}
