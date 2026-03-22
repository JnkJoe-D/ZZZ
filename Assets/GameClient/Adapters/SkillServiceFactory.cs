using System;
using System.Collections.Generic;
using Game.Logic.Character;
using Game.MAnimSystem;
using SkillEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class SkillServiceFactory
    {
        /// <summary>
        ///  公共缓存池
        /// </summary>
        static readonly Dictionary<GameObject, Dictionary<Type, object>> _staticCache
            = new Dictionary<GameObject, Dictionary<Type, object>>();

        public static object ProvideService(Type serviceType,GameObject owner)
        {
            if (serviceType == typeof(ISkillAnimationHandler))
            {
                return GetOrCreateCachedService(serviceType, owner,()=>
                {
                    var animComp = owner.GetComponent<AnimComponent>();
                    if (animComp == null)
                        return null;
                    return new SkillAnimationHandler(animComp);
                });
            }

            if (serviceType == typeof(MonoBehaviour))
            {
                return GetOrCreateCachedService(serviceType, owner ,() =>
                {
                    var animComp = owner.GetComponent<AnimComponent>();
                    if (animComp == null) return null;
                    return new SkillAnimationHandler(animComp);
                });
            }
            if (serviceType == typeof(ISkillBoneGetter))                
            {
                return GetOrCreateCachedService(serviceType, owner ,() =>new SkillBoneGetter(owner));
            }
            if (serviceType == typeof(ISkillAudioHandler))
            {
                return SkillAudioHandler.Instance;
            }

            if (serviceType == typeof(ISkillHitHandler))
            {
                return new SkillHitHandler();
            }
            if (serviceType == typeof(ISkillVFXHandler))
            {
                return SkillVFXHandler.Instance;
            }
            if (serviceType == typeof(ISkillSpawnHandler))
            {
                return new SkillSpawnHandler();
            }
            if (serviceType == typeof(ISkillCameraHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => new SkillCameraHandler(owner.GetComponent<CharacterEntity>()));
            }
            if (serviceType == typeof(ISkillMovementHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => {
                    var entity = owner.GetComponent<CharacterEntity>();
                    return entity != null ? new SkillMovementHandler(entity) : null;
                });
            }
            if (serviceType == typeof(ISkillEventHandler))
            {
                var ownerHandler = owner != null ? owner.GetComponent<ISkillEventHandler>() : null;
                return GetOrCreateCachedService(serviceType, owner ,() => ownerHandler);
            }

            if (serviceType == typeof(ISkillComboWindowHandler))
            {
                return GetOrCreateCachedService(serviceType, owner ,() => owner.GetComponent<CharacterEntity>().SkillComboWindowHandler);
            }

            if (serviceType == typeof(ISkillMotionWindowHandler))
            {
                return GetOrCreateCachedService(serviceType, owner, () => owner.GetComponent<CharacterEntity>().SkillMotionWindowHandler);
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
    }
}
