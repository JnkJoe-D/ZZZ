using UnityEngine;

namespace ATEditor
{
    /// <summary>旧 HitClip/HitData 的兼容转换；通用 AttackObservation 不持有这些业务字段。</summary>
    public static class LegacyHitDataFactory
    {
        public static HitData Create(GameObject owner, Vector3 center, Collider[] targets, DetectConfig config,
            HitDirectionMode direction = HitDirectionMode.AttackerToTarget, Vector2 customDirection = default,
            Vector3 customWorldDirection = default)
        {
            config = config ?? new DetectConfig();
            return new HitData
            {
                deployer = owner, hitBoxCenter = center, targetsCollilders = targets, hitEffectId = config.hitEffectId,
                hitDirectionMode = direction, customHitDirection = customDirection, customWorldDirection = customWorldDirection,
                hitMode = config.hitMode, multiHitCount = config.multiHitCount, multiHitDuration = config.multiHitDuration,
                enableHitStop = config.enableHitStop, hitStopDuration = config.hitStopDuration, hitStopScale = config.hitStopScale,
                hitVFXPrefab = config.hitVFXPrefab, hitVFXHeight = config.hitVFXHeight, hitVFXScale = config.hitVFXScale,
                hitAudioClip = config.hitAudioClip, hitStunDuration = config.hitStunDuration, followTarget = config.followTarget
            };
        }
    }
}
