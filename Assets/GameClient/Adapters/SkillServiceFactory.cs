using System;
using System.Collections.Generic;
using Game.MAnimSystem;
using Game.Pool;
using SkillEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class SkillServiceFactory : IServiceFactory
    {
        readonly GameObject _owner;

        static readonly Dictionary<GameObject, Dictionary<Type, object>> _staticCache
            = new Dictionary<GameObject, Dictionary<Type, object>>();

        public SkillServiceFactory(GameObject owner)
        {
            _owner = owner;

            var keys = new List<GameObject>(_staticCache.Keys);
            foreach (var key in keys)
            {
                if (key == null)
                    _staticCache.Remove(key);
            }

            if (_owner != null && !_staticCache.ContainsKey(_owner))
                _staticCache[_owner] = new Dictionary<Type, object>();
        }

        public object ProvideService(Type serviceType)
        {
            if (serviceType == typeof(ISkillAnimationHandler))
            {
                var animComp = _owner.GetComponent<AnimComponent>();
                if (animComp == null)
                    return null;
                return new SkillAnimationHandler(animComp);
            }

            if (serviceType == typeof(MonoBehaviour))
                return _owner.GetComponent<MonoBehaviour>();

            if (serviceType == typeof(ISkillBoneGetter))
                return new SkillBoneGetter(_owner);

            if (serviceType == typeof(ISkillAudioHandler))
            {
                var audioComp = _owner.GetComponent<SkillAudioHandler>();
                if (audioComp == null)
                    audioComp = _owner.AddComponent<SkillAudioHandler>();
                return audioComp;
            }

            if (serviceType == typeof(ISkillHitHandler))
                return new SkillHitHandler();

            if (serviceType == typeof(ISkillVFXHandler))
                return new SkillVFXHandler(_owner.GetComponent<MonoBehaviour>());

            if (serviceType == typeof(ISkillSpawnHandler))
                return new SkillSpawnHandler();

            if (serviceType == typeof(ISkillCameraHandler))
                return GetOrCreateCachedService(serviceType, () => new SkillCameraHandler());

            if (serviceType == typeof(ISkillEventHandler))
            {
                var ownerHandler = _owner != null ? _owner.GetComponent<ISkillEventHandler>() : null;
                return GetOrCreateCachedService(serviceType, () => ownerHandler);
            }

            return null;
        }

        object GetOrCreateCachedService(Type serviceType, Func<object> factory)
        {
            if (_owner == null)
                return factory();

            if (_staticCache[_owner].TryGetValue(serviceType, out var cached) && cached != null)
                return cached;

            var created = factory();
            _staticCache[_owner][serviceType] = created;
            return created;
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
