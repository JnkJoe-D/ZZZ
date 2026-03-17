using System.Collections;
using UnityEngine;
using SkillEditor;
using Game.Logic.Character.Config;
using Game.VFX;

namespace Game.Logic.Character
{
    /// <summary>
    /// 受击反应模块（视觉层）。可选挂载在任何 CharacterEntity 派生体上。
    /// 仅负责视觉反馈：受击特效、顿帧、状态机切换。
    /// 逻辑分发（扣血/加血/击退/Buff）由 IHitImpact 体系负责。
    /// </summary>
    public class HitReactionModule : MonoBehaviour
    {
        [Header("受击配置")]
        [Tooltip("角色级别的受击配置（动画变体/击退等）")]
        public HitReactionConfig reactionConfig;

        [Header("特效挂点覆盖")]
        [Tooltip("启用则使用固定挂点，否则使用动态碰撞点")]
        public bool useFixedHitPoint = false;
        public BindPoint fixedBindPoint = BindPoint.Body;

        [Header("受击保护")]
        [Tooltip("两次受击硬直之间的最小间隔")]
        public float hitProtectionInterval = 0.1f;

        [Header("霸体状态")]
        public bool isSuperArmor = false;

        private CharacterEntity _entity;
        private float _lastHitTime = -999f;

        public void Init(CharacterEntity entity)
        {
            _entity = entity;
        }

        /// <summary>
        /// 应用视觉反馈。由 IHitImpact 的具体实现调用。
        /// 职责：特效 → 顿帧 → 状态机切换。
        /// </summary>
        public void ApplyVisualFeedback(HitContext ctx)
        {
            if (_entity == null) return;

            // 受击保护检查
            if (Time.time - _lastHitTime < hitProtectionInterval) return;
            _lastHitTime = Time.time;

            // 1. 生成受击特效
            SpawnHitVFX(ctx);

            // 2. 播放受击音效
            PlayHitAudio(ctx);

            // 3. 顿帧
            if (ctx.enableHitStop)
            {
                ApplyHitStop(ctx);
            }

            // 4. 写入受击参数到 RuntimeData
            _entity.RuntimeData.CurrentHitStunDuration = ctx.hitStunDuration;

            // 5. 切换状态机（霸体状态下不进入硬直）
            if (!isSuperArmor)
            {
                _entity.StateMachine?.ChangeState<CharacterHitStunState>();
            }
        }

        private void SpawnHitVFX(HitContext ctx)
        {
            if (ctx.hitVFXPrefab == null) return;

            Vector3 spawnPos = ctx.hitPoint;
            
            // XZ使用碰撞点，Y使用角色根坐标高度 + 偏移高度
            spawnPos.y = _entity.transform.position.y + ctx.hitVFXHeight;

            // 默认面向主相机（Billboard）
            Quaternion spawnRot = Quaternion.identity;
            if (UnityEngine.Camera.main != null)
            {
                Vector3 camForward = UnityEngine.Camera.main.transform.forward;
                spawnRot = Quaternion.LookRotation(-camForward);
            }

            var vfx = VFXManager.Instance.Spawn(ctx.hitVFXPrefab, spawnPos, spawnRot);
            if (vfx != null)
            {
                vfx.transform.localScale = ctx.hitVFXScale;
            }
            VFXManager.Instance.ReturnWhenDone(vfx);
        }

        private void PlayHitAudio(HitContext ctx)
        {
            if (ctx.hitAudioClip == null) return;

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
                Game.Audio.AudioManager.Instance.PlayAudio(ctx.hitAudioClip, Game.Audio.AudioChannel.SFX, args);
            }
        }

        private void ApplyHitStop(HitContext ctx)
        {
            // 冻结攻击者
            ctx.attacker?.ActionPlayer?.SetPlaySpeed(0f);
            // 冻结受击者
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
