using System;
using System.Collections.Generic;
using Game.Logic;
using Game.MAnimSystem;
using ATEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class ATServiceFactory
    {
        /// <summary>
        ///  公共缓存池
        /// </summary>
        static readonly Dictionary<GameObject, Dictionary<Type, object>> _staticCache
            = new Dictionary<GameObject, Dictionary<Type, object>>();

        public static object ProvideService(Type serviceType,GameObject owner)
        {
            if (serviceType == typeof(IAnimationHandler))
            {
                return GetOrCreateCachedService(serviceType, owner,()=>
                {
                    var animComp = owner.GetComponent<AnimComponent>();
                    if (animComp == null)
                        return null;
                    return new ATAnimationHandler(animComp);
                });
            }

            if (serviceType == typeof(MonoBehaviour))
            {
                return GetOrCreateCachedService(serviceType, owner ,() =>
                {
                    var animComp = owner.GetComponent<AnimComponent>();
                    if (animComp == null) return null;
                    return new ATAnimationHandler(animComp);
                });
            }
            if (serviceType == typeof(IBoneGetter))                
            {
                return GetOrCreateCachedService(serviceType, owner ,() =>new ATBoneGetter(owner));
            }
            if (serviceType == typeof(IAudioHandler))
            {
                return ATAudioHandler.Instance;
            }

            if (serviceType == typeof(IHitHandler))
            {
                return new ATHitHandler();
            }
            if (serviceType == typeof(IVFXHandler))
            {
                return ATVFXHandler.Instance;
            }
            if (serviceType == typeof(ISpawnHandler))
            {
                return new ATSpawnHandler();
            }
            if (serviceType == typeof(ICameraHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => new ATCameraHandler(owner.GetComponent<RoleEntity>()));
            }
            if (serviceType == typeof(ITransformHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => {
                    var entity = owner.GetComponent<CharacterEntity>();
                    return entity != null ? new ATTransformHandler(entity) : null;
                });
            }
            if (serviceType == typeof(IEventHandler))
            {
                var ownerHandler = owner != null ? owner.GetComponent<IEventHandler>() : null;
                return GetOrCreateCachedService(serviceType, owner ,() => ownerHandler);
            }

            if (serviceType == typeof(IRouteWindowHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () =>
                {
                    var entity = owner.GetComponent<CharacterEntity>();
                    if (entity is RoleEntity role) return role.ActionController;
                    if (entity is MonsterEntity monster) return monster.ActionController;
                    return null;
                });
            }

            if (serviceType == typeof(IMotionWindowHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => owner.GetComponent<CharacterEntity>()?.MotionWindowHandler);
            }

            if (serviceType == typeof(IPhysicsHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => new ATPhysicsHandler(owner));
            }
            if(serviceType == typeof(IAttackWarningHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => {
                    var entity = owner.GetComponent<CharacterEntity>();
                    return entity != null ? new ATAttackWarningHandler(entity) : null;
                });
            }
            if (serviceType == typeof(IAssistHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => {
                    var entity = owner.GetComponent<CharacterEntity>();
                    return entity != null ? new ATAssistHandler(entity) : null;
                });
            }

            if (serviceType == typeof(IParryWindowHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => {
                    var entity = owner.GetComponent<CharacterEntity>();
                    return entity != null ? new ATParryWindowHandler(entity) : null;
                });
            }

            return null;
        }

        static object GetOrCreateCachedService(Type serviceType, GameObject owner, Func<object> factory)
        {
            if (owner == null)
                return null;

            if (!_staticCache.TryGetValue(owner, out var ownerCache))
            {
                ownerCache = new Dictionary<Type, object>();
                _staticCache[owner] = ownerCache;
            }

            if (ownerCache.TryGetValue(serviceType, out var cached) && cached != null)
                return cached;

            var created = factory();
            ownerCache[serviceType] = created;
            return created;
        }
        public static void RemoveStaticCaches(GameObject obj)
        {
            if(obj == null)return;
            if(_staticCache.TryGetValue(obj,out var caches))
            {
                caches.Clear();
                _staticCache.Remove(obj);
            }
        }
        public static void ClearAllStaticCaches()
        {
            foreach (var outerKvp in _staticCache)
            {
                if (outerKvp.Value == null)
                    continue;

                foreach (var innerKvp in outerKvp.Value)
                {
                    if (innerKvp.Value is IDisposable disposable)
                        disposable.Dispose();
                }
            }

            _staticCache.Clear();
        }

#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void AutoClearOnDomainReload()
        {
            ClearAllStaticCaches();
        }
#endif
    }
}
