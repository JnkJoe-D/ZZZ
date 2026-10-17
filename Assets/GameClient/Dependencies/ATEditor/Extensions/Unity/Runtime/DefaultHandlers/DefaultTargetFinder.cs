using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 目标寻找器契约接口：为时间轴内的移动、转向、镜头等变换提供目标交互引用。
    /// 业务层（如角色索敌模块）可实现此接口并通过 Context 注入，覆盖默认物理探测行为。
    /// </summary>
    public interface IATTargetFinder
    {
        /// <summary>根据宿主对象寻找当前交互目标。</summary>
        /// <param name="owner">动作宿主 GameObject</param>
        /// <returns>找到的目标 Transform，无目标时返回 null</returns>
        Transform FindTarget(GameObject owner);
    }

    /// <summary>
    /// 默认物理目标寻找器辅助类：基于非分配物理重叠球探测视野范围内的交互目标。
    /// 适用于无外部业务索敌系统接入时的独立演示与开箱即用场景。
    /// </summary>
    public sealed class DefaultTargetFinder : IATTargetFinder
    {
        private readonly Collider[] _hitBuffer;
        private readonly float _searchRadius;
        private readonly LayerMask _layerMask;

        public float SearchRadius => _searchRadius;
        public LayerMask LayerMask => _layerMask;

        public DefaultTargetFinder(float searchRadius = 15f, LayerMask? layerMask = null, int bufferSize = 32)
        {
            _searchRadius = Mathf.Max(0.1f, searchRadius);
            _layerMask = layerMask ?? ~0;
            _hitBuffer = new Collider[Mathf.Max(8, bufferSize)];
        }

        public Transform FindTarget(GameObject owner)
        {
            if (owner == null) return null;
            Transform ownerTransform = owner.transform;
            Vector3 ownerPosition = ownerTransform.position;
            Vector3 ownerForward = ownerTransform.forward;

            int hitCount = Physics.OverlapSphereNonAlloc(ownerPosition, _searchRadius, _hitBuffer, _layerMask, QueryTriggerInteraction.Ignore);
            if (hitCount <= 0) return null;

            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                if (hit == null || !hit.enabled) continue;

                // 排除宿主自身及其子层级碰撞体
                Transform hitTransform = hit.transform;
                if (hitTransform == ownerTransform || hitTransform.IsChildOf(ownerTransform))
                    continue;

                // 优先归拢至实体根部（attachedRigidbody 或顶层根）
                Transform candidate = hit.attachedRigidbody != null
                    ? hit.attachedRigidbody.transform
                    : hitTransform.root;

                if (candidate == ownerTransform || candidate == null)
                    continue;

                Vector3 toCandidate = candidate.position - ownerPosition;
                toCandidate.y = 0f; // 在水平面评估距离与朝向
                float sqrDist = toCandidate.sqrMagnitude;
                if (sqrDist < 0.0001f) continue;

                // 综合评分：优先在前方（角度惩罚因子），其次距离最近
                float dot = toCandidate.magnitude > 0.001f ? Vector3.Dot(ownerForward, toCandidate.normalized) : 0f;
                // dot 范围 [-1, 1]；若在正前方 dot=1，角度惩罚为 1；若在正后方 dot=-1，角度惩罚为 3
                float anglePenalty = 2f - dot;
                float score = sqrDist * anglePenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            // 清理 buffer 引用避免持久泄漏
            Array.Clear(_hitBuffer, 0, hitCount);

            return bestTarget;
        }
    }
}
