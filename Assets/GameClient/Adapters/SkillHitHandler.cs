using SkillEditor;
using UnityEngine;
using Game.Logic.Character;
using System;
using System.Linq;


namespace Game.Adapters
{
    public class SkillHitHandler : ISkillHitHandler
    {
        public void OnHitDetect(HitData hitData)
        {
            if (hitData.targetsCollilders == null || hitData.targetsCollilders.Length == 0) return;
            if(hitData.hitEffects == null || hitData.hitEffects.Length == 0) return;
            // 获取攻击者实体
            CharacterEntity attacker = null;
            if (hitData.deployer != null)
            {
                attacker = hitData.deployer.GetComponent<CharacterEntity>();
            }

            foreach (var collider in hitData.targetsCollilders)
            {
                if (collider == null) continue;

                var victim = collider.GetComponentInParent<CharacterEntity>();
                if (victim == null) continue;

                // 封装单次打击逻辑
                System.Action applySingleHit = () =>
                {
                    if (victim == null || collider == null) return;

                    // 计算碰撞点和攻击方向
                    Vector3 attackerPos = hitData.deployer != null ? hitData.deployer.transform.position : Vector3.zero;
                    Vector3 hitBoxPos = hitData.hitBoxCenter;
                    Vector3 hitPoint = collider.ClosestPoint(hitBoxPos);
                    Vector3 hitDirection = (victim.transform.position - hitBoxPos).normalized;

                    // 构建 HitContext
                    var ctx = new HitContext
                    {
                        attacker = attacker,
                        victim = victim,
                        hitEffects = hitData.hitEffects,
                        enableHitStop = hitData.enableHitStop,
                        hitStopDuration = hitData.hitStopDuration,
                        hitVFXPrefab = hitData.hitVFXPrefab,
                        hitVFXHeight = hitData.hitVFXHeight,
                        hitVFXScale = hitData.hitVFXScale,
                        hitVFXFollowTarget = hitData.followTarget,
                        hitAudioClip = hitData.hitAudioClip,
                        hitStunDuration = hitData.hitStunDuration,
                        hitPoint = hitPoint,
                        hitDirection = hitDirection,
                        reactionAxis = -hitDirection // 默认受击轴为攻击反方向
                    };

                    // 执行 Impacts
                    foreach (var entry in hitData.hitEffects)
                    {
                        if (entry == null) continue;
                        if (entry.targetTags.Contains(victim.tag))
                        {
                            var impact = HitImpactRegistry.Resolve(entry.eventTag);
                            impact?.Execute(ctx, entry);
                        }
                    }
                    Debug.Log($"<color=orange>[Hit] {hitData.deployer?.name} → {collider.gameObject.name} (Mode:{hitData.hitMode})</color>");
                };

                // 分发策略
                if (hitData.hitMode == HitMode.Times)
                {
                    var hitModule = victim.GetComponent<HitReactionModule>();
                    if (hitModule != null)
                    {
                        hitModule.ApplyMultiHit(hitData.multiHitDuration, hitData.multiHitCount, applySingleHit);
                    }
                    else
                    {
                        applySingleHit();
                    }
                }
                else
                {
                    applySingleHit();
                }
            }
        }
    }
}
