using System.Collections;
using UnityEngine;
using ATEditor;
using Game.Logic;
using Game.VFX;
using Game.Logic.Combat.Pipeline;

namespace Game.Logic
{
    /// <summary>
    /// Visual hit-reaction module for a character entity.
    /// Combat resolution is handled by IHitImpact; this module focuses on
    /// reaction presentation such as VFX, audio, hit-stop, and state changes.
    /// </summary>
    public abstract class HitReactionModule : MonoBehaviour
    {
        [Header("受击保护")]
        public float hitProtectionInterval = 0.1f;

        [Header("受击转向")]
        public bool faceAttackerBeforeHitAnimation = true;

        [Header("霸体")]
        public bool isSuperArmor = false;

        protected CharacterEntity _entity;
        protected HitReactionRuntimeData _hitData;
        protected float _lastHitTime = -999f;

        public virtual void Init(CharacterEntity entity)
        {
            _entity = entity;
            _hitData = _entity.DataModule?.Get<HitReactionRuntimeData>();
        }

        private float CurrentLogicTime => TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;

        /// <summary>
        /// 校验并记录受击时间（受击保护内置 CD）
        /// </summary>
        public virtual bool ValidateAndRecordHit(float currentTime)
        {
            if (currentTime - _lastHitTime < hitProtectionInterval)
                return false;
            _lastHitTime = currentTime;
            return true;
        }

        /// <summary>
        /// 由命中流水线（MotionAndActionPipe）驱动的打断切入钩子
        /// </summary>
        public virtual void TriggerInterruptedHook(HitPipelineContext ctx)
        {
            var legacyCtx = new HitContext
            {
                attacker = ctx.Attacker,
                victim = ctx.Victim,
                hitEffectId = ctx.HitEffectId,
                interruptLevel = ctx.InterruptLevel,
                reactionType = ctx.SelectedReactionType,
                enableHitStop = ctx.EnableHitStop,
                hitStopDuration = ctx.HitStopDuration,
                hitStopScale = ctx.HitStopScale,
                hitVFXPrefab = ctx.HitVFXPrefab,
                hitVFXHeight = ctx.HitVFXHeight,
                hitVFXScale = ctx.HitVFXScale,
                hitVFXFollowTarget = ctx.HitVFXFollowTarget,
                hitAudioClip = ctx.HitAudioClip,
                hitStunDuration = ctx.HitStunDuration,
                hitPoint = ctx.HitPoint,
                hitDirection = ctx.HitDirection,
                reactionAxis = ctx.ReactionAxis,
                resolvedHitAction = ctx.ResolvedHitAction,
                requireFaceAttacker = ctx.RequireFaceAttacker
            };
            OnInterrupted(legacyCtx);
        }

        public virtual void ApplyVisualFeedback(HitContext ctx)
        {
            if (_entity == null) return;

            var pipeline = HitPipeline.Default;
            var pipeCtx = pipeline.AllocateContext();

            pipeCtx.Attacker = ctx.attacker;
            pipeCtx.Victim = _entity;
            pipeCtx.HitPoint = ctx.hitPoint;
            pipeCtx.HitDirection = ctx.hitDirection;
            pipeCtx.ReactionAxis = ctx.reactionAxis;
            pipeCtx.InterruptLevel = ctx.interruptLevel;
            pipeCtx.SelectedReactionType = ctx.reactionType;
            pipeCtx.EnableHitStop = ctx.enableHitStop;
            pipeCtx.HitStopDuration = ctx.hitStopDuration;
            pipeCtx.HitStopScale = ctx.hitStopScale;
            pipeCtx.HitVFXPrefab = ctx.hitVFXPrefab;
            pipeCtx.HitVFXScale = ctx.hitVFXScale;
            pipeCtx.HitVFXHeight = ctx.hitVFXHeight;
            pipeCtx.HitVFXFollowTarget = ctx.hitVFXFollowTarget;
            pipeCtx.HitAudioClip = ctx.hitAudioClip;
            pipeCtx.HitStunDuration = ctx.hitStunDuration;

            if (ctx.IsParry)
            {
                pipeCtx.ResultFlags |= Combat.Pipeline.HitResultFlags.Parried;
            }

            pipeline.Execute(pipeCtx);
            pipeline.ReleaseContext(pipeCtx);
        }

        public virtual void ApplyHitStopOnly(HitContext ctx)
        {
            if (_entity == null || !ctx.enableHitStop) return;
            ApplyHitStop(ctx);
        }

        protected abstract void OnInterrupted(HitContext ctx);

        protected virtual int GetCurrentResilience()
        {
            if (_entity.StatusModule != null && _entity.StatusModule.Attributes != null && _entity.StatusModule.Attributes.Has(Game.Logic.AttributeId.BaseResilience))
            {
                float currentRes = _entity.StatusModule.Attributes.GetCurrent(Game.Logic.AttributeId.BaseResilience);
                return Mathf.RoundToInt(currentRes);
            }
            return 1; // Default
        }

        private void FaceAttackerBeforeHitAnimation(HitContext ctx)
        {
            if (!faceAttackerBeforeHitAnimation || _entity == null)
            {
                return;
            }

            // 面对攻击袭来的方向（-hitDirection）
            Vector3 directionToAttacker = -ctx.hitDirection;
            directionToAttacker.y = 0f;

            // 若 hitDirection 为 0，降级为朝向攻击者实时位置
            if (directionToAttacker.sqrMagnitude <= 0.0001f)
            {
                if (ctx.attacker != null)
                {
                    directionToAttacker = ctx.attacker.transform.position - transform.position;
                    directionToAttacker.y = 0f;
                }
            }

            if (directionToAttacker.sqrMagnitude > 0.0001f)
            {
                if (_entity.CharacterMotor != null)
                {
                    _entity.CharacterMotor.FaceToImmediately(directionToAttacker.normalized);
                }
                else
                {
                    transform.forward = directionToAttacker.normalized;
                }
            }
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

            Transform parent = ctx.hitVFXFollowTarget ? transform : null;
            GameObject vfx = VFXManager.Instance.Spawn(ctx.hitVFXPrefab, spawnPos, spawnRot, parent);
            if (vfx != null)
            {
                vfx.transform.localScale = ctx.hitVFXScale;
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

        public void ApplyMultiHit(float duration, int times, System.Action<int, int> hitAction)
        {
            StartCoroutine(MultiHitCoroutine(duration, times, hitAction));
        }

        private IEnumerator MultiHitCoroutine(float duration, int times, System.Action<int, int> hitAction)
        {
            if (times <= 0 || duration <= 0) yield break;

            float interval = duration / times;
            for (int i = 0; i < times; i++)
            {
                if (_entity == null) yield break;
                hitAction?.Invoke(i, times);
                yield return new WaitForLogicSeconds(interval);
            }
        }

        private void ApplyHitStop(HitContext ctx)
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.RegisterHitStop(ctx.attacker?.ActionPlayer, _entity?.ActionPlayer, ctx.hitStopDuration, ctx.hitStopScale);
            }
            else
            {
                ctx.attacker?.ActionPlayer?.SetPlaySpeed(ctx.hitStopScale);
                _entity?.ActionPlayer?.SetPlaySpeed(ctx.hitStopScale);
            }
        }
    }
}
