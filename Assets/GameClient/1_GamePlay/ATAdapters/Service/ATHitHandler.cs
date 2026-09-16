using System.Collections.Generic;
using UnityEngine;
using ATEditor;


namespace Game.GamePlay
{
    /// <summary>
    /// ATEditor 命中检测适配器（桥接 HitData 与 HitPipeline 管道架构）
    /// </summary>
    public class ATHitHandler : IHitHandler
    {
        private static readonly HashSet<CharacterEntity> _processedVictimsCache = new(8);

        public void OnHitDetect(HitData hitData)
        {
            if (hitData.targetsCollilders == null || hitData.targetsCollilders.Length == 0) return;
            if (hitData.hitEffectId <= 0)
            {
                return;
            }

            var hitEffectConfig = ConfigManager.Instance?.Tables?.TbHitEffect?.GetOrDefault(hitData.hitEffectId);
            if (hitEffectConfig == null || hitEffectConfig.Effects == null)
            {
                return;
            }

            // 获取攻击者实体
            CharacterEntity attacker = null;
            if (hitData.deployer != null)
            {
                attacker = hitData.deployer.GetComponent<CharacterEntity>();
            }

            _processedVictimsCache.Clear();
            try
            {
                var threatSession = attacker != null ? CombatWarningManager.GetActiveThreatSession(attacker) : null;

                foreach (var collider in hitData.targetsCollilders)
                {
                    if (collider == null) continue;

                    var victim = collider.GetComponentInParent<CharacterEntity>();
                    if (victim == null) continue;

                    // 防止同一个实体身上的多个 Collider 被同时打中导致触发多次重复命中
                    if (!_processedVictimsCache.Add(victim)) continue;

                    // 检查该受击者是否已在此攻击威胁会话中被豁免（例如已触发极限闪避）
                    if (threatSession != null && threatSession.HasResolvedFor(victim)) continue;

                // 计算碰撞点和攻击方向
                Vector3 attackerPos = hitData.deployer != null ? hitData.deployer.transform.position : Vector3.zero;
                Vector3 hitBoxPos = hitData.hitBoxCenter;
                Vector3 hitPoint = collider.ClosestPoint(hitBoxPos);

                Vector3 hitDirection = Vector3.forward;
                switch (hitData.hitDirectionMode)
                {
                    case HitDirectionMode.BoxToTarget:
                        hitDirection = (victim.transform.position - hitBoxPos);
                        hitDirection.y = 0f;
                        if (hitDirection.sqrMagnitude < 0.0001f)
                            hitDirection = victim.transform.position - attackerPos;
                        break;

                    case HitDirectionMode.AttackerToTarget:
                        hitDirection = (victim.transform.position - attackerPos);
                        hitDirection.y = 0f;
                        break;

                    case HitDirectionMode.OnEnterCustomRelative:
                        hitDirection = hitData.customWorldDirection;
                        hitDirection.y = 0f;
                        break;
                }

                if (hitDirection.sqrMagnitude < 0.0001f)
                {
                    hitDirection = hitData.deployer != null ? hitData.deployer.transform.forward : Vector3.forward;
                }
                hitDirection.Normalize();

                // 从管道上下文池分配并填充数据
                var pipeline = HitPipeline.Default;
                var ctx = pipeline.AllocateContext();

                ctx.Attacker = attacker;
                ctx.Victim = victim;
                ctx.HitCollider = collider;
                ctx.RawHitData = hitData;
                ctx.HitEffectConfig = hitEffectConfig;
                ctx.ThreatSession = threatSession;

                ctx.HitPoint = hitPoint;
                ctx.HitDirection = hitDirection;
                ctx.ReactionAxis = -hitDirection;

                ctx.InterruptLevel = ActionResilienceHelper.GetInterruptLevel(attacker);
                ctx.EnableHitStop = hitData.enableHitStop;
                ctx.HitStopDuration = hitData.hitStopDuration;
                ctx.HitStopScale = hitData.hitStopScale;
                ctx.HitVFXPrefab = hitData.hitVFXPrefab;
                ctx.HitVFXScale = hitData.hitVFXScale;
                ctx.HitVFXHeight = hitData.hitVFXHeight;
                ctx.HitVFXFollowTarget = hitData.followTarget;
                ctx.HitAudioClip = hitData.hitAudioClip;
                ctx.HitStunDuration = hitData.hitStunDuration;

                // 多段打击分派驱动（由攻击端驱动；若无有效攻击者宿主则降级单次同步结算）
                if (hitData.hitMode == HitMode.Times && hitData.multiHitCount > 1)
                {
                    MonoBehaviour runner = attacker != null && attacker.gameObject.activeInHierarchy
                        ? (MonoBehaviour)attacker
                        : null;
                    MultiHitDriver.RunMultiHit(runner, pipeline, ctx, hitData.multiHitDuration, hitData.multiHitCount);
                }
                else
                {
                    ctx.CurrentHitIndex = 0;
                    ctx.TotalHitCount = 1;
                    pipeline.Execute(ctx);
                    pipeline.ReleaseContext(ctx);
                }
            }
            }
            finally
            {
                _processedVictimsCache.Clear();
            }
        }

        public void AppendExtraHitTargets(GameObject deployer, List<Collider> outHits)
        {
            if (deployer == null || outHits == null) return;
            var attackerEntity = deployer.GetComponent<CharacterEntity>();
            if (attackerEntity != null)
            {
                var contract = CombatWarningManager.GetActiveContract(attackerEntity);
                if (contract != null && contract.IsValid && contract.ParryRole != null)
                {
                    var parryData = contract.ParryRole.DataModule?.Get<ParryRuntimeData>();
                    if (parryData != null && parryData.IsParrying)
                    {
                        var roleCol = contract.ParryRole.GetComponent<Collider>() ?? contract.ParryRole.GetComponentInChildren<Collider>();
                        if (roleCol != null && !outHits.Contains(roleCol))
                        {
                            outHits.Add(roleCol);
                        }
                    }
                }
            }
        }
    }
}
