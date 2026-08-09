using ATEditor;
using UnityEngine;
using Game.Logic;
using System;


namespace Game.Adapters
{
    public class ATHitHandler : IHitHandler
    {
        public void OnHitDetect(HitData hitData)
        {
            if (hitData.targetsCollilders == null || hitData.targetsCollilders.Length == 0) return;
            if (hitData.hitEffectId <= 0) return;
            var hitEffectConfig = ConfigManager.Instance.Tables.TbHitEffect.GetOrDefault(hitData.hitEffectId);
            if (hitEffectConfig == null || hitEffectConfig.Effects == null) return;

            // 获取攻击者实体
            CharacterEntity attacker = null;
            if (hitData.deployer != null)
            {
                attacker = hitData.deployer.GetComponent<CharacterEntity>();
            }

            var processedVictims = new System.Collections.Generic.HashSet<CharacterEntity>();

            foreach (var collider in hitData.targetsCollilders)
            {
                if (collider == null) continue;

                var victim = collider.GetComponentInParent<CharacterEntity>();
                if (victim == null) continue;
                
                // 防止同一个实体身上的多个 Collider 被同时打中导致触发多次命中
                if (!processedVictims.Add(victim)) continue;

                // 封装单次打击逻辑
                System.Action<int, int> applySingleHit = (currentHit, totalHits) =>
                {
                    if (victim == null || collider == null) return;

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

                    // 构建 HitContext
                    var ctx = new HitContext
                    {
                        attacker = attacker,
                        victim = victim,
                        hitEffectId = hitData.hitEffectId,
                        enableHitStop = hitData.enableHitStop,
                        hitStopDuration = hitData.hitStopDuration,
                        hitStopScale = hitData.hitStopScale,
                        hitVFXPrefab = hitData.hitVFXPrefab,
                        hitVFXHeight = hitData.hitVFXHeight,
                        hitVFXScale = hitData.hitVFXScale,
                        hitVFXFollowTarget = hitData.followTarget,
                        hitAudioClip = hitData.hitAudioClip,
                        hitStunDuration = hitData.hitStunDuration,
                        hitPoint = hitPoint,
                        hitDirection = hitDirection,
                        reactionAxis = -hitDirection
                    };

                    // ★ 数据驱动的效果分派
                    foreach (var effect in hitEffectConfig.Effects)
                    {
                        if (effect == null) continue;

                        // 触发概率过滤
                        if (effect.Chance < 1f && UnityEngine.Random.value > effect.Chance) continue;

                        // 根据多段触发策略过滤（FirstHitOnly 仅首段触发，EveryHit 每段都触发）
                        if (effect.TriggerPolicy == cfg.ZZZ.EffectTriggerPolicy.FirstHitOnly && currentHit > 0) continue;

                        // 根据 EffectTarget 决定操作谁
                        CharacterEntity targetEntity = effect.EffectTarget == cfg.ZZZ.EffectTarget.Attacker
                            ? attacker
                            : victim;

                        if (targetEntity == null) continue;

                        switch (effect.EffectType)
                        {
                            case cfg.ZZZ.HitEffectType.Damage:
                                ApplyDamage(ctx, targetEntity, effect.Value, 0f);
                                break;

                            case cfg.ZZZ.HitEffectType.ModifyAttribute:
                                ApplyModifyAttribute(targetEntity, (AttributeId)effect.AttrId, effect.Value);
                                break;

                            case cfg.ZZZ.HitEffectType.ApplyBuff:
                                if (effect.BuffId > 0)
                                {
                                    Debug.Log($"<color=green>TODO: 需要 Buff 查表把 ID {effect.BuffId} 转成 Buff 并施加给 {targetEntity.name}</color>");
                                }
                                break;
                        }
                    }

                    // 视觉反馈（始终对 victim 执行）
                    victim.HitReactionModule?.ApplyVisualFeedback(ctx);
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
                        applySingleHit(0, hitData.multiHitCount > 0 ? hitData.multiHitCount : 1);
                    }
                }
                else
                {
                    applySingleHit(0, 1);
                }
            }
        }

        /// <summary>造成伤害（扣 HP + 累积喧响值）</summary>
        private void ApplyDamage(HitContext ctx, CharacterEntity targetEntity, float baseDamage, float dazeAmount)
        {
            var statusModule = targetEntity.StatusModule;
            if (statusModule == null) return;

            // 伤害计算：基础伤害 + ATK - DEF，最低 1
            float atk = 0f;
            float def = 0f;

            if (ctx.attacker?.StatusModule?.Attributes != null)
            {
                if (ctx.attacker.StatusModule.Attributes.Has(AttributeId.ATK))
                    atk = ctx.attacker.StatusModule.Attributes.GetCurrent(AttributeId.ATK);
            }

            if (targetEntity.StatusModule?.Attributes != null)
            {
                if (targetEntity.StatusModule.Attributes.Has(AttributeId.DEF))
                    def = targetEntity.StatusModule.Attributes.GetCurrent(AttributeId.DEF);
            }

            float damage = Mathf.Max(1f, baseDamage + atk - def);

            if (statusModule.Attributes.Has(AttributeId.HP))
            {
                statusModule.Attributes.Modify(AttributeId.HP, -damage);
                Debug.Log($"<color=yellow>[DamageImpact] {ctx.attacker?.name} → {targetEntity.name} | " +
                          $"DMG:{damage:F0} (Base:{baseDamage}) | HP:{statusModule.Attributes.GetCurrent(AttributeId.HP):F0}</color>");
            }

            // 喧响值累积
            if (dazeAmount > 0f && statusModule.Attributes.Has(AttributeId.Decibel))
            {
                statusModule.Attributes.Modify(AttributeId.Decibel, +dazeAmount);
            }
        }

        /// <summary>修改任意属性</summary>
        private void ApplyModifyAttribute(CharacterEntity targetEntity, AttributeId attributeId, float delta)
        {
            if (targetEntity.StatusModule?.Attributes == null) return;
            if (!targetEntity.StatusModule.Attributes.Has(attributeId)) return;

            targetEntity.StatusModule.Attributes.Modify(attributeId, delta);
        }

        /// <summary>施加 Buff</summary>
        private void ApplyBuff(CharacterEntity targetEntity, BuffDefAsset buffDef, CharacterEntity source)
        {
            if (buffDef == null || targetEntity.StatusModule == null) return;

            if (targetEntity.StatusModule.IsBuffImmune(buffDef))
            {
                Debug.Log($"<color=grey>[ApplyBuff] {targetEntity.name} 免疫 Buff '{buffDef.DisplayName}'</color>");
                return;
            }

            targetEntity.StatusModule.Buffs.AddBuff(buffDef, source);
            Debug.Log($"<color=green>[ApplyBuff] {source?.name} → {targetEntity.name} | 施加 Buff '{buffDef.DisplayName}'</color>");
        }
    }
}
