using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ATEditor
{
    /// <summary>Director 私有缓存。每次借用有独立租约，归还后旧租约永久失效。</summary>
    internal sealed class ActionObjectPool : IDisposable
    {
        internal sealed class Lease : IDisposable
        {
            private ActionObjectPool _pool;
            internal readonly object Key;
            public GameObject Instance { get; }
            public bool IsValid => _pool != null && Instance != null;
            internal Lease(ActionObjectPool pool, object key, GameObject instance)
            { _pool = pool; Key = key; Instance = instance; }
            public void Dispose()
            {
                var pool = _pool;
                _pool = null;
                pool?.Return(this);
            }
            internal void Invalidate() => _pool = null;
        }

        private readonly Transform _storage;
        private readonly int _capacity;
        private readonly Dictionary<object, Stack<GameObject>> _idle = new Dictionary<object, Stack<GameObject>>();
        private readonly HashSet<Lease> _active = new HashSet<Lease>();
        private bool _disposed;
        private bool _closing;

        public ActionObjectPool(GameObject owner, int capacityPerAsset = 8)
        {
            _capacity = capacityPerAsset;
            var storage = new GameObject("ActionSystem Pool");
            storage.SetActive(false);
            storage.transform.SetParent(owner.transform, false);
            _storage = storage.transform;
        }

        public Transform Storage => _storage;

        public Lease Rent(object key, Func<Transform, GameObject> create)
        {
            if (_disposed || _closing) throw new ObjectDisposedException(nameof(ActionObjectPool));
            if (!_idle.TryGetValue(key, out var idle)) _idle.Add(key, idle = new Stack<GameObject>());
            GameObject instance = null;
            while (idle.Count > 0 && instance == null) instance = idle.Pop();
            if (instance == null) instance = create(_storage);
            instance.SetActive(false);
            var lease = new Lease(this, key, instance);
            _active.Add(lease);
            return lease;
        }

        private void Return(Lease lease)
        {
            if (!_active.Remove(lease) || lease.Instance == null) return;
            var instance = lease.Instance;
            instance.SetActive(false);
            if (_disposed || _closing || _idle[lease.Key].Count >= _capacity) { Destroy(instance); return; }
            instance.transform.SetParent(_storage, false);
            _idle[lease.Key].Push(instance);
        }

        public void BeginShutdown() => _closing = true;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var leases = new List<Lease>(_active);
            _active.Clear();
            foreach (var lease in leases)
            {
                lease.Invalidate();
                Destroy(lease.Instance);
            }
            _idle.Clear();
            if (_storage != null) Destroy(_storage.gameObject);
        }

        internal static void Destroy(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (Application.isPlaying) Object.Destroy(instance);
            else Object.DestroyImmediate(instance);
        }
    }
}
