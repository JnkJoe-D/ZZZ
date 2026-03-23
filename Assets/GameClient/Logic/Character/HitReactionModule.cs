using System.Collections;
using UnityEngine;
using SkillEditor;
using Game.Logic.Character.Config;
using Game.VFX;

namespace Game.Logic.Character
{
    /// <summary>
    /// Visual hit-reaction module for a character entity.
    /// Combat resolution is handled by IHitImpact; this module focuses on
    /// reaction presentation such as VFX, audio, hit-stop, and state changes.
    /// </summary>
    public class HitReactionModule : MonoBehaviour
    {
        [Header("Hit Protection")]
        [Tooltip("Minimum interval between two hit-stun reactions.")]
        public float hitProtectionInterval = 0.1f;

        [Header("Hit Facing")]
        [Tooltip("Rotate the victim to face the attacker before playing the hurt animation.")]
        public bool faceAttackerBeforeHitAnimation = true;

        [Header("Super Armor")]
        public bool isSuperArmor = false;

        private CharacterEntity _entity;
        private float _lastHitTime = -999f;

        public void Init(CharacterEntity entity)
        {
            _entity = entity;
        }

        /// <summary>
        /// Apply presentation feedback for a resolved hit.
        /// Flow: VFX -> audio -> hit-stop -> write stun data -> switch state.
        /// </summary>
        public void ApplyVisualFeedback(HitContext ctx)
        {
            if (_entity == null)
            {
                return;
            }

            if (Time.time - _lastHitTime < hitProtectionInterval)
            {
                return;
            }

            _lastHitTime = Time.time;

            SpawnHitVFX(ctx);
            PlayHitAudio(ctx);

            if (ctx.enableHitStop)
            {
                ApplyHitStop(ctx);
            }

            _entity.RuntimeData.CurrentHitStunDuration = ctx.hitStunDuration;
            _entity.RuntimeData.SetHitReactionAxis(ctx.reactionAxis);

            if (!isSuperArmor)
            {
                FaceAttackerBeforeHitAnimation(ctx);
                _entity.StateMachine?.ChangeState<CharacterHitStunState>();
            }
        }

        private void FaceAttackerBeforeHitAnimation(HitContext ctx)
        {
            if (!faceAttackerBeforeHitAnimation || _entity == null)
            {
                return;
            }

            if (ctx.attacker != null)
            {
                _entity.MovementController?.FaceToTargetImmediately(ctx.attacker.transform);
                return;
            }

            Vector3 directionToAttacker = -ctx.reactionAxis;
            directionToAttacker.y = 0f;
            if (directionToAttacker.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.forward = directionToAttacker.normalized;
        }

        private void SpawnHitVFX(HitContext ctx)
        {
            if (ctx.hitVFXPrefab == null)
            {
                return;
            }

            Vector3 spawnPos = ctx.hitPoint;
            spawnPos.y = _entity.transform.position.y + ctx.hitVFXHeight;

            Quaternion spawnRot = Quaternion.identity;
            if (UnityEngine.Camera.main != null)
            {
                Vector3 camForward = UnityEngine.Camera.main.transform.forward;
                spawnRot = Quaternion.LookRotation(-camForward);
            }

            GameObject vfx = VFXManager.Instance.Spawn(ctx.hitVFXPrefab, spawnPos, spawnRot);
            if (vfx != null)
            {
                vfx.transform.localScale = ctx.hitVFXScale;
                if (ctx.hitVFXFollowTarget)
                {
                    vfx.transform.SetParent(transform);
                }
            }

            VFXManager.Instance.ReturnWhenDone(vfx);
        }

        private void PlayHitAudio(HitContext ctx)
        {
            if (ctx.hitAudioClip == null)
            {
                return;
            }

            if (Game.Audio.AudioManager.Instance != null)
            {
                Vector3 soundPos = ctx.hitPoint;
                soundPos.y = _entity.transform.position.y + ctx.hitVFXHeight;

                var args = new Game.Audio.AudioArgs
                {
                    position = soundPos,
                    spatialBlend = 1f,
                    volume = 1f,
                    pitch = 1f
                };

                Game.Audio.AudioManager.Instance.PlayAudio(
                    ctx.hitAudioClip,
                    Game.Audio.AudioChannel.SFX,
                    args);
            }
        }

        public void ApplyMultiHit(float duration, int times, System.Action hitAction)
        {
            StartCoroutine(MultiHitCoroutine(duration, times, hitAction));
        }

        private IEnumerator MultiHitCoroutine(float duration, int times, System.Action hitAction)
        {
            if (times <= 0 || duration <= 0) yield break;

            float interval = duration / times;
            for (int i = 0; i < times; i++)
            {
                if (_entity == null) yield break;
                hitAction?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }

        private void ApplyHitStop(HitContext ctx)
        {
            ctx.attacker?.ActionPlayer?.SetPlaySpeed(0f);
            _entity.ActionPlayer?.SetPlaySpeed(0f);

            StartCoroutine(RestoreAfterHitStop(ctx));
        }

        private IEnumerator RestoreAfterHitStop(HitContext ctx)
        {
            yield return new WaitForSecondsRealtime(ctx.hitStopDuration);

            ctx.attacker?.ActionPlayer?.SetPlaySpeed(1f);
            _entity.ActionPlayer?.SetPlaySpeed(1f);
        }
    }
}
