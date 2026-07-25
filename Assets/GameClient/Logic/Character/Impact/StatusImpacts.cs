using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性伤害 Impact。替代原有 DefaultDamageImpact 中的 TODO 占位。
    /// 执行：伤害计算 → 扣 HP → 累积喧响值 → 视觉反馈。
    /// </summary>
    public class DamageAttributeImpact : IHitImpact
    {
        public virtual void Execute(HitContext ctx, ATEditor.HitEffectEntry entry)
        {
            if (ctx.victim == null) return;

            var victimStatus = ctx.victim.StatusModule;
            if (victimStatus == null) return;

            // ── 伤害计算（占位公式：ATK - DEF，最低 1） ──
            float damage = CalculateDamage(ctx);
            if (victimStatus.Attributes.Has(AttributeId.HP))
            {
                victimStatus.Attributes.Modify(AttributeId.HP, -damage);

                Debug.Log($"<color=yellow>[DamageImpact] {ctx.attacker?.name} → {ctx.victim?.name} | " +
                          $"DMG:{damage:F0} | HP:{victimStatus.Attributes.GetCurrent(AttributeId.HP):F0} | " +
                          $"Tag:{entry.eventTag}</color>");
            }

            // ── 喧响值累积（仅受击方） ──
            float dazeAmount = CalculateDaze(ctx);
            if (dazeAmount > 0f && victimStatus.Attributes.Has(AttributeId.Daze))
            {
                victimStatus.Attributes.Modify(AttributeId.Daze, +dazeAmount);
            }

            // ── 视觉反馈 ──
            ctx.victim.HitReactionModule?.ApplyVisualFeedback(ctx);
        }

        /// <summary>
        /// 占位伤害公式。ATK - DEF，最低 1。
        /// 后续替换为正式的数值系统。
        /// </summary>
        protected virtual float CalculateDamage(HitContext ctx)
        {
            float atk = 100f; // 占位默认攻击力
            float def = 0f;   // 占位默认防御力

            if (ctx.attacker?.StatusModule?.Attributes != null)
            {
                if (ctx.attacker.StatusModule.Attributes.Has(AttributeId.ATK))
                {
                    atk = ctx.attacker.StatusModule.Attributes.GetCurrent(AttributeId.ATK);
                }
            }

            if (ctx.victim?.StatusModule?.Attributes != null)
            {
                if (ctx.victim.StatusModule.Attributes.Has(AttributeId.DEF))
                {
                    def = ctx.victim.StatusModule.Attributes.GetCurrent(AttributeId.DEF);
                }
            }

            return Mathf.Max(1f, atk - def);
        }

        /// <summary>
        /// 占位喧响值公式。固定值 10。
        /// 后续可根据攻击类型/技能配置调整。
        /// </summary>
        protected virtual float CalculateDaze(HitContext ctx)
        {
            return 10f;
        }
    }

    /// <summary>
    /// 重击属性 Impact。更大的伤害倍率 + 更大的喧响值。
    /// </summary>
    public class HeavyDamageAttributeImpact : DamageAttributeImpact
    {
        protected override float CalculateDamage(HitContext ctx)
        {
            return base.CalculateDamage(ctx) * 2f;
        }

        protected override float CalculateDaze(HitContext ctx)
        {
            return 25f;
        }
    }

    /// <summary>
    /// 施加 Buff 的 Impact。命中时对受击者施加指定 Buff。
    /// 需要注册到 HitImpactRegistry，eventTag 自定义。
    /// </summary>
    public class ApplyBuffImpact : IHitImpact
    {
        private readonly BuffDefAsset _buffDef;

        public ApplyBuffImpact(BuffDefAsset buffDef)
        {
            _buffDef = buffDef;
        }

        public void Execute(HitContext ctx, ATEditor.HitEffectEntry entry)
        {
            if (ctx.victim == null || _buffDef == null) return;

            var statusModule = ctx.victim.StatusModule;
            if (statusModule == null) return;

            // 检查免疫
            if (statusModule.IsBuffImmune(_buffDef))
            {
                Debug.Log($"<color=grey>[ApplyBuffImpact] {ctx.victim.name} 免疫 Buff '{_buffDef.DisplayName}'</color>");
                return;
            }

            statusModule.Buffs.AddBuff(_buffDef, ctx.attacker);
            Debug.Log($"<color=green>[ApplyBuffImpact] {ctx.attacker?.name} → {ctx.victim.name} | 施加 Buff '{_buffDef.DisplayName}'</color>");
        }
    }

    /// <summary>
    /// 通用属性修改 Impact。命中时对指定目标（攻击者或受击者）的指定属性进行加减。
    /// 可以用来替代专门编写的各种计数器 Impact（如霜冻层数+1）。
    /// </summary>
    public class ModifyAttributeImpact : IHitImpact
    {
        private readonly AttributeId _targetAttribute;
        private readonly float _deltaValue;
        private readonly bool _applyToAttacker; // true: 改攻击者的属性(攒层), false: 改受击者的属性(削韧/抽蓝)

        public ModifyAttributeImpact(AttributeId targetAttribute, float deltaValue, bool applyToAttacker = true)
        {
            _targetAttribute = targetAttribute;
            _deltaValue = deltaValue;
            _applyToAttacker = applyToAttacker;
        }

        public void Execute(HitContext ctx, ATEditor.HitEffectEntry entry)
        {
            var targetEntity = _applyToAttacker ? ctx.attacker : ctx.victim;
            
            if (targetEntity == null)
            {
                // Debug.LogWarning($"<color=red>[ModifyAttrImpact] 失败：找不到目标实体 (applyToAttacker={_applyToAttacker})。检查 attacker/victim 是否为空。</color>");
                // return;
            }

            if (targetEntity.StatusModule == null || targetEntity.StatusModule.Attributes == null)
            {
                // Debug.LogWarning($"<color=red>[ModifyAttrImpact] 失败：实体 {targetEntity.name} 没有初始化 StatusModule 或 Attributes。</color>");
                return;
            }

            if (targetEntity.StatusModule.Attributes.Has(_targetAttribute))
            {
                targetEntity.StatusModule.Attributes.Modify(_targetAttribute, _deltaValue);

                float current = targetEntity.StatusModule.Attributes.GetCurrent(_targetAttribute);
                string targetName = _applyToAttacker ? "Attacker" : "Victim";
                // Debug.Log($"<color=cyan>[ModifyAttrImpact] 成功：{targetEntity.name}({targetName}) {_targetAttribute} {_deltaValue:+0;-0;0} = {current:F0}</color>");
            }
            else
            {
                // Debug.LogWarning($"<color=red>[ModifyAttrImpact] 失败：实体 {targetEntity.name} 身上找不到名为 {_targetAttribute} 的属性！请检查 StatusProfile 配置。</color>");
            }
        }
    }
}
