using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ATEditor
{
    /// <summary>默认攻击检测处理器（基于 Unity 物理重叠查询）。窗口独占缓存；扇形/环形使用保守包围体筛选，不是网格精确相交或连续扫掠。</summary>
    public sealed class DefaultAttackDetectionHandler : IAttackDetectionHandler
    {
        public IAttackDetectionWindow OpenWindow(ProcessContext context, AttackClip clip) => new Window(context, clip);

        private sealed class Window : IAttackDetectionWindow
        {
            private static long _nextWindowId;
            private readonly long _id = Interlocked.Increment(ref _nextWindowId);
            private readonly ProcessContext _context;
            private readonly AttackClip _clip;
            private readonly IBoneGetter _bones;
            private readonly IAttackTargetResolver _resolver;
            private readonly Vector3 _fixedPosition;
            private readonly Quaternion _fixedRotation;
            private Collider[] _buffer = new Collider[32];
            private readonly Dictionary<UnityEngine.Object, Collider> _candidates = new Dictionary<UnityEngine.Object, Collider>();
            private readonly Dictionary<UnityEngine.Object, float> _lastObserved = new Dictionary<UnityEngine.Object, float>();
            private readonly List<UnityEngine.Object> _ordered = new List<UnityEngine.Object>();
            private bool _disposed;
            private int _lastSample = -1;

            public Window(ProcessContext context, AttackClip clip)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                if (clip == null) throw new ArgumentNullException(nameof(clip));
                _clip = (AttackClip)clip.Clone();
                _clip.clipId = clip.clipId;
                _bones = context.GetService<IBoneGetter>();
                _resolver = context.GetService<IAttackTargetResolver>() ?? new DefaultAttackTargetResolver();
                _clip.scope.CaptureMatrix(context.OwnerTransform, _bones, out _fixedPosition, out _fixedRotation);
            }

            public IReadOnlyList<AttackObservation> Sample(float timelineTime, int sampleIndex)
            {
                if (_disposed || _context.Owner == null || sampleIndex <= _lastSample) return Array.Empty<AttackObservation>();
                _lastSample = sampleIndex;
                // Director 可在一帧内多次移动/采样，不能等待下一次 FixedUpdate 同步目标骨骼碰撞体。
                Physics.SyncTransforms();
                _clip.scope.CalculateMatrix(_context.OwnerTransform, _bones, _fixedPosition, _fixedRotation,
                    out Vector3 center, out Quaternion rotation);
                int count;
                // NonAlloc 满载时不能区分“刚好装满”与“被截断”，扩大后重新查询。
                while ((count = Query(center, rotation)) == _buffer.Length)
                    Array.Resize(ref _buffer, checked(_buffer.Length * 2));
                _candidates.Clear();
                _ordered.Clear();
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var collider = _buffer[i];
                        if (collider == null || (!_clip.includeSelf && collider.transform.IsChildOf(_context.OwnerTransform))) continue;
                        if (!InsideRoundShape(collider, center, rotation)) continue;
                        var target = _resolver.ResolveTarget(_context.Owner, collider);
                        if (target == null) continue;
                        if (_lastObserved.TryGetValue(target, out float last))
                        {
                            if (_clip.repeatMode == AttackRepeatMode.OncePerWindow) continue;
                            if (_clip.repeatMode == AttackRepeatMode.Cooldown && timelineTime - last + 0.000001f < _clip.cooldown) continue;
                        }
                        else if (_clip.maxTargets > 0 && _lastObserved.Count >= _clip.maxTargets) continue;
                        if (!_candidates.TryGetValue(target, out var previous))
                        {
                            _candidates.Add(target, collider);
                            _ordered.Add(target);
                        }
                        else if (Compare(collider, previous, center) < 0) _candidates[target] = collider;
                    }
                    if (_clip.sortMode == TargetSortMode.Random)
                    {
                        for (int i = 0; i < _ordered.Count; i++)
                        {
                            int j = UnityEngine.Random.Range(i, _ordered.Count);
                            var swap = _ordered[i]; _ordered[i] = _ordered[j]; _ordered[j] = swap;
                        }
                    }
                    else _ordered.Sort((a, b) => Compare(_candidates[a], _candidates[b], center));

                    var results = new List<AttackObservation>(_ordered.Count);
                    foreach (var target in _ordered)
                    {
                        bool known = _lastObserved.ContainsKey(target);
                        if (!known && _clip.maxTargets > 0 && _lastObserved.Count >= _clip.maxTargets) continue;
                        _lastObserved[target] = timelineTime;
                        results.Add(new AttackObservation(_context.PlaybackId, _id, _context.SkillId, _clip.clipId,
                            sampleIndex, timelineTime, _context.Owner, target, _candidates[target], center));
                    }
                    return results.ToArray();
                }
                finally
                {
                    Array.Clear(_buffer, 0, count);
                    _candidates.Clear(); _ordered.Clear();
                }
            }

            private static int Compare(Collider a, Collider b, Vector3 center)
            {
                int order = (a.ClosestPoint(center) - center).sqrMagnitude.CompareTo((b.ClosestPoint(center) - center).sqrMagnitude);
                return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
            }

            private int Query(Vector3 center, Quaternion rotation)
            {
                var s = _clip.scope.shape;
                float radius = Mathf.Max(0f, s.radius);
                switch (s.shapeType)
                {
                    case HitBoxType.Sphere:
                        return Physics.OverlapSphereNonAlloc(center, radius, _buffer, _clip.layers, _clip.triggers);
                    case HitBoxType.Box:
                        return Physics.OverlapBoxNonAlloc(center, new Vector3(Mathf.Abs(s.size.x), Mathf.Abs(s.size.y), Mathf.Abs(s.size.z)) * 0.5f,
                            _buffer, rotation, _clip.layers, _clip.triggers);
                    case HitBoxType.Capsule:
                        var half = rotation * Vector3.up * (Mathf.Max(0f, s.height - radius * 2f) * 0.5f);
                        return Physics.OverlapCapsuleNonAlloc(center - half, center + half, radius, _buffer, _clip.layers, _clip.triggers);
                    case HitBoxType.Sector:
                    case HitBoxType.Ring:
                        return Physics.OverlapBoxNonAlloc(center, new Vector3(radius, Mathf.Max(0f, s.height) * 0.5f, radius),
                            _buffer, rotation, _clip.layers, _clip.triggers);
                    default: return 0;
                }
            }

            private bool InsideRoundShape(Collider collider, Vector3 center, Quaternion rotation)
            {
                var shape = _clip.scope.shape;
                if (shape.shapeType != HitBoxType.Sector && shape.shapeType != HitBoxType.Ring) return true;
                Bounds bounds = collider.bounds;
                var inverse = Quaternion.Inverse(rotation);
                Vector3 p = inverse * (bounds.center - center);
                // 把世界 AABB 投影至检测局部轴，避免旋转检测盒下低估目标范围。
                Vector3 x = inverse * Vector3.right, y = inverse * Vector3.up, z = inverse * Vector3.forward;
                Vector3 e = Abs(x) * bounds.extents.x + Abs(y) * bounds.extents.y + Abs(z) * bounds.extents.z;
                if (Mathf.Abs(p.y) - e.y > shape.height * 0.5f) return false;
                float targetRadius = new Vector2(e.x, e.z).magnitude;
                var planar = new Vector2(p.x, p.z);
                float distance = planar.magnitude;
                if (distance - targetRadius > shape.radius) return false;
                if (shape.shapeType == HitBoxType.Ring) return distance + targetRadius >= shape.innerRadius;
                return distance <= targetRadius || Vector2.Angle(Vector2.up, planar)
                    - Mathf.Asin(Mathf.Clamp01(targetRadius / distance)) * Mathf.Rad2Deg <= shape.angle * 0.5f;
            }

            private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

            public void Dispose()
            {
                _disposed = true;
                Array.Clear(_buffer, 0, _buffer.Length);
                _candidates.Clear(); _ordered.Clear(); _lastObserved.Clear();
            }
        }
    }
}
