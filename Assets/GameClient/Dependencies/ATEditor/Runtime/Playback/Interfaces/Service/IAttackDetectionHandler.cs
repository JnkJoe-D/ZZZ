using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>几何重叠观察，不代表已经通过阵营、无敌、格挡等规则确认命中。</summary>
    public readonly struct AttackObservation
    {
        public readonly long PlaybackId, WindowId;
        public readonly int ActionId, SampleIndex;
        public readonly string SourceClipId;
        public readonly float TimelineTime;
        public readonly GameObject Owner;
        public readonly UnityEngine.Object Target;
        public readonly Collider Collider;
        public readonly Vector3 QueryCenter, ContactPoint, Direction;

        public AttackObservation(long playbackId, long windowId, int actionId, string sourceClipId,
            int sampleIndex, float time, GameObject owner, UnityEngine.Object target, Collider collider, Vector3 center)
        {
            PlaybackId = playbackId; WindowId = windowId; ActionId = actionId; SourceClipId = sourceClipId;
            SampleIndex = sampleIndex; TimelineTime = time; Owner = owner; Target = target; Collider = collider;
            QueryCenter = center; ContactPoint = collider.ClosestPoint(center);
            var direction = ContactPoint - center;
            Direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.zero;
        }
    }

    public interface IAttackTargetResolver
    {
        /// <summary>合并多个 HurtBox 为一个目标；返回 null 在排序和配额之前剔除。业务筛选可在此扩展。</summary>
        UnityEngine.Object ResolveTarget(GameObject owner, Collider collider);
    }

    public interface IAttackObservationHandler { void OnObserved(AttackObservation observation); }

    public interface IAttackDetectionHandler
    {
        IAttackDetectionWindow OpenWindow(ProcessContext context, AttackClip clip);
    }

    public interface IAttackDetectionWindow : IDisposable
    {
        /// <summary>返回本轮独立快照；重复策略与不同目标预算由窗口持有，Dispose 后不再检测。</summary>
        IReadOnlyList<AttackObservation> Sample(float timelineTime, int sampleIndex);
    }

    /// <summary>宿主可据此在采样时刻分段推进；直接使用 Runner 的调用方自行控制步长。</summary>
    public interface IProcessTickBoundary { float NextBoundary { get; } }
}
