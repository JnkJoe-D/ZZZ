using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic.Combat.Pipeline.Pipes
{
    /// <summary>
    /// 数值计算与生命周期结算过滤器（计算伤害、属性变更、致死判定）
    /// </summary>
    public class DamageCalculationPipe : IHitPipe
    {
        public string PipeName => "DamageCalculationPipe";
        public int Priority => 300;

        public void Process(HitPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.HitEffectConfig == null || ctx.HitEffectConfig.Effects == null)
                return;

            foreach (var effect in ctx.HitEffectConfig.Effects)
            {
                if (effect == null) continue;

                // 1. 触发概率过滤
                if (effect.Chance < 1f && Random.value > effect.Chance) continue;

                // 2. 多段触发策略过滤（FirstHitOnly 仅首段触发，EveryHit 每段都触发）
                if (effect.TriggerPolicy == EffectTriggerPolicy.FirstHitOnly && ctx.CurrentHitIndex > 0) continue;

                // 3. 作用目标分发
                CharacterEntity targetEntity = effect.EffectTarget == EffectTarget.Attacker
                    ? ctx.Attacker
                    : ctx.Victim;

                if (targetEntity == null || targetEntity.StatusModule == null) continue;

                switch (effect.EffectType)
                {
                    case HitEffectType.Damage:
                        ApplyDamage(ctx, targetEntity, effect.Value);
                        break;

                    case HitEffectType.ModifyAttribute:
                        ApplyModifyAttribute(targetEntity, (AttributeId)effect.AttrId, effect.Value);
                        break;

                    case HitEffectType.ApplyBuff:
                        if (effect.BuffId > 0)
                        {
                            Debug.Log($"[DamageCalculationPipe] 命中触发施加 Buff ID: {effect.BuffId} -> {targetEntity.name}");
                        }
                        break;
                }
            }
        }

        private void ApplyDamage(HitPipelineContext ctx, CharacterEntity target, float baseDamage)
        {
            var attributes = target.StatusModule?.Attributes;
            if (attributes == null) return;

            float atk = 0f;
            float def = 0f;

            if (ctx.Attacker?.StatusModule?.Attributes != null && ctx.Attacker.StatusModule.Attributes.Has(AttributeId.ATK))
            {
                atk = ctx.Attacker.StatusModule.Attributes.GetCurrent(AttributeId.ATK);
            }

            if (attributes.Has(AttributeId.DEF))
            {
                def = attributes.GetCurrent(AttributeId.DEF);
            }

            float damage = Mathf.Max(1f, baseDamage + atk - def);
            ctx.FinalDamage += damage;

            if (attributes.Has(AttributeId.HP))
            {
                attributes.Modify(AttributeId.HP, -damage);
                ctx.ResultFlags |= HitResultFlags.Damaged;

                Debug.Log($"<color=orange>[HitPipeline] 命中成功: {ctx.Attacker?.name} → {target.name} | 造成伤害: {damage:F0} (基础: {baseDamage}) | 目标剩余HP: {attributes.GetCurrent(AttributeId.HP):F0}</color>");

                // 致死判定与通用生命周期结算
                if (attributes.GetCurrent(AttributeId.HP) <= 0f && !target.IsDead)
                {
                    ctx.ResultFlags |= HitResultFlags.Killed;
                    target.LifecycleModule?.Die(null);
                }
            }
        }

        private void ApplyModifyAttribute(CharacterEntity target, AttributeId attrId, float value)
        {
            var attributes = target.StatusModule?.Attributes;
            if (attributes != null && attributes.Has(attrId))
            {
                attributes.Modify(attrId, value);
            }
        }
    }
}
