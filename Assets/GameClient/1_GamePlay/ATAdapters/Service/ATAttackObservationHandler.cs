using System;
using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>将项目无关的几何观察交给现有 ZZZ 结算管线；配表绑定由外部按动作/片段/采样轮次提供。</summary>
    public sealed class ATAttackObservationHandler : IAttackObservationHandler
    {
        private readonly Func<AttackObservation, DetectConfig> _resolveEffect;
        private readonly IHitHandler _combat;

        public ATAttackObservationHandler(Func<AttackObservation, DetectConfig> resolveEffect, IHitHandler combat = null)
        {
            _resolveEffect = resolveEffect ?? throw new ArgumentNullException(nameof(resolveEffect));
            _combat = combat ?? new ATHitHandler();
        }

        public void OnObserved(AttackObservation observation)
        {
            if (observation.Owner == null || observation.Collider == null || observation.Target == null) return;
            var effect = _resolveEffect(observation);
            // 无绑定时仅保留观察，不猜测配表 ID，也不执行伤害。
            if (effect == null || effect.hitEffectId <= 0) return;
            _combat.OnHitDetect(LegacyHitDataFactory.Create(observation.Owner, observation.QueryCenter,
                new[] { observation.Collider }, effect));
        }
    }

    /// <summary>在去重、排序和目标限额之前识别 CharacterEntity；阵营/免疫政策仍由项目决定。</summary>
    public sealed class ATAttackTargetResolver : IAttackTargetResolver
    {
        public UnityEngine.Object ResolveTarget(GameObject owner, Collider collider) => collider.GetComponentInParent<CharacterEntity>();
    }
}
