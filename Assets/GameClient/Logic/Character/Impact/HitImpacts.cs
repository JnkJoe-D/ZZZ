using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 默认伤害 Impact。用于 Hit_Default、Hit_Light 等通用伤害类型。
    /// 执行：扣血 + 受击视觉反馈。
    /// </summary>
    public class DefaultDamageImpact : IHitImpact
    {
        public virtual void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry)
        {
            // TODO: 数值计算 — 从攻击者属性和技能配置计算伤害值
            // TODO: 扣血 — victim.Health -= calculatedDamage;

            // 视觉反馈交给 HitReactionModule（如果存在）
            var hitModule = ctx.victim?.GetComponent<HitReactionModule>();
            hitModule?.ApplyVisualFeedback(ctx);

            Debug.Log($"<color=yellow>[Impact] DefaultDamage: {ctx.attacker?.name} → {ctx.victim?.name} | Tag:{entry.eventTag}</color>");
        }
    }

    /// <summary>
    /// 重击 Impact。更大的伤害 + 更长的硬直。
    /// </summary>
    public class HeavyDamageImpact : IHitImpact
    {
        public void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry)
        {
            // TODO: 重击伤害倍率
            // TODO: 更长的硬直时长覆盖

            var hitModule = ctx.victim?.GetComponent<HitReactionModule>();
            hitModule?.ApplyVisualFeedback(ctx);

            Debug.Log($"<color=red>[Impact] HeavyDamage: {ctx.attacker?.name} → {ctx.victim?.name} | Tag:{entry.eventTag}</color>");
        }
    }

    /// <summary>
    /// 击退 Impact。伤害 + 击退位移。
    /// </summary>
    public class KnockbackImpact : IHitImpact
    {
        public void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry)
        {
            // TODO: 伤害计算
            // TODO: 通过 MovementController 施加击退力
            // ctx.victim?.MovementController?.ApplyForce(ctx.hitDirection * knockbackForce);

            var hitModule = ctx.victim?.GetComponent<HitReactionModule>();
            hitModule?.ApplyVisualFeedback(ctx);

            Debug.Log($"<color=cyan>[Impact] Knockback: {ctx.attacker?.name} → {ctx.victim?.name} | Tag:{entry.eventTag}</color>");
        }
    }

    /// <summary>
    /// 击飞 Impact。伤害 + 垂直方向位移。
    /// </summary>
    public class LaunchImpact : IHitImpact
    {
        public void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry)
        {
            // TODO: 伤害计算
            // TODO: 通过 MovementController 施加垂直力
            // ctx.victim?.MovementController?.ApplyForce(Vector3.up * launchForce + ctx.hitDirection * horizontalForce);

            var hitModule = ctx.victim?.GetComponent<HitReactionModule>();
            hitModule?.ApplyVisualFeedback(ctx);

            Debug.Log($"<color=magenta>[Impact] Launch: {ctx.attacker?.name} → {ctx.victim?.name} | Tag:{entry.eventTag}</color>");
        }
    }

    /// <summary>
    /// 治疗 Impact 示例（接口占位）。
    /// </summary>
    public class HealImpact : IHitImpact
    {
        public void Execute(HitContext ctx, SkillEditor.HitEffectEntry entry)
        {
            // TODO: 治疗计算
            Debug.Log($"<color=green>[Impact] Heal: {ctx.attacker?.name} → {ctx.victim?.name} | Tag:{entry.eventTag}</color>");
        }
    }
}
